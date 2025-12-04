# Quick Testing Guide for Firebase Notifications

## 1. Test FCM Token Registration (2 min)

### Backend Check:
```sql
-- Check if token is saved in database
SELECT Id, Email, FcmDeviceToken FROM Users WHERE Email = 'your@email.com'
```

### App Check:
- **Android**: Check Logcat for `FCM Token: ...`
- **iOS**: Check Xcode console for `FCM Token: ...`

---

## 2. Send Test Notification via Backend (1 min)

```bash
# Replace YOUR_USER_ID with actual GUID from database
curl -X POST https://localhost:7000/api/users/test-notification/YOUR_USER_ID
```

**Should see notification on your device immediately!**

---

## 3. Send Test via Firebase Console (1 min)

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Click **Cloud Messaging** (left menu)
3. Click **Send your first message**
4. **Notification title**: "Test"
5. **Notification text**: "Testing push notification"
6. Click **Send test message**
7. Paste your FCM token (from app logs)
8. Click **Test**

**Notification should appear on device!**

---

## 4. Test Reminder Job (2 min)

### Create Calendar Item with Payment Due Soon:
```sql
INSERT INTO CalendarItems (UserId, Title, PaymentDue, Amount, IsPaid)
VALUES ('YOUR_USER_ID', 'Test Payment', DATEADD(HOUR, 23, GETDATE()), 100.00, 0)
```

### Manually Trigger Job:
1. Go to Hangfire Dashboard: `https://localhost:7000/hangfire`
2. Click **Recurring jobs**
3. Find **send-reminders**
4. Click **Trigger now**

**Check device for reminder notification!**

---

## 5. Quick Troubleshooting

### No notification received?

**Check:**
1. FCM token in database? → `SELECT FcmDeviceToken FROM Users`
2. Backend logs → Look for "Successfully sent notification"
3. Firebase credentials file exists? → `firebase-credentials.json`
4. Permissions granted on device?

### Working? You'll see:
- Backend log: `Successfully sent notification to user {UserId}`
- Device: Notification appears
- Tap notification → App opens

---

## Complete Test Sequence

**Test in this order:**

1. **Login** → Token saved ✓
2. **Backend test** → Notification ✓
3. **Firebase Console** → Notification ✓
4. **Reminder job** → Notification ✓

**Total time: ~5 minutes**

---

## Platform-Specific Testing

### Android (Kotlin)

**Check Logs:**
```bash
adb logcat | grep FCM
```

**Verify:**
- Permission granted (Android 13+)
- Notification channel created
- `google-services.json` in place

### iOS (Swift)

**Check Logs:**
- Xcode Console → Look for "FCM Token"
- Look for "Notification permission granted"

**Verify:**
- Physical device (not simulator)
- Push Notifications capability enabled
- APNs certificate uploaded to Firebase

---

## Expected Results

### After Login:
```
Android Logcat: FCM Token: fj3k4l5j6k...
iOS Console: FCM Token: Optional("fj3k4l5j6k...")
Database: FcmDeviceToken = "fj3k4l5j6k..."
```

### After Test Notification:
```
Backend Log: Successfully sent notification to user {UserId}. FCM Response: projects/.../messages/...
Device: Notification appears with title and message
```

### After Reminder Job:
```
Hangfire: SendRemindersJob executed
Backend Log: Found 1 unpaid items due in 24 hours
Backend Log: Successfully sent notification
Device: "Payment Due Soon" notification
```

---

## Common Issues

### Issue: No token in database
**Solution:**
- Check app logs for errors
- Verify `NotificationManager.initialize()` is called after login
- Check network connectivity

### Issue: Token exists but no notification
**Solution:**
- Verify `firebase-credentials.json` exists in backend
- Check Firebase Console → Cloud Messaging → Usage stats
- Look for errors in backend logs

### Issue: Notification sent but not received
**Solution:**
- **Android**: Check battery optimization settings
- **iOS**: Ensure not in Do Not Disturb mode
- Verify permissions granted on device

---

**Done! Your Firebase notifications are working!** 🎉
