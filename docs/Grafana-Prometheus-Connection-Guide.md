# Connecting Grafana to Prometheus in AICalendar Dashboard

This guide provides step-by-step instructions on how to connect Grafana to Prometheus as a data source and create dashboards to visualize your AICalendar metrics.

## Overview

Grafana connects to Prometheus to query and visualize time-series data. In the AICalendar setup, Prometheus collects metrics from your application, and Grafana provides the dashboard interface to display this data.

## Prerequisites

- AICalendar services are running (API, Database, Redis, Prometheus, Grafana)
- Access to Grafana web interface at http://localhost:3000
- Prometheus running at http://localhost:9090

## Step 1: Access Grafana

1. Open your web browser and navigate to `http://localhost:3000`
2. Log in with the default credentials:
   - Username: `admin`
   - Password: `admin` (or the value set in your `.env` file for `GRAFANA_ADMIN_PASSWORD`)

## Step 2: Add Prometheus as a Data Source

### Option A: Manual Configuration (if not auto-provisioned)

1. In Grafana, click on the **Configuration** gear icon (⚙️) in the left sidebar
2. Select **Data Sources**
3. Click **Add data source**
4. Search for and select **Prometheus**
5. Configure the following settings:
   - **Name**: `Prometheus` (or any descriptive name)
   - **URL**: `http://prometheus:9090` (use container name for internal networking)
   - **Access**: `Server` (default)
6. Click **Save & Test** to verify the connection

### Option B: Verify Auto-Provisioning

If your setup uses provisioning (as configured in `monitoring/grafana/provisioning/datasources/prometheus.yml`), the data source should already be configured automatically.

1. Go to **Configuration** → **Data Sources**
2. You should see **Prometheus** listed
3. Click on it to verify the connection status

## Step 3: Create Your First Dashboard

1. Click on the **+** icon in the left sidebar
2. Select **Dashboard**
3. Click **Add a new panel**

## Step 4: Query Prometheus Metrics

### Basic Query Structure

In the query editor of your new panel:

1. Select **Prometheus** as the data source
2. Enter a PromQL query in the **Metrics** field

### Common AICalendar Metrics to Query

#### API Response Time
```
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket{job="aicalendar-api"}[5m]))
```
- Shows 95th percentile response time over 5-minute windows

#### HTTP Request Rate
```
rate(http_requests_total{job="aicalendar-api"}[5m])
```
- Shows request rate per second

#### Database Connections
```
sqlserver_connections_total
```
- Shows total database connections (if SQL Server metrics are enabled)

#### Application Uptime
```
up{job="aicalendar-api"}
```
- Shows if the application is up (1) or down (0)

#### Error Rate
```
rate(http_requests_total{job="aicalendar-api", status=~"5.."}[5m]) / rate(http_requests_total{job="aicalendar-api"}[5m]) * 100
```
- Shows percentage of 5xx errors

## Step 5: Configure Panel Visualization

1. **Panel Title**: Give your panel a descriptive name (e.g., "API Response Time")
2. **Visualization Type**: Choose appropriate chart type:
   - **Graph**: For time-series data
   - **Stat**: For single values
   - **Table**: For tabular data
   - **Gauge**: For percentage/progress indicators

3. **Field Settings**:
   - **Unit**: Select appropriate unit (e.g., seconds, percent, requests/sec)
   - **Decimals**: Set decimal precision

4. **Thresholds**: Set color thresholds for alerts

## Step 6: Save the Dashboard

1. Click **Apply** to save the panel
2. Click **Save dashboard** (disk icon) in the top right
3. Enter a dashboard name (e.g., "AICalendar Monitoring")
4. **Create or select a folder**:
   - Click the **Folder** dropdown
   - To create a new folder:
     - Select **New folder** from the dropdown
     - Enter a folder name (e.g., "AICalendar Dashboards")
     - Click **Create**
   - To use an existing folder:
     - Select the desired folder from the dropdown
5. Click **Save**

## Step 7: Import Pre-built Dashboards

### Using Dashboard JSON

1. Go to **+** → **Dashboard** → **Import**
2. Upload the `monitoring/grafana/dashboards/aicalendar-overview.json` file
3. Select **Prometheus** as the data source
4. Click **Import**

### From Grafana Dashboard Gallery

1. Go to **+** → **Dashboard** → **Import**
2. Search for dashboard ID or name
3. Popular IDs for ASP.NET Core:
   - 10427: ASP.NET Core Metrics
   - 10915: .NET Core Metrics

## Step 8: Set Up Dashboard Variables (Optional)

For dynamic dashboards:

1. Go to dashboard settings (gear icon)
2. Add variables under **Variables**
3. Example: Create a "job" variable to filter by service

## Step 9: Configure Refresh and Time Range

1. **Auto-refresh**: Set dashboard to refresh every 30s or 1m
2. **Time range**: Set default time range (e.g., last 1 hour)
3. **Relative time**: Use relative time ranges for consistent views

## Step 10: Add Alerts (Optional)

1. In a panel, click **Edit** → **Alert**
2. Set alert conditions (e.g., response time > 2 seconds)
3. Configure notification channels
4. Save the alert

## Step 11: Test Prediction Endpoints and Monitor Metrics

### Testing Prediction API Endpoints

1. **Create test prediction data first**:
   ```bash
   curl -X POST "http://localhost:8080/api/predictions/test-seed" \
        -H "accept: application/json" \
        -H "Content-Type: application/json"
   ```
   This creates sample prediction data for testing.

2. **Test the predictions endpoint using curl**:
   ```bash
   curl -X GET "http://localhost:8080/api/predictions/user/123e4567-e89b-12d3-a456-426614174000" \
        -H "accept: application/json"
   ```

3. **Or test in browser**: Open `http://localhost:8080/api/predictions/user/123e4567-e89b-12d3-a456-426614174000`

4. **Generate traffic for monitoring**: Run multiple requests to generate metrics:
   ```bash
   for i in {1..10}; do
     curl -X GET "http://localhost:8080/api/predictions/user/123e4567-e89b-12d3-a456-426614174000" \
          -H "accept: application/json" -s -o /dev/null -w "Request $i: %{http_code}\n"
     sleep 2
   done
   ```

### Check Metrics in Prometheus

1. **Access Prometheus**: Open `http://localhost:9090` in your browser

2. **Query prediction endpoint metrics**:
   - **Total requests to predictions endpoints**:
     ```
     http_requests_total{job="aicalendar-api", path=~"/api/predictions.*"}
     ```
   - **Response time for predictions endpoints**:
     ```
     histogram_quantile(0.95, rate(http_request_duration_seconds_bucket{job="aicalendar-api", path=~"/api/predictions.*"}[5m]))
     ```
   - **Requests by HTTP method**:
     ```
     rate(http_requests_total{job="aicalendar-api", path=~"/api/predictions.*"}[5m]) by (method)
     ```
   - **Error rate for predictions**:
     ```
     rate(http_requests_total{job="aicalendar-api", path=~"/api/predictions.*", status=~"5.."}[5m]) / rate(http_requests_total{job="aicalendar-api", path=~"/api/predictions.*"}[5m]) * 100
     ```

3. **View target status**: Go to **Status** → **Targets** to ensure Prometheus is scraping your API

4. **Explore raw metrics**: Visit `http://localhost:8080/metrics` to see all exposed metrics

### Visualize in Grafana

1. **Create a "Predictions API Monitoring" dashboard**

2. **Add panels for prediction endpoints**:

   **Panel 1: Predictions API Request Rate**
   - Query: `rate(http_requests_total{job="aicalendar-api", path=~"/api/predictions.*"}[5m])`
   - Title: "Predictions API Request Rate"
   - Unit: "requests/sec"

   **Panel 2: Predictions Response Time**
   - Query: `histogram_quantile(0.95, rate(http_request_duration_seconds_bucket{job="aicalendar-api", path=~"/api/predictions.*"}[5m]))`
   - Title: "95th Percentile Response Time - Predictions"
   - Unit: "seconds"

   **Panel 3: Predictions by HTTP Method**
   - Query: `rate(http_requests_total{job="aicalendar-api", path=~"/api/predictions.*"}[5m]) by (method)`
   - Title: "Requests by HTTP Method - Predictions"
   - Visualization: "Bar chart"

   **Panel 4: Predictions Error Rate**
   - Query: `rate(http_requests_total{job="aicalendar-api", path=~"/api/predictions.*", status=~"5.."}[5m]) / rate(http_requests_total{job="aicalendar-api", path=~"/api/predictions.*"}[5m]) * 100`
   - Title: "Predictions API Error Rate (%)"
   - Unit: "percent"

   **Panel 5: Most Active Prediction Endpoints**
   - Query: `rate(http_requests_total{job="aicalendar-api", path=~"/api/predictions.*"}[5m]) by (path)`
   - Title: "Requests by Prediction Endpoint"
   - Visualization: "Table"

3. **Set dashboard time range**: Use "Last 15 minutes" to see recent test activity

4. **Enable auto-refresh**: Set to refresh every 30 seconds to see live metrics

### Troubleshooting Prediction Metrics

**No prediction metrics appearing?**
- Ensure you've created test data with the POST /api/predictions/test-seed endpoint
- Check that you're hitting the correct user ID in GET requests
- Verify the path regex in PromQL queries matches your endpoint patterns

**Metrics not updating?**
- Confirm the application is running and accessible
- Check Prometheus scrape interval (default: 15s)
- Ensure the /metrics endpoint is enabled in your ASP.NET Core app

**Grafana showing old data?**
- Refresh the dashboard manually
- Check if the time range includes when you made the requests
- Verify Prometheus data source is connected and healthy

## Troubleshooting Connection Issues

### Prometheus Not Accessible

**Symptoms**: "Data source connected, but no data" or connection errors

**Solutions**:
1. Check if Prometheus container is running: `docker compose ps`
2. Verify Prometheus URL in data source settings
3. Check Prometheus logs: `docker compose logs prometheus`
4. Ensure network connectivity between containers

### No Metrics Appearing

**Symptoms**: Empty graphs or "No data" messages

**Solutions**:
1. Verify metrics are being exposed by your application
2. Check Prometheus targets: Go to http://localhost:9090/targets
3. Ensure correct job names in queries
4. Check metric names in Prometheus: http://localhost:9090/graph

### Authentication Issues

**Symptoms**: "Authentication failed" errors

**Solutions**:
1. Verify Grafana admin credentials
2. Check if Prometheus requires authentication
3. Ensure correct data source URL format

## Advanced Configuration

### Custom PromQL Queries

#### Request Duration by Endpoint
```
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket{job="aicalendar-api"}[5m])) by (path)
```

#### Error Rate by Status Code
```
rate(http_requests_total{job="aicalendar-api"}[5m]) by (status)
```

#### Memory Usage
```
process_resident_memory_bytes{job="aicalendar-api"} / 1024 / 1024
```

### Dashboard Templates

Create reusable dashboard templates using variables:

```
rate(http_requests_total{job="$job"}[5m])
```

## Best Practices

1. **Naming Conventions**: Use consistent naming for panels and dashboards
2. **Color Schemes**: Use consistent colors for similar metrics
3. **Documentation**: Add descriptions to panels and dashboards
4. **Organization**: Group related panels in rows
5. **Performance**: Limit query time ranges for better performance
6. **Updates**: Regularly update Grafana and Prometheus versions

## Next Steps

- Explore more visualization options in Grafana
- Set up alerting for critical metrics
- Create custom dashboards for business-specific KPIs
- Integrate with other monitoring tools

## Resources

- [Grafana Documentation](https://grafana.com/docs/grafana/latest/)
- [Prometheus Query Language](https://prometheus.io/docs/prometheus/latest/querying/basics/)
- [Grafana Dashboard Examples](https://grafana.com/grafana/dashboards/)
- [ASP.NET Core Metrics Guide](https://docs.microsoft.com/en-us/aspnet/core/log-monitors/metrics)
