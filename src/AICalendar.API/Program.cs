using AICalendar.API.Middleware;
using AICalendar.Application.Common.Behaviors;
using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.Services;
using AICalendar.Infrastructure.Data;
using AICalendar.Infrastructure.Persistence.Repositories;
using AICalendar.Infrastructure.Repositories;
using AICalendar.Infrastructure.Persistence.UnitOfWork;
using AICalendar.Infrastructure.ExternalServices.Cache;
using AICalendar.Infrastructure.Services;
using AICalendar.API.Extensions;
using AICalendar.API.Filters;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using AICalendar.Application.BackgroundJobs;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using Prometheus;
using Serilog;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using AICalendar.Application.Feedback.AI.Jobs;
using AICalendar.Application.Feedback.AI;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add response compression services
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;                     // Very important in 2024+
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    // options.Providers.Add<ZstdCompressionProvider>(); // .NET 9+ if you want Zstandard

    // Optional: configure compression level
    options.Providers.Add(new BrotliCompressionProvider(
        new BrotliCompressionProviderOptions { Level = CompressionLevel.Fastest }));
});

// Add output caching for performance
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(10)));

    options.AddPolicy("predictions-short", builder =>
        builder.Expire(TimeSpan.FromSeconds(30))
               .SetVaryByQuery("userId")
               .Tag("predictions"));

    options.AddPolicy("calendar-short", builder =>
        builder.Expire(TimeSpan.FromSeconds(30))
               .SetVaryByQuery("userId")
               .Tag("calendar"));

    options.AddPolicy("notifications-short", builder =>
        builder.Expire(TimeSpan.FromSeconds(10))
               .SetVaryByQuery("userId")
               .Tag("notifications"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AICalendar API",
        Version = "v1.0",
        Description = "AI-Powered Payment Prediction & Calendar Management API with support for predictions, calendar management, user management, notifications, and health monitoring.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "AICalendar Development Team",
            Email = "support@aicalendar.com"
        }
    });

    // Include XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    // Group by controller for better organization
    c.TagActionsBy(api =>
    {
        var controllerName = api.ActionDescriptor.RouteValues["controller"];
        return new[] { controllerName ?? "Default" };
    });

    // Order actions alphabetically within each group
    c.OrderActionsBy(apiDesc =>
    {
        var controller = apiDesc.ActionDescriptor.RouteValues["controller"];
        var action = apiDesc.ActionDescriptor.RouteValues["action"];
        return $"{controller}_{apiDesc.HttpMethod}_{apiDesc.RelativePath}";
    });

    // Add custom operation IDs for better documentation
    c.CustomOperationIds(apiDesc =>
    {
        var controller = apiDesc.ActionDescriptor.RouteValues["controller"];
        var action = apiDesc.ActionDescriptor.RouteValues["action"];
        return $"{controller}_{action}";
    });
});

// Configure DbContext with Interceptors
builder.Services.AddScoped<AICalendar.Infrastructure.Persistence.Interceptors.OutboxInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Enable retry on transient failures (Azure SQL recommended)
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);

        // Set command timeout to 60 seconds
        sqlOptions.CommandTimeout(60);

        // Performance optimizations
        sqlOptions.MaxBatchSize(100);
        sqlOptions.MinBatchSize(2);
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });

    // Set global query tracking behavior to NoTracking
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

    // Add OutboxInterceptor for automatic domain event conversion
    var outboxInterceptor = serviceProvider.GetRequiredService<AICalendar.Infrastructure.Persistence.Interceptors.OutboxInterceptor>();
    options.AddInterceptors(outboxInterceptor);
});

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

// Register Repositories
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IPredictionRepository, PredictionRepository>();
builder.Services.AddScoped<ICalendarRepository, CalendarRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserFeedbackRepository, UserFeedbackRepository>();
builder.Services.AddScoped<IFailedPredictionAttemptRepository, FailedPredictionAttemptRepository>();
builder.Services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();

// Register Domain Services
builder.Services.AddScoped<ICalendarDomainService, CalendarDomainService>();

// Register Resilience Options
builder.Services.Configure<AICalendar.Infrastructure.Resilience.ResilienceOptions>(
    builder.Configuration.GetSection(AICalendar.Infrastructure.Resilience.ResilienceOptions.SectionName));

// Register Application Services with Polly Resilience Policies
builder.Services.AddHttpClient<AICalendar.Application.Services.IAIPredictionService, AIPredictionService>()
    .AddPolicyHandler((services, request) =>
    {
        var logger = services.GetRequiredService<ILogger<AIPredictionService>>();
        var options = services.GetRequiredService<IOptions<AICalendar.Infrastructure.Resilience.ResilienceOptions>>().Value;
        return AICalendar.Infrastructure.Resilience.AIPredictionServicePolicies.GetResiliencePipeline(logger, options);
    });

builder.Services.AddHttpClient<IAIFeedbackClient, AIFeedbackClient>();

builder.Services.AddScoped<SendFeedbackToAIJob>();

// Register UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(AICalendar.Application.AssemblyReference).Assembly);

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
builder.Services.AddScoped<AICalendar.Infrastructure.Outbox.OutboxProcessorJob>();
builder.Services.AddScoped<AICalendar.Infrastructure.BackgroundJobs.CleanupOutboxJob>();
builder.Services.AddScoped<AICalendar.Application.BackgroundJobs.BatchPredictionJob>();
builder.Services.AddScoped<AICalendar.Application.BackgroundJobs.SendRemindersJob>();
builder.Services.AddScoped<CleanupExpiredPredictionsJob>();

// Configure Hangfire for background job processing
builder.Services.AddHangfireServices(builder.Configuration);

// Register Cache Services (Redis or Null based on configuration)
builder.Services.AddCacheServices(builder.Configuration);

// Register cache invalidator
builder.Services.AddScoped<AICalendar.Application.Common.Interfaces.ICacheInvalidator, AICalendar.Infrastructure.Services.OutputCacheInvalidator>();

// ═══════════════════════════════════════════════════════════
// Firebase Cloud Messaging Configuration
// ═══════════════════════════════════════════════════════════
// var firebaseCredentialsPath = Path.Combine(
//     builder.Environment.ContentRootPath,
//     "firebase-credentials.json"
// );



var isDocker = Environment.GetEnvironmentVariable("RENDER") == "true" && 
               Directory.Exists("/etc/secrets"); // Or set RENDER_DOCKER env var manually in dashboard

var firebaseCredentialsPath = isDocker 
    ? Path.Combine("/etc/secrets", "firebase-credentials.json")
    : Path.Combine(builder.Environment.ContentRootPath, "firebase-credentials.json");

if (File.Exists(firebaseCredentialsPath))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseCredentialsPath)
    });

    builder.Services.AddScoped<INotificationService, FirebaseNotificationService>();

    Log.Information("✅ Firebase Cloud Messaging initialized");
}
else
{
    Log.Warning("⚠️  WARNING: firebase-credentials.json not found. Notifications disabled.");

    // Register a null/dummy service for development
    builder.Services.AddScoped<INotificationService, NullNotificationService>();
}


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>(); // Or your Log

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        logger.LogInformation("🔄 Applying EF Core migrations...");
        
        // This applies pending migrations or does nothing if up-to-date
        context.Database.Migrate(); // Async: await context.Database.MigrateAsync();

        logger.LogInformation("✅ Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error during database migration!");
        // Don't crash the app—log and continue (or throw in dev)
        if (app.Environment.IsDevelopment())
        {
            throw; // Fail fast in dev
        }
    }
}
// Configure the HTTP request pipeline.

// IMPORTANT: This middleware must be one of the FIRST
app.UseResponseCompression();

// Add global exception handling middleware (must be first in pipeline)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Enable Swagger in all environments (Development and Production)
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

// Initialize Hangfire recurring jobs
using (var scope = app.Services.CreateScope())
{
    try
    {
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<Hangfire.IRecurringJobManager>();
        AICalendar.Infrastructure.BackgroundJobs.HangfireConfiguration.ConfigureRecurringJobs(configuration, recurringJobManager);
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Hangfire recurring jobs configured successfully");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to configure Hangfire recurring jobs");
    }
}

// Add output caching middleware
app.UseOutputCache();

app.UseRouting();

// Log all HTTP requests through Serilog
app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Enable CORS
app.UseCors("AllowAll");

app.UseAuthorization();

// Configure Hangfire Dashboard
app.UseHangfireDashboardWithAuth();

// Add Prometheus metrics middleware
app.UseMetricServer();
app.UseHttpMetrics();

app.MapControllers();

app.Run();
