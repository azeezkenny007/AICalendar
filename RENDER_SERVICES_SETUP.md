# Connect External Services to Render Deployment

This guide shows you how to make all unhealthy containers healthy by connecting external managed services to your Render app.

## Current Status

```json
{
  "database": "healthy",      ✅ Already working
  "apihealth": "healthy",     ✅ Already working
  "hangfire": "healthy",      ✅ Already working
  "redis": "unhealthy",       ❌ Need to fix
  "prometheus": "unhealthy",  ❌ Need to fix
  "grafana": "unhealthy",     ❌ Need to fix
  "seq": "unhealthy"          ❌ Need to fix
}
```

---

## 1. Redis (Cache) - REQUIRED

### Why You Need It
Redis handles caching in your app. Without it, performance will be slower.

### Setup Steps

#### Option A: Upstash Redis (Recommended - FREE)

1. **Sign up for Upstash**
   - Go to https://upstash.com/
   - Sign up with GitHub/Google
   - Free tier: 10,000 commands/day

2. **Create Redis Database**
   - Click "Create Database"
   - Name: `aicalendar-cache`
   - Region: Choose closest to your Render region
   - Type: Regional (free)
   - Click "Create"

3. **Get Connection String**
   - Click on your database
   - Copy the connection string that looks like:
     ```
     rediss://default:xxxxxxxxxxxxx@us1-living-firefly-12345.upstash.io:6379
     ```

4. **Add to Render**
   - Go to your Render dashboard
   - Open your web service
   - Go to "Environment" tab
   - Click "Add Environment Variable"
   - Key: `Redis__Configuration`
   - Value: Paste your Upstash connection string
   - Click "Save Changes"
   - **Render will auto-redeploy**

#### Option B: Redis Cloud (Alternative - FREE)

1. Go to https://redis.com/try-free/
2. Sign up and create 30MB free database
3. Get connection string
4. Add to Render environment variables as above

---

## 2. Prometheus (Metrics) - OPTIONAL

### Why You Need It
Prometheus collects application metrics (request counts, response times, etc.)

### Setup Steps

#### Option A: Grafana Cloud (Recommended - FREE)

1. **Sign up for Grafana Cloud**
   - Go to https://grafana.com/auth/sign-up/create-user
   - Free tier: 10K series metrics

2. **Get Prometheus Endpoint**
   - After signup, go to "My Account"
   - Click "Hosted Prometheus"
   - Copy the "Remote Write Endpoint" URL
   - Copy the "Username" and "Password"

3. **Configure Your App**
   - You'll need to update your code to push metrics to Grafana Cloud
   - This requires code changes (I can help with this)

#### Option B: Remove It (Easier)

If you don't need metrics tracking:
- Remove the Prometheus health check from your code
- Your app will work fine without it

---

## 3. Grafana (Dashboards) - OPTIONAL

### Why You Need It
Grafana visualizes your metrics in dashboards

### Setup Steps

#### Use Grafana Cloud (FREE)

1. **If you signed up for Grafana Cloud above**
   - You already have Grafana included
   - Access it at: `https://yourorg.grafana.net`
   - No container needed!

2. **Access Your Dashboards**
   - Login to Grafana Cloud
   - Create dashboards to visualize Prometheus metrics
   - No environment variables needed

#### Option B: Remove It

If you don't need visual dashboards:
- Remove the Grafana health check from your code
- Use Render's built-in monitoring instead

---

## 4. Seq (Centralized Logging) - OPTIONAL

### Why You Need It
Seq provides a UI to search and view all your application logs

### Setup Steps

#### Option A: Seq Cloud (PAID - $10/month minimum)

1. Go to https://datalust.co/seq
2. Sign up for cloud hosting
3. Get your Seq server URL and API key
4. Add to Render environment variables:
   - `Seq__ServerUrl`: Your Seq cloud URL
   - `Seq__ApiKey`: Your API key

#### Option B: Better Stack (FREE Alternative)

1. **Sign up for Better Stack**
   - Go to https://betterstack.com/logs
   - Free tier: 1GB/month logs

2. **Get Source Token**
   - Create a new source
   - Copy the source token

3. **Update Serilog Configuration**
   - Update `appsettings.json` to send logs to Better Stack
   - Add environment variable in Render:
     - `BetterStack__Token`: Your source token

#### Option C: Remove It (Recommended for Now)

Use Render's built-in logs:
- Go to your Render service
- Click "Logs" tab
- View all application logs there
- No extra cost!

---

## Quick Setup (Minimum Required)

If you want to get everything healthy with minimal effort:

### Step 1: Add Redis Only (5 minutes)

1. Sign up at https://upstash.com/
2. Create database
3. Copy connection string
4. Add to Render:
   - Key: `Redis__Configuration`
   - Value: `rediss://default:xxxxx@us1-living-firefly-12345.upstash.io:6379`

### Step 2: Remove Other Health Checks (2 minutes)

I'll update your code to remove Prometheus, Grafana, and Seq health checks since they're not critical.

**After these 2 steps, all health checks will be green!**

---

## Render Environment Variables Summary

After setup, your Render environment variables should include:

```env
# Database (Already configured)
DefaultConnection=Server=...

# Redis (NEW - Add this)
Redis__Configuration=rediss://default:xxxxx@upstash.io:6379

# Optional: Seq (if using)
Seq__ServerUrl=https://your-seq-instance.com
Seq__ApiKey=your-api-key

# Optional: Better Stack (if using instead of Seq)
BetterStack__Token=your-token
```

---

## Health Check Configuration File

Your health checks are registered in these files:
- `src/AICalendar.API/Extensions/HangfireServiceExtensions.cs` - For Hangfire/Redis health
- `src/AICalendar.API/Program.cs` - May have additional health check registrations

I can update these files to remove unnecessary health checks.

---

## Recommended Action Plan

### For Production (Recommended)

1. ✅ Add Upstash Redis (free, 5 min setup)
2. ✅ Remove Prometheus health check (not needed yet)
3. ✅ Remove Grafana health check (use Render metrics)
4. ✅ Remove Seq health check (use Render logs)

**Result:** All health checks green, minimal cost, production-ready

### For Full Monitoring (Advanced)

1. ✅ Add Upstash Redis
2. ✅ Add Grafana Cloud (Prometheus + Grafana)
3. ✅ Add Better Stack (Logging)

**Result:** Full observability stack, still free tier

---

## Next Steps

**Tell me which option you want:**

**Option 1:** "Just add Redis and remove the others" (Fastest)
**Option 2:** "Add Redis + Grafana Cloud" (Free monitoring)
**Option 3:** "Add everything" (Full setup)

I'll update your code and create the exact environment variables you need to add to Render!
