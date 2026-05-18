using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Orizon.API.Hubs;
using Orizon.API.Metrics;
using Orizon.Application.Common.Behaviors;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using Orizon.Application.UseCases.Auth.Commands.RegisterUser;
using Orizon.Infrastructure.Data;
using Orizon.Infrastructure.Identity;
using Orizon.Infrastructure.Repositories;
using Orizon.Infrastructure.Services;
using Orizon.Infrastructure.Services.Auth;
using Orizon.Infrastructure.Services.Email;
using Orizon.Infrastructure.Services.External;
using Scalar.AspNetCore;
using SendGrid;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Iniciando Orizon API...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.Seq(context.Configuration["Seq:ServerUrl"]
                ?? "http://localhost:5341"));

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    builder.Services.AddDbContext<OrizonDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("PostgreSQL"),
            npgsql => npgsql.MigrationsAssembly(
                typeof(OrizonDbContext).Assembly.FullName)));

    builder.Services.AddIdentity<AppIdentityUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<OrizonDbContext>()
    .AddDefaultTokenProviders();

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Secret"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/briefing"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration
            .GetConnectionString("Redis");
        options.InstanceName = "orizon:";
    });

    builder.Services.AddSignalR();

    builder.Services.AddHangfire(config =>
        config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(
                    builder.Configuration.GetConnectionString("PostgreSQL"))));

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("OrizonPolicy", policy =>
        {
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? new[] { "http://localhost:4200" };

            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // REPOSITORIES
    builder.Services.AddScoped<IBriefingRepository, BriefingRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    builder.Services.AddScoped<ITrelloBoardConfigRepository, TrelloBoardConfigRepository>();

    // AUTH SERVICES
    builder.Services.AddScoped<IJwtService, JwtService>();
    builder.Services.AddScoped<IIdentityService, IdentityService>();

    // EXTERNAL SERVICES
    builder.Services.AddHttpClient<IWeatherService, WeatherService>()
        .AddStandardResilienceHandler();

    builder.Services.AddHttpClient<IGoogleOAuthService, GoogleOAuthService>()
        .AddStandardResilienceHandler();

    builder.Services.AddHttpClient<TrelloService>()
        .AddStandardResilienceHandler();
    builder.Services.AddScoped<ITrelloService, TrelloService>();

    builder.Services.AddScoped<IGmailService, GmailIntegrationService>();
    builder.Services.AddScoped<ICalendarService, CalendarIntegrationService>();
    builder.Services.AddScoped<IGoogleTasksService, GoogleTasksIntegrationService>();
    builder.Services.AddScoped<IClaudeService, ClaudeService>();
    builder.Services.AddHttpClient<IJobScheduler, HangfireJobScheduler>();

    // EMAIL — SendGrid
    var sendGridApiKey = builder.Configuration["Email:SendGridApiKey"];
    if (!string.IsNullOrEmpty(sendGridApiKey))
    {
        builder.Services.AddSingleton<ISendGridClient>(
            new SendGridClient(sendGridApiKey));
        builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
    }
    else
    {
        builder.Services.AddScoped<IEmailNotificationService, NullEmailNotificationService>();
    }

    // MEDIATR + CQRS
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(
            typeof(RegisterUserCommand).Assembly));

    builder.Services.AddValidatorsFromAssembly(
        typeof(RegisterUserCommandValidator).Assembly);

    builder.Services.AddTransient(
        typeof(IPipelineBehavior<,>),
        typeof(ValidationBehavior<,>));

    // OPENTELEMETRY + PROMETHEUS
    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("Orizon.API")
                .AddPrometheusExporter();
        })
        .WithTracing(tracing =>
        {
            tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation();
        });

    // MÉTRICAS CUSTOMIZADAS
    builder.Services.AddSingleton<IOrizonMetrics, OrizonMetrics>();

    // HEALTH CHECKS
    var pgConnection = builder.Configuration.GetConnectionString("PostgreSQL");
    var redisConnection = builder.Configuration.GetConnectionString("Redis");

    var healthChecks = builder.Services.AddHealthChecks();

    if (!string.IsNullOrEmpty(pgConnection))
        healthChecks.AddNpgSql(pgConnection, name: "postgresql",
            tags: new[] { "db", "ready" });

    if (!string.IsNullOrEmpty(redisConnection))
        healthChecks.AddRedis(redisConnection, name: "redis",
            tags: new[] { "cache", "ready" });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseSerilogRequestLogging();
    app.UseCors("OrizonPolicy");
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<BriefingHub>("/hubs/briefing");
    app.MapHealthChecks("/health/ready");
    app.MapHealthChecks("/health/live");
    app.MapPrometheusScrapingEndpoint("/metrics");

    if (app.Environment.IsDevelopment())
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter()]
        });
    }

    Log.Information("Orizon API iniciada com sucesso");

    if (app.Environment.IsProduction())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrizonDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Migrations aplicadas com sucesso");
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Orizon API falhou ao iniciar");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }