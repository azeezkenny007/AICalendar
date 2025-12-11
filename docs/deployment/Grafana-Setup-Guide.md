# Grafana Setup Guide for AICalendar

This guide explains how to add Grafana monitoring and visualization to the AICalendar Docker configuration.

## Overview

Grafana is a powerful open-source platform for monitoring and observability that provides:

- **Dashboards and Data Visualization**: Create interactive dashboards with charts, graphs, and alerts
- **Monitoring Applications and Infrastructure**: Track application metrics, database performance, and system health
- **Error Monitoring**: Visualize startup/config errors, database errors, HTTP/router errors, data source/query errors, plugin/integration errors, and runtime/fatal errors

## Prerequisites

- Docker and Docker Compose installed
- Existing AICalendar docker-compose.yml setup
- Basic understanding of monitoring concepts

## Architecture

```
AICalendar API ──┐
                 ├── Prometheus (Metrics Collection)
                 │
Database ────────┤
                 │
Redis ──────────┼── Grafana (Visualization & Dashboards)
                 │
Infrastructure ─┘
```

## Step 1: Add Grafana to docker-compose.yml

Add the following services to your existing `docker-compose.yml`:

```yaml
# Add after the redis service
  # ============================================================================
  # Prometheus - Metrics Collection
  # ============================================================================
  prometheus:
    image: prom/prometheus:latest
    container_name: aicalendar-prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus_data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/etc/prometheus/console_libraries'
      - '--web.console.templates=/etc/prometheus/consoles'
      - '--storage.tsdb.retention.time=200h'
      - '--web.enable-lifecycle'
    restart: unless-stopped
    networks:
      - aicalendar-network

  # ============================================================================
  # Grafana - Monitoring & Visualization
  # ============================================================================
  grafana:
    image: grafana/grafana:latest
    container_name: aicalendar-grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_USER=${GRAFANA_ADMIN_USER:-admin}
      - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_ADMIN_PASSWORD:-admin}
      - GF_USERS_ALLOW_SIGN_UP=false
      - GF_INSTALL_PLUGINS=grafana-piechart-panel,grafana-worldmap-panel
    volumes:
      - grafana_data:/var/lib/grafana
      - ./monitoring/grafana/provisioning:/etc/grafana/provisioning:ro
      - ./monitoring/grafana/dashboards:/var/lib/grafana/dashboards:ro
    depends_on:
      - prometheus
    restart: unless-stopped
    networks:
      - aicalendar-network
```

## Step 2: Update Volumes Section

Add the new volumes to the volumes section:

```yaml
volumes:
  # ... existing volumes ...

  # Monitoring data
  prometheus_data:
    name: aicalendar_prometheus_data
  grafana_data:
    name: aicalendar_grafana_data
```

## Step 3: Create Monitoring Directory Structure

Create the monitoring directory and configuration files:

```bash
mkdir -p monitoring/prometheus
mkdir -p monitoring/grafana/provisioning/datasources
mkdir -p monitoring/grafana/provisioning/dashboards
mkdir -p monitoring/grafana/dashboards
```

## Step 4: Configure Prometheus

Create `monitoring/prometheus.yml`:

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

rule_files:
  # - "first_rules.yml"
  # - "second_rules.yml"

scrape_configs:
  - job_name: 'aicalendar-api'
    static_configs:
      - targets: ['api:8080']
    scrape_interval: 5s
    metrics_path: '/metrics'

  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']

  - job_name: 'node-exporter'
    static_configs:
      - targets: ['node-exporter:9100']
```

## Step 5: Configure Grafana Data Sources

Create `monitoring/grafana/provisioning/datasources/prometheus.yml`:

```yaml
apiVersion: 1

datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
    editable: true
```

## Step 6: Configure Grafana Dashboards

Create `monitoring/grafana/provisioning/dashboards/dashboards.yml`:

```yaml
apiVersion: 1

providers:
  - name: 'default'
    orgId: 1
    folder: ''
    type: file
    disableDeletion: false
    updateIntervalSeconds: 10
    allowUiUpdates: true
    options:
      path: /var/lib/grafana/dashboards
```

## Step 7: Create Sample Dashboards

Create `monitoring/grafana/dashboards/aicalendar-overview.json`:

```json
{
  "dashboard": {
    "id": null,
    "title": "AICalendar Overview",
    "tags": ["aicalendar", "overview"],
    "timezone": "browser",
    "panels": [
      {
        "id": 1,
        "title": "API Response Time",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(http_request_duration_seconds_bucket{job=\"aicalendar-api\"}[5m]))",
            "legendFormat": "95th percentile"
          }
        ]
      },
      {
        "id": 2,
        "title": "Database Connections",
        "type": "graph",
        "targets": [
          {
            "expr": "sqlserver_connections_total",
            "legendFormat": "Total connections"
          }
        ]
      }
    ],
    "time": {
      "from": "now-1h",
      "to": "now"
    },
    "refresh": "5s"
  }
}
```

## Step 8: Update Environment Variables

Add to your `.env` file:

```bash
# Grafana
GRAFANA_ADMIN_USER=admin
GRAFANA_ADMIN_PASSWORD=secure_password_here

# Prometheus (if needed)
PROMETHEUS_RETENTION_TIME=200h
```

## Step 9: Add Application Metrics

Update your `Program.cs` to expose metrics:

```csharp
// Add using statements
using Prometheus;

// Add metrics middleware
app.UseMetricServer();
app.UseHttpMetrics();
```

Add to your `.csproj`:

```xml
<PackageReference Include="prometheus-net.AspNetCore" Version="8.0.1" />
```

## Step 10: Add Node Exporter (Optional)

For infrastructure monitoring, add node-exporter to docker-compose.yml:

```yaml
  node-exporter:
    image: prom/node-exporter:latest
    container_name: aicalendar-node-exporter
    ports:
      - "9100:9100"
    volumes:
      - /proc:/host/proc:ro
      - /sys:/host/sys:ro
      - /:/rootfs:ro
    command:
      - '--path.procfs=/host/proc'
      - '--path.rootfs=/rootfs'
      - '--path.sysfs=/host/sys'
      - '--collector.filesystem.mount-points-exclude=^/(sys|proc|dev|host|etc)($$|/)'
    restart: unless-stopped
    networks:
      - aicalendar-network
```

## Step 11: Start the Services

```bash
# Start all services including monitoring
docker compose up -d

# Or start only monitoring services
docker compose up -d prometheus grafana
```

## Step 12: Access Grafana

1. Open http://localhost:3000
2. Login with admin credentials
3. Add Prometheus as data source (if not auto-provisioned)
4. Import or create dashboards

## Monitoring Categories

### Application Monitoring
- API response times and throughput
- Error rates and types
- Database query performance
- Cache hit/miss ratios

### Infrastructure Monitoring
- CPU, memory, and disk usage
- Network I/O
- Container resource usage
- Database connection pools

### Business Metrics
- User registrations and logins
- Prediction accuracy rates
- Calendar item processing
- Notification delivery success

## Troubleshooting

### Common Issues

1. **Grafana can't connect to Prometheus**
   - Check network connectivity between containers
   - Verify Prometheus is running on port 9090
   - Check firewall settings

2. **No metrics appearing**
   - Ensure application has metrics endpoint enabled
   - Check Prometheus scrape configuration
   - Verify target URLs are correct

3. **Dashboard not loading**
   - Check JSON syntax in dashboard files
   - Verify provisioning configuration
   - Check Grafana logs for errors

### Useful Commands

```bash
# Check service status
docker compose ps

# View logs
docker compose logs grafana
docker compose logs prometheus

# Restart services
docker compose restart grafana prometheus

# Clean restart
docker compose down
docker compose up -d
```

## Security Considerations

1. **Change default passwords** in production
2. **Use HTTPS** for Grafana in production
3. **Restrict network access** to monitoring ports
4. **Enable authentication** and authorization
5. **Regularly update** Grafana and Prometheus images

## Next Steps

1. Create custom dashboards for your specific metrics
2. Set up alerting rules in Prometheus
3. Configure log aggregation (ELK stack)
4. Add distributed tracing (Jaeger)
5. Implement automated deployment pipelines

## Resources

- [Grafana Documentation](https://grafana.com/docs/)
- [Prometheus Documentation](https://prometheus.io/docs/)
- [ASP.NET Core Metrics](https://docs.microsoft.com/en-us/aspnet/core/log-monitors/metrics)
- [Grafana Dashboards Gallery](https://grafana.com/grafana/dashboards/)
