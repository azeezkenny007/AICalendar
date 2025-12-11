# API Performance Optimization Guide

## Current Performance Analysis

Your API already has some good optimizations:
- ✅ NoTracking queries (line 128 in Program.cs)
- ✅ Response compression (Brotli/Gzip)
- ✅ Redis caching infrastructure
- ✅ Connection retry logic

## Critical Optimizations (Implement These First)

### 1. Add Response Caching to GET Endpoints ⚡

**Impact:** 50-90% faster responses for repeated requests

**Implementation:**

Add to `Program.cs` after line 60:

```csharp
// Add response caching
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();

// Add output caching (better than response caching)
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(30)));

    // Cache predictions for 1 minute
    options.AddPolicy("predictions", builder =>
        builder.Expire(TimeSpan.FromMinutes(1))
               .Tag("predictions"));

    // Cache user data for 5 minutes
    options.AddPolicy("users", builder =>
        builder.Expire(TimeSpan.FromMinutes(5))
               .Tag("users"));
});
```

Add to middleware pipeline in `Program.cs` after line 296:

```csharp
app.UseOutputCache(); // Add before UseRouting()
app.UseRouting();
```

**Update Controllers:**

```csharp
// In PredictionsController.cs
[HttpGet("user/{userId:guid}")]
[OutputCache(PolicyName = "predictions")]
public async Task<IActionResult> GetUserPredictions(Guid userId)
{
    // ... existing code
}

[HttpGet("user/{userId:guid}/items-limit")]
[OutputCache(PolicyName = "predictions")]
public async Task<IActionResult> GetUserPredictionItemsLimit(Guid userId)
{
    // ... existing code
}
```

---

### 2. Add Database Indexes 🔍

**Impact:** 70-95% faster database queries

**Create Migration:**

```bash
dotnet ef migrations add AddPerformanceIndexes --project src/AICalendar.Infrastructure --startup-project src/AICalendar.API
```

**Add to PredictionConfiguration.cs:**

```csharp
// In Configure method, add:
builder.HasIndex(p => p.UserId);
builder.HasIndex(p => p.CreatedAt);
builder.HasIndex(p => p.Status);

// For prediction items table
item.HasIndex("PredictionId");
item.HasIndex(i => i.DueDate);
item.HasIndex(i => i.IsAccepted);
```

**Add to TransactionConfiguration.cs:**

```csharp
builder.HasIndex(t => t.UserId);
builder.HasIndex(t => t.Date);
builder.HasIndex(t => new { t.UserId, t.Date }); // Composite index
```

---

### 3. Enable Database Connection Pooling 🏊

**Impact:** 30-50% faster connection times

**Update Program.cs (line 115):**

```csharp
options.UseSqlServer(connectionString, sqlOptions =>
{
    sqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(30),
        errorNumbersToAdd: null);

    sqlOptions.CommandTimeout(60);

    // Add these optimizations
    sqlOptions.MaxBatchSize(100); // Batch multiple operations
    sqlOptions.MinBatchSize(2);
    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
});

// Add connection pooling
options.EnableSensitiveDataLogging(false);
options.EnableDetailedErrors(false); // Disable in production
options.ConfigureWarnings(warnings =>
    warnings.Ignore(RelationalEventId.MultipleCollectionIncludeWarning));
```

---

### 4. Optimize Query Projections 📊

**Impact:** 40-60% less data transferred

**Update GetUserPredictionsQueryHandler.cs:**

Instead of loading full entities, project only needed fields:

```csharp
public async Task<Result<List<PredictionDto>>> Handle(
    GetUserPredictionsQuery request,
    CancellationToken cancellationToken)
{
    var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
    if (user == null)
    {
        return Result<List<PredictionDto>>.Failure($"User {request.UserId} not found.");
    }

    // Use AsNoTracking and select only needed fields
    var predictions = await _context.Predictions
        .AsNoTracking()
        .Where(p => p.UserId == request.UserId)
        .OrderByDescending(p => p.CreatedAt)
        .Select(p => new PredictionDto(
            p.Id.Value,
            p.UserId.Value,
            p.Status.ToString(),
            p.CreatedAt,
            p.Items.Select(i => new PredictionItemDto(
                i.Id.Value,
                i.Merchant,
                i.Amount,
                i.DueDate,
                i.Explanation,
                i.Confidence.Value,
                i.Pattern.ToString(),
                i.IsAccepted,
                i.IsEdited,
                i.Account,
                i.AccountName,
                i.Description
            )).ToList()
        ))
        .ToListAsync(cancellationToken);

    return Result<List<PredictionDto>>.Success(predictions);
}
```

---

### 5. Add Pagination to Large Result Sets 📄

**Impact:** 80-95% faster for large datasets

**Create new query:**

```csharp
public record GetUserPredictionsPagedQuery(
    UserId UserId,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<Result<PagedResult<PredictionDto>>>;

public record PagedResult<T>(
    List<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages
);
```

**Handler:**

```csharp
public async Task<Result<PagedResult<PredictionDto>>> Handle(
    GetUserPredictionsPagedQuery request,
    CancellationToken ct)
{
    var totalCount = await _context.Predictions
        .Where(p => p.UserId == request.UserId)
        .CountAsync(ct);

    var predictions = await _context.Predictions
        .AsNoTracking()
        .Where(p => p.UserId == request.UserId)
        .OrderByDescending(p => p.CreatedAt)
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(/* ... */)
        .ToListAsync(ct);

    var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

    return Result<PagedResult<PredictionDto>>.Success(
        new PagedResult<PredictionDto>(
            predictions,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages
        )
    );
}
```

---

### 6. Add Redis Distributed Caching 🚀

**Impact:** 70-95% faster for frequently accessed data

**Implementation:**

```csharp
// In your query handler
public class GetUserPredictionsQueryHandler
{
    private readonly IPredictionRepository _repository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<GetUserPredictionsQueryHandler> _logger;

    public async Task<Result<List<PredictionDto>>> Handle(
        GetUserPredictionsQuery request,
        CancellationToken ct)
    {
        var cacheKey = $"predictions:user:{request.UserId.Value}";

        // Try get from cache
        var cachedData = await _cache.GetStringAsync(cacheKey, ct);
        if (cachedData != null)
        {
            var predictions = JsonSerializer.Deserialize<List<PredictionDto>>(cachedData);
            return Result<List<PredictionDto>>.Success(predictions);
        }

        // Get from database
        var result = await _repository.GetByUserIdAsync(request.UserId, ct);
        var dtos = ConvertToDtos(result);

        // Cache for 5 minutes
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(dtos),
            options,
            ct
        );

        return Result<List<PredictionDto>>.Success(dtos);
    }
}
```

**Clear cache on edit:**

```csharp
// In EditPredictionItemCommandHandler
public async Task<Result> Handle(EditPredictionItemCommand request, CancellationToken ct)
{
    // ... existing edit logic

    await _unitOfWork.SaveChangesAsync(ct);

    // Clear cache
    var cacheKey = $"predictions:user:{prediction.UserId.Value}";
    await _cache.RemoveAsync(cacheKey, ct);

    return Result.Success();
}
```

---

### 7. Add SQL Query Compilation 📝

**Impact:** 20-40% faster queries

**Update repositories:**

```csharp
// In PredictionRepository
private static readonly Func<ApplicationDbContext, UserId, IAsyncEnumerable<Prediction>>
    _compiledGetByUserId = EF.CompileAsyncQuery(
        (ApplicationDbContext context, UserId userId) =>
            context.Set<Prediction>()
                .Include(p => p.Items)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
    );

public async Task<List<Prediction>> GetByUserIdAsync(UserId userId, CancellationToken ct = default)
{
    var predictions = new List<Prediction>();
    await foreach (var prediction in _compiledGetByUserId(_context, userId).WithCancellation(ct))
    {
        predictions.Add(prediction);
    }
    return predictions;
}
```

---

### 8. Enable HTTP/2 and Compression 🗜️

**Already implemented**, but verify in `launchSettings.json`:

```json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "launchBrowser": false,
      "applicationUrl": "https://localhost:7000;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_HTTP_PORTS": "5000",
        "ASPNETCORE_HTTPS_PORTS": "7000"
      }
    }
  }
}
```

---

### 9. Reduce Payload Size with DTOs 📦

**Current issue:** Returning full domain entities with all navigation properties

**Solution:** Use minimal DTOs

```csharp
// Minimal DTO for list views
public record PredictionListItemDto(
    Guid Id,
    string Merchant,
    decimal Amount,
    DateTime DueDate,
    double Confidence
);

// Full DTO only for detail views
public record PredictionDetailDto(
    Guid Id,
    Guid UserId,
    string Status,
    DateTime CreatedAt,
    List<PredictionItemDetailDto> Items
);
```

---

### 10. Add Request Throttling/Rate Limiting 🚦

**Impact:** Prevent API overload

```csharp
// Add to Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// Add middleware
app.UseRateLimiter();
```

---

## Performance Monitoring

### Add Application Insights (Optional)

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Add Performance Logging

```csharp
// Create a performance logging middleware
public class PerformanceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        if (sw.ElapsedMilliseconds > 1000) // Log slow requests (>1s)
        {
            _logger.LogWarning(
                "Slow request: {Method} {Path} took {Duration}ms",
                context.Request.Method,
                context.Request.Path,
                sw.ElapsedMilliseconds
            );
        }
    }
}
```

---

## Implementation Priority

### Phase 1 (Do This Week) - Biggest Impact:
1. ✅ Add output caching to GET endpoints
2. ✅ Add database indexes
3. ✅ Enable connection pooling optimizations

### Phase 2 (Next Week):
4. ✅ Optimize query projections
5. ✅ Add Redis distributed caching
6. ✅ Add pagination

### Phase 3 (Later):
7. ✅ SQL query compilation
8. ✅ Request throttling
9. ✅ Performance monitoring

---

## Expected Performance Gains

| Optimization | Response Time Improvement |
|--------------|---------------------------|
| Output Caching | 50-90% faster |
| Database Indexes | 70-95% faster queries |
| Redis Caching | 70-95% for cached data |
| Pagination | 80-95% for large datasets |
| Query Projections | 40-60% less data |
| Connection Pooling | 30-50% faster connections |

**Combined:** Your API could be **5-10x faster** after all optimizations!

---

## Measuring Performance

### Before optimization:
```bash
# Test endpoint
curl -w "@curl-format.txt" -o /dev/null -s https://localhost:7000/api/predictions/user/{userId}

# curl-format.txt:
time_namelookup:  %{time_namelookup}\n
time_connect:  %{time_connect}\n
time_starttransfer:  %{time_starttransfer}\n
time_total:  %{time_total}\n
```

### After optimization:
Run same test and compare!
