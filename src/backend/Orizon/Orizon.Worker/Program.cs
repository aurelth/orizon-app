using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Orizon.Infrastructure.Data;
using Orizon.Infrastructure.Identity;
using Orizon.Infrastructure.Repositories;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Worker.Jobs;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Iniciando Orizon Worker...");

try
{
    var builder = Host.CreateApplicationBuilder(args);
    
    builder.Services.AddSerilog((services, config) =>
        config
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"]
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
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(
                    builder.Configuration.GetConnectionString("PostgreSQL"))));
   
    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = builder.Configuration
            .GetValue<int>("Hangfire:WorkerCount", 5);
        options.ServerName = "orizon-worker";
    });
   
    // REPOSITORIES  
    builder.Services.AddScoped<IBriefingRepository, BriefingRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    
    // JOBS    
    builder.Services.AddScoped<BriefingJob>();

    var host = builder.Build();
    
    // REGISTRAR O JOB RECORRENTE => Cron: 0 6 * * * = todos os dias às 06h00     
    using (var scope = host.Services.CreateScope())
    {
        RecurringJob.AddOrUpdate<BriefingJob>(
            recurringJobId: "morning-briefing",
            methodCall: job => job.ExecuteAsync(CancellationToken.None),
            cronExpression: "0 6 * * *",
            options: new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                    "E. South America Standard Time")
            });

        Log.Information(
            "Job 'morning-briefing' registrado — executa diariamente às 06h (Brasília)");
    }

    Log.Information("Orizon Worker iniciado com sucesso");

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Orizon Worker falhou ao iniciar");
}
finally
{
    Log.CloseAndFlush();
}