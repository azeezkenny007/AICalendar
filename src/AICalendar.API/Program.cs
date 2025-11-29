using AICalendar.Application.Common.Behaviors;
using AICalendar.Domain.Interfaces;
using AICalendar.Infrastructure.Data;
using AICalendar.Infrastructure.Persistence.UnitOfWork;
using AICalendar.Infrastructure.ExternalServices.Cache;
using AICalendar.API.Filters;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AICalendar API",
        Version = "v1",
        Description = "AI-powered calendar and prediction API",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "AICalendar Team"
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(AICalendar.Application.AssemblyReference).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

// Register FluentValidation validators
builder.Services.AddValidatorsFromAssembly(typeof(AICalendar.Application.AssemblyReference).Assembly);

// Register background jobs for Hangfire
builder.Services.AddScoped<AICalendar.Application.BackgroundJobs.CleanupExpiredPredictionsJob>();

// Configure Hangfire for background job processing
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

// Add Hangfire server
builder.Services.AddHangfireServer(options =>
{
    var hangfireSettings = builder.Configuration.GetSection("Hangfire");
    options.WorkerCount = hangfireSettings.GetValue<int>("WorkerCount", Environment.ProcessorCount * 5);
    options.ServerTimeout = TimeSpan.FromMinutes(4);
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
});

// Register Cache Services (Redis or Null based on configuration)
builder.Services.AddCacheServices(builder.Configuration);


var app = builder.Build();

// Configure the HTTP request pipeline.
// Swagger should be available in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AICalendar API v1");
        c.RoutePrefix = "swagger"; // Swagger UI at /swagger instead of /swagger/index.html
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.EnableValidator();
    });
}

// Automatic Migration
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // This applies any pending migrations to the database.
        // If the database doesn't exist, it creates it.
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Initialize Hangfire recurring jobs
using (var scope = app.Services.CreateScope())
{
    try
    {
        AICalendar.Infrastructure.BackgroundJobs.HangfireConfiguration.ConfigureRecurringJobs();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Hangfire recurring jobs configured successfully");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to configure Hangfire recurring jobs");
    }
}

app.UseRouting();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

// Configure Hangfire Dashboard
// In Development: Open access
// In Production: Protected by HangfireAuthorizationFilter (requires authentication)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    DashboardTitle = "AICalendar Background Jobs",
    StatsPollingInterval = 2000,
    // Production security settings
    IsReadOnlyFunc = (DashboardContext context) =>
    {
        // Make dashboard read-only in production (optional)
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return environment == "Production";
    }
});

app.MapControllers();

app.Run();
