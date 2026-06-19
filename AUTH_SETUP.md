# Google & Microsoft Sign-In Setup

Marketplace Copilot supports **real OAuth sign-in**:

| Provider | Works with |
|----------|------------|
| **Google** | Gmail (`@gmail.com`), Google Workspace |
| **Microsoft** | Outlook, Hotmail, Live, work/school Microsoft accounts |

Until you add credentials below, the buttons appear but redirect with a setup message. **Email/password signup** still works for demo.

---

## 1. Google Sign-In (for Gmail)

1. Go to [Google Cloud Console → Credentials](https://console.cloud.google.com/apis/credentials)
2. Create a project (e.g. `Marketplace Copilot`)
3. **OAuth consent screen** → External → add your email as test user
4. **Create Credentials** → OAuth client ID → **Web application**
5. **Authorized redirect URIs** — add exactly:
   ```
   http://localhost:5280/api/auth/google/callback
   ```
6. Copy **Client ID** and **Client Secret**

---

## 2. Microsoft Sign-In (for Outlook / work accounts)

1. Go to [Azure Portal → App registrations](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps)
2. **New registration** → name: `Marketplace Copilot`
3. Redirect URI: **Web** →
   ```
   http://localhost:5280/api/auth/microsoft/callback
   ```
4. After create: **Certificates & secrets** → New client secret
5. Copy **Application (client) ID** and **secret value**

---

## 3. Add credentials to the app

Edit `MarketplaceCopilot.Api/appsettings.json`:

```json
"Auth": {
  "FrontendUrl": "http://localhost:4200",
  "Google": {
    "ClientId": "paste-google-client-id",
    "ClientSecret": "paste-google-secret"
  },
  "Microsoft": {
    "ClientId": "paste-microsoft-client-id",
    "ClientSecret": "paste-microsoft-secret"
  }
}
```

Restart backend:
```powershell
cd MarketplaceCopilot.Api
dotnet run
```

---

## 4. Test

1. Open http://localhost:4200/login
2. Click **Sign in with Google** → use your Gmail
3. Or **Sign in with Microsoft** → use Outlook/work account
4. You land on the dashboard automatically (OAuth emails are pre-verified)

---

## Flow summary

| Method | Email verification | Role |
|--------|-------------------|------|
| Google / Microsoft | Done by Google/Microsoft | Auto-approved (Sales Rep) |
| Email signup | Demo verify screen | Admin approval demo |

For production, restrict auto-approval and add real admin panel + SendGrid for local email signups.
