using AICalendar.API.Middleware;
using AICalendar.Application.Common.Behaviors;
using AICalendar.Application.Common.Interfaces;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.Services;
using AICalendar.Infrastructure.Data;
using AICalendar.Infrastructure.Persistence.Repositories;
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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

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
        Version = "v1",
        Description = @"AI-powered calendar and prediction API with intelligent payment tracking and forecasting.

## Features
- **Predictions**: AI-generated payment predictions from transaction history
- **Calendar**: Manage scheduled payments with due dates and tracking
- **Health Monitoring**: Comprehensive health checks for all infrastructure components

## Caching
Calendar endpoints use Redis caching for improved performance (1-hour TTL).
Cache is automatically invalidated on data modifications.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "AICalendar Team"
        }
    });

    // Include XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    // Order actions by API path for better organization
    c.OrderActionsBy(apiDesc => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.RelativePath}");
});

// Configure DbContext with Interceptors
builder.Services.AddSingleton<AICalendar.Infrastructure.Persistence.Interceptors.OutboxInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseSqlServer(connectionString);

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

// Register Domain Services
builder.Services.AddScoped<ICalendarDomainService, CalendarDomainService>();

// Register Resilience Options
builder.Services.Configure<AICalendar.Infrastructure.Services.ResilienceOptions>(
    builder.Configuration.GetSection(AICalendar.Infrastructure.Services.ResilienceOptions.SectionName));

// Register Application Services with Polly Resilience Policies
builder.Services.AddHttpClient<AICalendar.Application.Services.IAIPredictionService, AIPredictionService>()
    .AddPolicyHandler((services, request) =>
    {
        var logger = services.GetRequiredService<ILogger<AIPredictionService>>();
        var options = services.GetRequiredService<IOptions<AICalendar.Infrastructure.Services.ResilienceOptions>>().Value;
        return AICalendar.Infrastructure.Services.AIPredictionServicePolicies.GetResiliencePipeline(logger, options);
    });

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

// ═══════════════════════════════════════════════════════════
// Firebase Cloud Messaging Configuration
// ═══════════════════════════════════════════════════════════
var firebaseCredentialsPath = Path.Combine(
    builder.Environment.ContentRootPath,
    "firebase-credentials.json"
);

if (File.Exists(firebaseCredentialsPath))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseCredentialsPath)
    });

    builder.Services.AddScoped<INotificationService, FirebaseNotificationService>();

    Console.WriteLine("✅ Firebase Cloud Messaging initialized");
}
else
{
    Console.WriteLine("⚠️  WARNING: firebase-credentials.json not found. Notifications disabled.");

    // Register a null/dummy service for development
    builder.Services.AddScoped<INotificationService, NullNotificationService>();
}


var app = builder.Build();

// Configure the HTTP request pipeline.

// Add global exception handling middleware (must be first in pipeline)
app.UseMiddleware<ExceptionHandlingMiddleware>();

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
else
{
    // In production, don't use DeveloperExceptionPage
    app.UseHsts();
}


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

app.UseRouting();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Enable CORS
app.UseCors("AllowAll");

app.UseAuthorization();

// Configure Hangfire Dashboard
app.UseHangfireDashboardWithAuth();

app.MapControllers();

app.Run();
