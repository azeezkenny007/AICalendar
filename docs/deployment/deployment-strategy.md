# Azure Container Apps Deployment Strategy Explained

This document explains the "Multiple Revision" deployment strategy used in the AICalendar project, covering concepts like Blue-Green deployment, Canary releases, and Traffic Splitting.

## 1. Single vs. Multiple Revisions

### Single Revision Mode (The Default)
In this mode, your Container App behaves like a standard web server.
- When you deploy a new image, the old one is shut down immediately.
- The new one takes over 100% of the traffic.
- **Risk:** If the new version crashes on startup, your app goes down (downtime).
- **Rollback:** You have to re-deploy the old image, which takes time.

### Multiple Revision Mode (What we enabled)
In this mode, Azure creates a **Snapshot** of your app every time you deploy.
- **Revisions:** These are immutable snapshots. `Revision 1` (Old) stays running while `Revision 2` (New) starts up.
- **Co-existence:** Both versions can run at the same time.
- **Traffic Control:** You decide who sees which version.

## 2. Key Concepts

### Active Revisions
These are the versions of your app that are currently "On".
- In our pipeline, we typically have **two** active revisions during a deployment:
  1.  **Blue (Old):** The stable version currently serving users.
  2.  **Green (New):** The new version we just built.

### Traffic Splitting
This is the "Volume Knob" for your app. You can tell Azure exactly how to route request.
- **Example:** "Send 90% of users to Blue, and 10% to Green."
- This is handled by the Azure Load Balancer automatically. Users don't know which version they are on, but their request is routed to a specific container.

### A/B Testing
This uses Traffic Splitting to test features.
- You deploy a version with a new "Buy Now" button color (Revision B).
- You send 50% of traffic to Revision A (Old) and 50% to Revision B (New).
- You compare analytics to see which one performs better.

### Staging Releases (Zero Traffic Deployments)
This constitutes deploying a new revision but giving it **0% traffic**.
- The app is running, and it has a unique URL (e.g., `myapp--revision2.azurecontainerapps.io`).
- **Public Users:** Still see the old version (100% traffic).
- **Developers/QA:** Can use the unique URL to test the new version in production before any real user sees it.

### Deployment Complexity
- **Simple:** Just replace Old with New.
- **Moderate:** Manage rules for splitting traffic (what we are doing).
- We use the pipeline to automate this complexity so you don't have to manage it manually.

## 3. Our Pipeline Workflow (Blue-Green / Canary)

Here is exactly how `azure-pipelines.yml` uses these features:

1.  **Deploy New Revision (Green):**
    - We run `az containerapp update`. This creates the new revision.
    - **Traffic:** Azure defaults to giving it 100%, but we intercept this.

2.  **Health Check:**
    - The pipeline waits for the new revision to report "I am healthy".
    - If it fails, the pipeline stops. The mechanism simply cleans up the bad revision. **Zero user impact.**

3.  **Canary Release (10%):**
    - The pipeline runs: `az containerapp ingress traffic set --revision-weight Old=90 New=10`.
    - Now, 1 in 10 requests goes to the new code.
    - We wait (e.g., 2 minutes) to see if errors spike.

4.  **Full Rollout (100%):**
    - If the Canary phase passes, we run: `az containerapp ingress traffic set --revision-weight New=100`.
    - Now everyone is on the new version.

5.  **Deactivation:**
    - We turn off the old revisions to save money, but we keep them in the history list in case we need to "Reactivate" them later.

## 4. Rollback Capability
Because `Revision 1` (Old) was never deleted, just deactivated (or given 0% traffic):
- **To Rollback:** We simply set the traffic dial: `Revision 1 = 100%`, `Revision 2 = 0%`.
- **Speed:** This happens in **seconds**, because the container image doesn't need to be re-downloaded or re-built. It just switches the routing rules.
