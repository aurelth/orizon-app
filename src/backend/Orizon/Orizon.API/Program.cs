using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using Orizon.Infrastructure.Data;
using Orizon.Infrastructure.Identity;
using Orizon.Infrastructure.Repositories;
using Orizon.Infrastructure.Services;
using Serilog;

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

    //SERVICES REGISTRATION
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();    
    builder.Services.AddScoped<IJwtService, JwtService>();    

    builder.Services.AddDbContext<OrizonDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("PostgreSQL"),
            npgsql => npgsql.MigrationsAssembly(
                typeof(OrizonDbContext).Assembly.FullName)));
      
    builder.Services.AddIdentity<AppIdentityUser, IdentityRole>(options =>
    {
        // PASSWORD CONFIGURATIONS
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
    
    // REDIS
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration
            .GetConnectionString("Redis");
        options.InstanceName = "orizon:";
    });
    
    // SIGNALR   
    builder.Services.AddSignalR();
    
    // CORS 
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

    builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("PostgreSQL")!,
            name: "postgresql",
            tags: new[] { "db", "ready" })
        .AddRedis(
            builder.Configuration.GetConnectionString("Redis")!,
            name: "redis",
            tags: new[] { "cache", "ready" });
       
    var app = builder.Build();

    // MIDDLEWARES PIPELINE
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }
    
    app.UseSerilogRequestLogging();    
    app.UseCors("OrizonPolicy");
    app.UseHttpsRedirection();    
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();    
    app.MapHealthChecks("/health/ready");
    app.MapHealthChecks("/health/live");

    Log.Information("Orizon API iniciada com sucesso");

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