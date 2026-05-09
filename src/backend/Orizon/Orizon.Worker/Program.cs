using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using Orizon.Infrastructure.Data;
using Orizon.Infrastructure.Identity;
using Orizon.Infrastructure.Repositories;
using Orizon.Infrastructure.Services.Email;
using Orizon.Infrastructure.Services.External;
using Orizon.Worker.Jobs;
using SendGrid;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Iniciando Orizon Worker...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, config) =>
        config
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.Seq(context.Configuration["Seq:ServerUrl"]
                ?? "http://localhost:5341"));

    builder.Services.AddDbContext<OrizonDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("PostgreSQL"),
            npgsql => npgsql.MigrationsAssembly(
                typeof(OrizonDbContext).Assembly.FullName)));

    builder.Services.AddIdentityCore<AppIdentityUser>()
        .AddEntityFrameworkStores<OrizonDbContext>();

    builder.Services.AddHangfire(config =>
        config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(
                    builder.Configuration.GetConnectionString("PostgreSQL"))));

    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = builder.Configuration
            .GetValue<int>("Hangfire:WorkerCount", 5);
        options.ServerName = "orizon-worker";
        options.Queues = ["default"];
        options.SchedulePollingInterval = TimeSpan.FromSeconds(1);
    });

    // REPOSITORIES
    builder.Services.AddScoped<IBriefingRepository, BriefingRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ITrelloBoardConfigRepository, TrelloBoardConfigRepository>();

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
    builder.Services.AddScoped<IClaudeService, ClaudeService>();

    // EMAIL
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

    // JOBS
    builder.Services.AddScoped<BriefingJob>();

    var app = builder.Build();

    // ENDPOINT INTERNO — acionado pela API para gerar briefing manualmente
    app.MapPost("/internal/briefing/trigger", () =>
    {
        RecurringJob.TriggerJob("morning-briefing");
        return Results.Accepted("/internal/briefing/trigger", new { message = "Job acionado com sucesso." });
    });

    using (var scope = app.Services.CreateScope())
    {
        var recurringJobManager = scope.ServiceProvider
            .GetRequiredService<IRecurringJobManager>();

        recurringJobManager.RemoveIfExists("morning-briefing");

        RecurringJob.AddOrUpdate<BriefingJob>(
            recurringJobId: "morning-briefing",
            methodCall: job => job.ExecuteAsync(CancellationToken.None),
            cronExpression: "0 */4 * * *",
            options: new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                    "E. South America Standard Time")
            });

        Log.Information("Job 'morning-briefing' registrado com sucesso");
    }

    Log.Information("Orizon Worker iniciado com sucesso");

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Orizon Worker falhou ao iniciar");
}
finally
{
    Log.CloseAndFlush();
}