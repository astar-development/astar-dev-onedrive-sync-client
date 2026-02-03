# Entra ID (Azure AD) App Registration Guide for OneDrive Sync Client

This guide walks you through creating and configuring a Microsoft Entra ID (formerly Azure Active Directory) application registration to enable OAuth 2.0 authentication for the OneDrive Sync Client.

## Prerequisites

- Microsoft Azure account with permissions to create app registrations
- Access to the [Azure Portal](https://portal.azure.com)
- Understanding of OAuth 2.0 authentication flows

## Overview

The OneDrive Sync Client uses OAuth 2.0 to authenticate users and access their OneDrive files via the Microsoft Graph API. To enable this, you need to register an application in Microsoft Entra ID.

## Step 1: Create App Registration

1. **Navigate to Azure Portal**
   - Go to [Azure Portal](https://portal.azure.com)
   - Sign in with your Microsoft account

2. **Access Microsoft Entra ID**
   - In the left navigation, select **Microsoft Entra ID** (or search for it in the top search bar)
   - Select **App registrations** from the left menu
   - Click **+ New registration**

3. **Configure Basic Settings**
   - **Name**: Enter a descriptive name (e.g., "OneDrive Sync Client")
   - **Supported account types**: Select one of:
     - **Accounts in any organizational directory and personal Microsoft accounts** (Recommended for this app)
     - This allows both work/school accounts and personal Microsoft accounts
   - **Redirect URI**: Select **Public client/native (mobile & desktop)** and enter:
     ```
     http://localhost
     ```
   - Click **Register**

## Step 2: Note Your Application (Client) ID

After registration, you'll be redirected to the app's overview page.

1. **Copy the Application (client) ID**
   - This is displayed prominently on the overview page
   - Example format: `3057f494-687d-4abb-a653-4b8066230b6e`
   - **Save this value** - you'll need it for your `appsettings.json`

2. **Note the Directory (tenant) ID**
   - Also visible on the overview page
   - For personal Microsoft accounts, you'll use `"common"` instead of the tenant ID

## Step 3: Configure Authentication

1. **Navigate to Authentication**
   - In the left menu, select **Authentication**

2. **Add Additional Redirect URIs** (if needed)
   - Under **Platform configurations**, find your **Mobile and desktop applications** entry
   - Recommended additional URIs:
     ```
     http://localhost:8080
     http://localhost:3000
     http://127.0.0.1
     ```

3. **Configure Advanced Settings**
   - Under **Advanced settings**, ensure the following:
     - **Allow public client flows**: Set to **Yes**
     - This enables Device Code Flow and other native client authentication methods

4. **Save Changes**
   - Click **Save** at the top of the page

## Step 4: Configure API Permissions

1. **Navigate to API Permissions**
   - In the left menu, select **API permissions**

2. **Review Default Permissions**
   - By default, **User.Read** (Microsoft Graph) is added
   - This is sufficient for basic profile information

3. **Add OneDrive Permissions**
   - Click **+ Add a permission**
   - Select **Microsoft Graph**
   - Select **Delegated permissions**
   - Search for and add the following permissions:
     - `Files.ReadWrite` - Read and write access to user files
     - `Files.ReadWrite.All` - Read and write access to all files user can access
     - `offline_access` - Maintain access to data you have given it access to
   - Click **Add permissions**

4. **Permission Descriptions**
   - `Files.ReadWrite`: Allows the app to read and write files that the user selects
   - `Files.ReadWrite.All`: Allows the app to read and write all files the user can access
   - `offline_access`: Allows the app to maintain access when the user is not present (refresh tokens)

5. **Admin Consent** (Optional)
   - For organizational accounts, an admin may need to grant consent
   - Click **Grant admin consent for [Your Organization]** if you have admin rights
   - For personal Microsoft accounts, users will consent during their first login

## Step 5: Configure Optional Client Secret (Not Recommended for Desktop Apps)

> **Note**: Client secrets are **NOT recommended** for native/desktop applications as they cannot be kept secure. This section is included for completeness but should only be used for confidential client scenarios.

If you absolutely need a client secret:

1. **Navigate to Certificates & secrets**
   - In the left menu, select **Certificates & secrets**

2. **Create New Client Secret**
   - Under **Client secrets**, click **+ New client secret**
   - Add a description (e.g., "Development Secret")
   - Select an expiration period (shorter is more secure)
   - Click **Add**

3. **Copy the Secret Value**
   - **Important**: Copy the secret **value** immediately - it will not be shown again
   - Store this securely using User Secrets (never commit to source control)

## Step 6: Update Application Configuration

Update your `appsettings.json` with the values from your app registration:

```json
{
  "Authentication": {
    "Microsoft": {
      "ClientId": "YOUR-APPLICATION-CLIENT-ID-HERE",
      "TenantId": "common",
      "RedirectUri": "http://localhost",
      "Scopes": [
        "Files.ReadWrite",
        "Files.ReadWrite.All",
        "offline_access"
      ],
      "LoginTimeout": 30,
      "TokenRefreshMargin": 5
    }
  }
}
```

### Configuration Details

- **ClientId**: Your Application (client) ID from Step 2
- **TenantId**: Use `"common"` for Microsoft Personal accounts, or your Directory (tenant) ID for organizational accounts
- **RedirectUri**: Must match one of your configured redirect URIs (typically `http://localhost`)
- **Scopes**: The Graph API permissions your app will request
- **LoginTimeout**: Seconds to wait for user login (default: 30)
- **TokenRefreshMargin**: Minutes before token expiry to proactively refresh (default: 5)

### Store Client Secret Securely (if applicable)

If you created a client secret, store it using User Secrets:

```bash
cd src/AStar.Dev.OneDrive.Sync.Client
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "your-secret-value-here"
```

## Step 7: Test Authentication

1. **Build and Run the Application**
   ```bash
   dotnet run --project src/AStar.Dev.OneDrive.Sync.Client
   ```

2. **Expected Flow**
   - The app will open a browser window or provide a device code
   - You'll be prompted to sign in with your Microsoft account
   - You'll be asked to consent to the requested permissions
   - After consent, you'll be redirected back to the app

3. **Verify Token Storage**
   - Tokens are stored securely using platform-specific secure storage
   - Check application logs for successful authentication messages

## Troubleshooting

### Error: "AADSTS7000215: Invalid client secret provided"
- **Solution**: The client secret is expired or incorrect. Generate a new secret and update User Secrets.

### Error: "AADSTS50011: The reply URL specified in the request does not match"
- **Solution**: Ensure the redirect URI in your app registration matches exactly what's in `appsettings.json`.

### Error: "AADSTS65001: The user or administrator has not consented"
- **Solution**: 
  - For personal accounts: User needs to go through the consent flow on first login
  - For organizational accounts: An admin may need to grant tenant-wide consent

### Error: "AADSTS700016: Application not found in the directory"
- **Solution**: Verify you're using the correct Application (client) ID and that you're authenticating against the correct tenant.

### Error: "Login timed out"
- **Solution**: 
  - Increase `LoginTimeout` value in configuration
  - Check your network connection
  - Try again - Azure AD may be experiencing temporary issues

## Security Best Practices

1. **Never commit secrets to source control**
   - Use User Secrets for local development
   - Use Azure Key Vault or environment variables for production

2. **Use minimal required permissions**
   - Only request the Graph API scopes your app actually needs
   - Avoid requesting `Files.ReadWrite.All` if `Files.ReadWrite` is sufficient

3. **Regularly rotate secrets**
   - If using client secrets, set short expiration periods and rotate regularly

4. **Enable logging for security events**
   - Monitor authentication failures and suspicious activity
   - Use Entra ID sign-in logs to audit access

5. **Implement proper token refresh**
   - The app automatically refreshes tokens 5 minutes before expiry
   - Implement exponential backoff for refresh failures

## Additional Resources

- [Microsoft identity platform documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/)
- [Microsoft Graph API documentation](https://docs.microsoft.com/en-us/graph/)
- [OneDrive API reference](https://docs.microsoft.com/en-us/onedrive/developer/)
- [OAuth 2.0 authorization code flow](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow)
- [Device code flow](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-device-code)

## Support

If you encounter issues not covered in this guide:

1. Check the application logs in `logs/` directory
2. Enable debug logging by setting `EnableDebugLogging` to `true` in account settings
3. Review the Microsoft Entra ID sign-in logs in Azure Portal
4. Consult the [project documentation](../README.md) for additional troubleshooting steps

---

**Document Version**: 1.0  
**Last Updated**: February 3, 2026  
**Applies To**: AStar OneDrive Sync Client v1.0+
