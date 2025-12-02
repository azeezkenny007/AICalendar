# Implementation Plan: Add Caching to Calendar Endpoints

## Overview
Add caching to the calendar endpoints to improve performance by reducing database queries for frequently accessed calendar data. The application already has a Redis-based caching infrastructure in place, so we'll integrate it into the calendar query handler and ensure proper cache invalidation on updates.

## Current State Analysis

### Existing Infrastructure
- ✅ Redis cache service already implemented (`RedisCacheService`)
- ✅ Cache interface available (`ICacheService`) with methods: `GetAsync`, `SetAsync`, `RemoveAsync`, `GetOrSetAsync`
- ✅ Cache configuration in `appsettings.json` with Redis settings
- ✅ Key prefixing support (`aicalendar:` prefix)
- ✅ Default expiration time: 60 minutes (configurable)

### Endpoints to Cache
1. **GET /api/calendar/user/{userId}** - Primary candidate for caching
   - Query handler: `GetUserCalendarQueryHandler`
   - Returns: `CalendarDto` with all calendar items
   - Current behavior: Fetches from database on every request

### Cache Invalidation Points
Cache must be invalidated when calendar data changes:
1. **EditCalendarItem** - Updates calendar item details
2. **MarkItemAsPaid** - Changes payment status
3. **PredictionAcceptedCalendarHandler** - Adds new items to calendar

## Implementation Strategy

### 1. Cache Key Design
Use consistent, meaningful cache keys:
- Pattern: `calendar:user:{userId}`
- Example: `calendar:user:123e4567-e89b-12d3-a456-426614174000`
- The `aicalendar:` prefix will be added automatically by `RedisCacheService`

### 2. Cache in Query Handler
**File**: `GetUserCalendarQueryHandler.cs`

**Approach**: Use the `GetOrSetAsync` pattern for clean, readable code
- Check cache first for user's calendar
- If not found, fetch from database and cache the result
- Use configurable TTL (Time-To-Live) - default 60 minutes

**Benefits**:
- Reduces database load for frequent calendar views
- Improves response time for cached requests
- Graceful fallback if cache is unavailable

### 3. Cache Invalidation Strategy
Invalidate cache whenever calendar data is modified to ensure data consistency.

**Files to Update**:
1. `EditCalendarItemCommandHandler.cs`
   - After successful update, invalidate cache for the calendar's user
   - Extract userId from calendar aggregate

2. `MarkItemAsPaidCommandHandler.cs`
   - After successful paid status update, invalidate cache
   - Extract userId from calendar aggregate

3. `PredictionAcceptedCalendarHandler.cs`
   - After adding prediction items to calendar, invalidate cache
   - userId is already available in the domain event

**Pattern**: Call `_cacheService.RemoveAsync(cacheKey)` after successful database save

### 4. Cache Expiration Policy
- **Default TTL**: 1 hour (60 minutes)
- **Rationale**: Calendar data changes infrequently for most users, but should stay reasonably fresh
- **Alternative**: Could be made configurable via `CacheOptions` if needed

## Detailed Implementation Steps

### Step 1: Update GetUserCalendarQueryHandler
- Inject `ICacheService` into constructor
- Implement cache-aside pattern using `GetOrSetAsync`
- Generate cache key: `calendar:user:{userId}`
- Set TTL to 1 hour

### Step 2: Add Cache Invalidation to EditCalendarItemCommandHandler
- Inject `ICacheService` into constructor
- After successful `SaveChangesAsync`, invalidate cache
- Use `calendar.UserId.Value` to build cache key

### Step 3: Add Cache Invalidation to MarkItemAsPaidCommandHandler
- Inject `ICacheService` into constructor
- After successful `SaveChangesAsync`, invalidate cache
- Use `calendar.UserId.Value` to build cache key

### Step 4: Add Cache Invalidation to PredictionAcceptedCalendarHandler
- Inject `ICacheService` into constructor
- After successful `SaveChangesAsync`, invalidate cache
- Use `domainEvent.UserId.Value` from the event

## Trade-offs & Considerations

### Chosen Approach
✅ **Cache at Query Handler Level**
- Clean separation: queries cache, commands invalidate
- Follows CQRS pattern already in use
- Easy to maintain and understand
- Works with existing MediatR pipeline

### Alternative Approaches (Not Chosen)
❌ **Cache at Controller Level**
- Would bypass MediatR and break architectural consistency
- Harder to test
- Less reusable

❌ **Cache at Repository Level**
- Would cache domain entities instead of DTOs
- Complicates domain layer with infrastructure concerns
- Harder to control cache keys

❌ **Use MediatR Pipeline Behavior**
- Could create generic caching behavior
- More complex, harder to debug
- Overkill for single endpoint
- Would need custom attributes/metadata

### Cache Coherency
- **Write-Through**: Not needed (we invalidate instead)
- **Cache-Aside**: ✅ Chosen pattern - simple and effective
- **Risk**: Very low - invalidation happens immediately after save

### Performance Impact
- **Read Performance**: Significant improvement (database query eliminated)
- **Write Performance**: Minimal overhead (single Redis DELETE operation)
- **Network**: Redis is fast, typically <1ms latency

## Risks & Mitigation

### Risk 1: Stale Data
**Scenario**: Cache invalidation fails but database update succeeds
**Mitigation**:
- Cache service has error handling (logs errors, continues execution)
- TTL ensures stale data expires after 1 hour maximum
- Cache invalidation errors are logged for monitoring

### Risk 2: Cache Unavailability
**Scenario**: Redis is down
**Mitigation**:
- `RedisCacheService` already has graceful error handling
- Returns `default(T)` on errors, causing fallback to database
- Application continues to function without cache

### Risk 3: Missing Invalidation Point
**Scenario**: New code path modifies calendar without invalidating cache
**Mitigation**:
- All calendar modifications go through command handlers (CQRS pattern)
- Code review process should catch this
- Consider adding comment/documentation to CalendarDto

## Testing Considerations

### Manual Testing
1. Verify cache hit: Call GET endpoint twice, check logs for "Cache hit"
2. Verify cache miss: Call GET endpoint first time, check logs for "Cache miss"
3. Verify invalidation: Edit item, then GET - should fetch from DB
4. Verify TTL: Wait 1 hour, verify cache expires

### Integration Testing (Optional Future Work)
- Mock `ICacheService` in handler tests
- Verify cache methods are called with correct keys
- Verify cache invalidation on commands

## Files to Modify

1. ✏️ `src/AICalendar.Application/Calendar/Queries/GetUserCalendar/GetUserCalendarQueryHandler.cs`
2. ✏️ `src/AICalendar.Application/Calendar/Commands/EditCalendarItem/EditCalendarItemCommandHandler.cs`
3. ✏️ `src/AICalendar.Application/Calendar/Commands/MarkItemAsPaid/MarkItemAsPaidCommandHandler.cs`
4. ✏️ `src/AICalendar.Application/Predictions/DomainEventHandlers/PredictionAcceptedCalendarHandler.cs`

**Total**: 4 files

## Success Criteria

✅ Calendar GET requests are cached in Redis
✅ Cache is invalidated when calendar items are edited
✅ Cache is invalidated when items are marked as paid
✅ Cache is invalidated when prediction items are added
✅ Cache keys follow consistent naming pattern
✅ Error handling maintains existing resilience
✅ No breaking changes to existing functionality
✅ Logging shows cache hits/misses for debugging

## Implementation Notes

- Keep changes minimal and focused
- Follow existing patterns in the codebase
- Use the `GetOrSetAsync` helper method for cleaner code
- Add logging where helpful for debugging
- Maintain error handling standards
- No need for new configuration - use existing cache settings
