# Monitoring Architecture: Node Exporter, API, Prometheus, and Grafana Integration

## Overview

The AICalendar monitoring stack consists of four key components working together to provide comprehensive observability: Node Exporter, the AICalendar API, Prometheus, and Grafana. This document explains how these components interact to collect, store, and visualize metrics.

## Components

### 1. Node Exporter

**Purpose**: Collects system-level metrics from the host machine.

**Role in the Stack**:
- Runs as a Docker container (`prom/node-exporter:latest`)
- Exposes metrics on port 9100
- Collects hardware and OS metrics such as CPU usage, memory, disk I/O, network statistics, and filesystem information

**Key Metrics**:
- `node_cpu_seconds_total`: CPU time spent in different modes
- `node_memory_MemTotal_bytes`: Total memory available
- `node_memory_MemAvailable_bytes`: Available memory
- `node_disk_io_time_seconds_total`: Disk I/O time
- `node_filesystem_avail_bytes`: Available filesystem space

### 2. AICalendar API

**Purpose**: The main application service that exposes both business logic and application metrics.

**Role in the Stack**:
- Built with .NET 8 and ASP.NET Core
- Uses `prometheus-net.AspNetCore` package for metrics collection
- Exposes metrics endpoint at `/metrics` on port 8080
- Collects HTTP request metrics automatically via middleware

**Key Metrics**:
- `http_requests_total`: Total number of HTTP requests (labeled by method, status code, path)
- `http_request_duration_seconds`: Request duration histograms
- `http_requests_in_progress`: Number of active requests
- .NET runtime metrics (GC, thread pools, memory usage)

### 3. Prometheus

**Purpose**: Time-series database that scrapes and stores metrics from various sources.

**Role in the Stack**:
- Runs as a Docker container (`prom/prometheus:latest`)
- Scrapes metrics from configured targets at regular intervals
- Stores metrics with timestamps for historical analysis
- Provides a query language (PromQL) for data retrieval

**Configuration** (`monitoring/prometheus.yml`):
```yaml
scrape_configs:
  - job_name: 'aicalendar-api'
    static_configs:
      - targets: ['api:8080']
    metrics_path: '/metrics'

  - job_name: 'node-exporter'
    static_configs:
      - targets: ['node-exporter:9100']
```

**Scraping Intervals**:
- API metrics: Every 5 seconds
- Node Exporter metrics: Every 15 seconds (global default)

### 4. Grafana

**Purpose**: Visualization and dashboard platform for metrics data.

**Role in the Stack**:
- Runs as a Docker container (`grafana/grafana:latest`)
- Connects to Prometheus as a data source
- Provides pre-built and custom dashboards for monitoring
- Supports alerting based on metric thresholds

**Key Features**:
- Pre-configured AICalendar Overview dashboard
- Panels for API response times, request rates, system resources
- Custom queries using PromQL
- User authentication and multi-user support

## Data Flow

1. **Collection**:
   - Node Exporter collects system metrics from the host
   - API middleware automatically collects HTTP metrics
   - Both expose metrics via HTTP endpoints

2. **Scraping**:
   - Prometheus periodically fetches metrics from Node Exporter and API
   - Metrics are stored with timestamps in Prometheus's time-series database

3. **Querying**:
   - Grafana queries Prometheus using PromQL
   - Data is processed and visualized in dashboards

4. **Visualization**:
   - Users access Grafana dashboards to monitor system health
   - Dashboards show real-time and historical metrics

## How Prometheus Gets Data from Node Exporter and API

Prometheus operates on a pull-based model, actively scraping metrics from configured targets rather than waiting for them to be pushed. This section details the scraping process for both Node Exporter and the AICalendar API.

### Scraping Configuration

The scraping behavior is defined in `monitoring/prometheus.yml`:

```yaml
global:
  scrape_interval: 15s  # Default interval for all jobs

scrape_configs:
  - job_name: 'aicalendar-api'
    static_configs:
      - targets: ['api:8080']  # API container and port
    scrape_interval: 5s        # Override global interval
    metrics_path: '/metrics'   # Path to metrics endpoint

  - job_name: 'node-exporter'
    static_configs:
      - targets: ['node-exporter:9100']  # Node Exporter container and port
    # Uses global scrape_interval: 15s
```

### API Metrics Scraping

1. **Target Discovery**: Prometheus identifies the API as a target via `targets: ['api:8080']`
2. **Endpoint Access**: Makes HTTP GET requests to `http://api:8080/metrics` every 5 seconds
3. **Data Retrieval**: Parses the Prometheus exposition format response containing:
   - HTTP request counts by status code, method, and path
   - Request duration histograms
   - Active request gauges
   - .NET runtime metrics
4. **Storage**: Stores each metric with timestamp, labels, and value in its time-series database

### Node Exporter Metrics Scraping

1. **Target Discovery**: Prometheus identifies Node Exporter via `targets: ['node-exporter:9100']`
2. **Endpoint Access**: Makes HTTP GET requests to `http://node-exporter:9100/metrics` every 15 seconds
3. **Data Retrieval**: Parses metrics including:
   - CPU usage statistics
   - Memory and swap information
   - Disk I/O and filesystem metrics
   - Network interface statistics
   - System load averages
4. **Storage**: Stores metrics with appropriate labels (e.g., `instance`, `cpu`, `device`) for querying

### Scraping Process Details

- **HTTP Requests**: Prometheus sends GET requests to the `/metrics` endpoints
- **Format Parsing**: Expects responses in Prometheus text-based exposition format
- **Label Addition**: Automatically adds labels like `job`, `instance`, and `__name__`
- **Timestamping**: Records the scrape timestamp for each metric
- **Error Handling**: Marks targets as down if scraping fails, with retry logic
- **Relabeling**: Can modify labels before storage using relabeling rules

### Network Communication in Docker

Within the Docker Compose network (`aicalendar-network`):
- Prometheus container communicates with `api` and `node-exporter` services using their container names
- No external ports are exposed for scraping (internal network only)
- Service discovery is static (hardcoded targets) rather than dynamic

### Monitoring Scraping Health

You can verify scraping status by:
- Accessing Prometheus UI at `http://localhost:9090/targets` to see target health
- Checking container logs: `docker logs aicalendar-prometheus`
- Querying scrape metrics: `up{job="aicalendar-api"}` (1 = up, 0 = down)

## Integration Benefits

- **Comprehensive Monitoring**: Covers both application and infrastructure metrics
- **Real-time Insights**: Low-latency metric collection and visualization
- **Scalability**: Prometheus can handle high-volume metrics efficiently
- **Alerting**: Grafana can trigger alerts based on metric thresholds
- **Historical Analysis**: Time-series data enables trend analysis and debugging

## Docker Compose Integration

All components are orchestrated via `docker-compose.yml`:

- **Networks**: All services communicate via `aicalendar-network`
- **Dependencies**: Grafana depends on Prometheus; API depends on database and Redis
- **Health Checks**: Services include health checks for reliability
- **Volumes**: Persistent data for databases and monitoring tools

## Usage Examples

### Accessing Metrics Directly

```bash
# API metrics
curl http://localhost:8080/metrics

# Node Exporter metrics
curl http://localhost:9100/metrics

# Prometheus UI
open http://localhost:9090

# Grafana UI
open http://localhost:3000
```

### Common Monitoring Queries

- **API Success Rate**: `rate(http_requests_total{status=~"2.."}[5m])`
- **CPU Usage**: `100 - (avg by(instance) (irate(node_cpu_seconds_total{mode="idle"}[5m])) * 100)`
- **Memory Usage**: `100 - ((node_memory_MemAvailable_bytes / node_memory_MemTotal_bytes) * 100)`

## Troubleshooting

### Common Issues

1. **Metrics Not Appearing**: Check service health and network connectivity
2. **High Latency**: Verify scrape intervals and resource allocation
3. **Data Gaps**: Ensure targets are reachable and metrics endpoints are responding

### Logs and Debugging

- Check Docker container logs: `docker logs <container_name>`
- Prometheus targets page: `http://localhost:9090/targets`
- Grafana data source health: Check in Grafana UI

## Security Considerations

- Metrics endpoints may expose sensitive information
- Secure Grafana with strong passwords and network restrictions
- Consider authentication for production deployments
- Limit Prometheus and Grafana exposure to trusted networks

## Related Documentation

- [Prometheus Metrics Integration](Prometheus-Metrics-Integration.md)
- [Grafana Setup Guide](../../deployment/Grafana-Setup-Guide.md)
- [Docker Compose Setup](../../docker-compose.yml)
