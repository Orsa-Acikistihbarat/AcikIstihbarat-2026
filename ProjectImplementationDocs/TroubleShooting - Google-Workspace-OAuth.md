# Operational Guide: Google Workspace OAuth2 Configuration & Expiration Prevention

**Document ID:** DOC-OPS-GMAIL-OAUTH  
**Date:** 2026-09-28  
**Scope:** `bilgi@acikistihbarat.com` (Google Workspace Account) & Google Cloud Console  

---

## 1. Background: Why OAuth Tokens Expire

Google Cloud Platform (GCP) enforces strict security policies on OAuth2 refresh tokens:

| App Configuration | Account Type | Refresh Token Lifetime | Action Needed |
|-------------------|--------------|------------------------|---------------|
| **External + "Testing"** | Personal or Workspace | **Exactly 7 Days** (Auto-revoked by Google) | ⚠️ Must be converted to Internal or Production |
| **External + "In production"** | Personal or Workspace | Perpetual (unless revoked by user or unused for 6 months) | Safe, but may require unverified app screen |
| **Internal (Recommended)** | **Google Workspace Only** | **Perpetual / Non-Expiring** | ✅ Best Practice for AcikIstihbarat |

Because `bilgi@acikistihbarat.com` is a **Google Workspace** organization account, you have access to the **"Internal" User Type**.

---

## 2. Step-by-Step Verification Checklist in Google Cloud Console

### Step 1: Open OAuth Consent Screen
1. Open your browser and go to [Google Cloud Console](https://console.cloud.google.com/).
2. Log in using an administrator account for `acikistihbarat.com`.
3. In the top project selector dropdown, ensure your **AcikIstihbarat** project is selected.
4. From the left sidebar, navigate to:  
   **APIs & Services** > **OAuth consent screen** (veya *OAuth onay ekranı*).

### Step 2: Verify and Configure User Type
* Look at the **User Type** section:
  * **If User Type is "Internal" (Dahili):**  
    ✅ **You are fully protected.** Google will never expire your refresh token after 7 days.
  * **If User Type is "External" (Harici):**  
    Check the **Publishing status** (Yayın durumu):
    * If status is **"Testing"** (Test ediliyor):  
      ⚠️ **This was the cause of token invalidation after 7 days.**
      * **Action Option A (Best):** Click **"Make Internal"** (Dahili Yap). Since you are on Google Workspace, this restricts the app to your domain and eliminates the 7-day limit completely.
      * **Action Option B:** Click **"Publish App"** (Uygulamayı Yayınla) to move it to Production status.

### Step 3: Verify OAuth Scopes
Under the **Scopes** (Kapsamlar) tab, verify that the following scope is added:
* `https://mail.google.com/` (Read, compose, send, and permanently delete all your email from Gmail)

### Step 4: Verify Authorized Redirect URIs
Navigate to **APIs & Services** > **Credentials** (Kimlik Bilgileri):
* Click on your OAuth 2.0 Client ID (e.g. `AcikIstihbarat-API` or Web Client).
* Under **Authorized redirect URIs** (Yetkili yönlendirme URI'leri), confirm:
  * `http://localhost:8721/` (Used by `scratch/GmailOAuthBootstrap/Program.cs` during bootstrap).

---

## 3. Google Workspace Admin Console (admin.google.com) Check

To ensure Google Workspace security policies do not periodically revoke the token:
1. Go to [Google Admin Console](https://admin.google.com/).
2. Navigate to **Security** > **Access and data control** > **API controls** (*API kontrolleri*).
3. Under **App access control** (*Uygulama erişim denetimi*):
   * Locate your OAuth App (by Client ID).
   * Ensure access is marked as **Trusted** (*Güvenilen*).

---

## 4. Re-issuing the Refresh Token (If Ever Needed)

If the token was ever revoked or needs to be refreshed:
1. Run the local bootstrap tool in `scratch/GmailOAuthBootstrap`:
   ```powershell
   dotnet run --project c:\Belgelerim\yazilim\calisma-projeleri\AcikIstihbarat-2026\scratch\GmailOAuthBootstrap
   ```
2. Log in with `bilgi@acikistihbarat.com` and grant permissions.
3. The tool will print the new `MAIL_GMAIL_REFRESH_TOKEN` to paste into your production `.env` file.
4. Restart the API container:
   ```bash
   docker compose restart api
   ```
