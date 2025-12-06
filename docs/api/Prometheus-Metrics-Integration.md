# Prometheus Metrics Integration

## Overview

The AICalendar API integrates Prometheus metrics for comprehensive monitoring and observability. This document explains the implementation and configuration of metrics collection using the `prometheus-net.AspNetCore` package.

## Package Dependencies

The following NuGet package is required for Prometheus metrics integration:

```xml
<PackageReference Include="prometheus-net.AspNetCore" Version="8.2.1" />
```

## Configuration

### Program.cs Setup

The metrics middleware is configured in the `Program.cs` file with the following components:

#### Using Directive
```csharp
using Prometheus;
```

#### Metrics Middleware Registration
```csharp
// Add Prometheus metrics middleware
app.UseMetricServer();
app.UseHttpMetrics();
```

## Components

### 1. Metric Server (`UseMetricServer()`)

- **Purpose**: Exposes a `/metrics` endpoint that Prometheus can scrape
- **Default Port**: 1234 (configurable)
- **Endpoint**: `http://localhost:1234/metrics`
- **Functionality**:
  - Serves metrics in Prometheus exposition format
  - Includes default .NET runtime metrics
  - Thread-safe and high-performance

### 2. HTTP Metrics (`UseHttpMetrics()`)

- **Purpose**: Automatically collects HTTP request/response metrics
- **Metrics Collected**:
  - Request count by HTTP method, status code, and path
  - Request duration histograms
  - Active request gauge
  - Error rates and response times

## Default Metrics

The integration provides several built-in metrics:

### HTTP Metrics
- `http_requests_total`: Total number of HTTP requests
- `http_request_duration_seconds`: Request duration in seconds
- `http_requests_in_progress`: Number of requests currently being processed

### .NET Runtime Metrics
- Garbage collection statistics
- Thread pool information
- Memory usage
- JIT compilation metrics

## Monitoring Setup

### Prometheus Configuration

Add the following to your `prometheus.yml`:

```yaml
scrape_configs:
  - job_name: 'aicalendar-api'
    static_configs:
      - targets: ['localhost:1234']
```

### Grafana Integration

The metrics can be visualized in Grafana using pre-built dashboards or custom panels. The monitoring setup includes:

- **Prometheus**: Metrics collection
- **Grafana**: Visualization and alerting
- **Dashboards**: Pre-configured panels for API monitoring

## Usage Examples

### Accessing Metrics

```bash
# Direct access to metrics endpoint
curl http://localhost:1234/metrics
```

### Custom Metrics (Future Enhancement)

While the current implementation uses built-in metrics, custom metrics can be added:

```csharp
// Example: Custom counter
private static readonly Counter CustomOperationCounter = Metrics
    .CreateCounter("aicalendar_custom_operations_total", "Number of custom operations");

// Usage
CustomOperationCounter.Inc();
```

## Troubleshooting

### Common Issues

1. **Build Errors**: Ensure the `prometheus-net.AspNetCore` package is installed and the `using Prometheus;` directive is present.

2. **Metrics Not Appearing**: Verify that `UseMetricServer()` and `UseHttpMetrics()` are called in the correct order in the middleware pipeline.

3. **Port Conflicts**: If port 1234 is in use, configure a different port:

```csharp
app.UseMetricServer(options =>
{
    options.Port = 9090; // Custom port
});
```

## Security Considerations

- The `/metrics` endpoint should be secured in production environments
- Consider using authentication or network restrictions
- Avoid exposing sensitive application metrics

## Performance Impact

- Minimal performance overhead for built-in HTTP metrics
- Metrics collection is asynchronous and non-blocking
- Suitable for high-throughput applications

## Related Documentation

- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Setup Guide](../deployment/Grafana-Setup-Guide.md)
- [REST API Documentation](REST-API-Documentation.md)
