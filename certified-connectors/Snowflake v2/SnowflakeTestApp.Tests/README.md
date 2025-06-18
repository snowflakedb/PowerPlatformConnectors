# SnowflakeTestApp.Tests

Integration tests for the Snowflake Test App that verify the endpoint's functionalities.

## Test Coverage

This test suite provides comprehensive coverage for all Snowflake V2 connector endpoints:

### Core Endpoints
- **TestConnectionEndpointIntegrationTest.cs** - Tests `/testconnection` endpoint for connection validation
- **DatasetEndpointIntegrationTest.cs** - Tests `/datasets` endpoint for listing datasets
- **DataSetsMetadataEndpointIntegrationTest.cs** - Tests `/$metadata.json/datasets` endpoint for datasets metadata

### Table Operations
- **TableEndpointIntegrationTest.cs** - Tests `/datasets/{dataset}/tables` endpoint for listing tables
- **TableMetadataEndpointIntegrationTest.cs** - Tests `/$metadata.json/datasets/{dataset}/tables/{table}` endpoint for table metadata

### Data Operations (CRUD)
- **TableDataEndpointIntegrationTest.cs** - Tests table data endpoints for:
  - GET `/datasets/{dataset}/tables/{table}/items` - List items
  - GET `/datasets/{dataset}/tables/{table}/items/{id}` - Get specific item
  - POST `/datasets/{dataset}/tables/{table}/items` - Create new item
  - PATCH/PUT `/datasets/{dataset}/tables/{table}/items/{id}` - Update item
  - DELETE `/datasets/{dataset}/tables/{table}/items/{id}` - Delete item

### SQL Operations
- **SqlEndpointIntegrationTest.cs** - Tests SQL API endpoints for:
  - POST `/sql` - Execute SQL statements
  - POST `/sql/{statementHandle}` - Get query results
  - POST `/sql/{statementHandle}/cancel` - Cancel running queries

### Trigger Operations
- **TriggerEndpointIntegrationTest.cs** - Tests trigger endpoints for:
  - `/datasets/{dataset}/tables/{table}/onnewitems` - New items trigger
  - `/datasets/{dataset}/tables/{table}/onupdateditems` - Updated items trigger
  - `/datasets/{dataset}/tables/{table}/onchangeditems` - Changed items trigger
  - `/datasets/{dataset}/tables/{table}/ondeleteditems` - Deleted items trigger

Each test file includes both positive (with authentication) and negative (without authentication, with invalid parameters) test cases to ensure comprehensive endpoint validation.

## Quick Start

### Method 1: Visual Studio (Recommended)

1. **Open Solution**
   - Launch Visual Studio
   - Open `dirs.sln` in `Snowflake V2` directory

2. **Start Application**
   - Run the application
   - Verify it's accessible at `https://localhost:44362`

3. **Run Tests**
   - Open **Test Explorer**
   - Click **Run All Tests** 
   - View results in Test Explorer

### Method 2: Command Line

1. **Start Application**
   ```bash
   # Build and run the app
   dotnet build
   dotnet run --project SnowflakeTestApp
   ```

2. **Run Tests** (in a new terminal)
   ```bash
   # Run all tests
   dotnet test
   
   # Or run specific test project
   dotnet test SnowflakeTestApp.Tests/SnowflakeTestApp.Tests.csproj
   ```

## Setting Up Bearer Token for Tests

Some integration tests require authentication with a valid bearer token. To set this up:

### 1. Update TestConfiguration

Open the `TestConfiguration.cs` file in the `SnowflakeTestApp.Tests` project and update the `BearerToken` property with your actual OAuth bearer token:

```csharp
/// <summary>
/// Bearer token for test authentication
/// </summary>
public static string BearerToken => "your-actual-bearer-token-here";
```

Replace `"your-actual-bearer-token-here"` with your actual OAuth bearer token. You can obtain this token by following the "Generating OAuth Tokens" section below.

### 2. Example File Structure

Your test directory should look like this:
```
SnowflakeTestApp.Tests/
├── TestConfiguration.cs       ← Contains BearerToken configuration
├── BaseIntegrationTest.cs     ← Base class for all integration tests
├── DatasetEndpointIntegrationTest.cs
├── DataSetsMetadataEndpointIntegrationTest.cs
├── SqlEndpointIntegrationTest.cs
├── TableDataEndpointIntegrationTest.cs
├── TableEndpointIntegrationTest.cs
├── TableMetadataEndpointIntegrationTest.cs
├── TestConnectionEndpointIntegrationTest.cs
├── TriggerEndpointIntegrationTest.cs
└── ...
```

### 3. Security Notes

- Never commit actual tokens to version control
- Consider using environment variables or user secrets for production scenarios
- The bearer token is stored directly in the TestConfiguration class for simplicity during development

## Troubleshooting

- **Test fails**: Ensure SnowflakeTestApp is running at `https://localhost:44362`
- **Connection issues**: Check that the application started successfully without errors

## Generating OAuth Tokens

This guide walks you through setting up OAuth authentication for Snowflake using the Authorization Code Grant flow.

### Prerequisites

- **ACCOUNTADMIN** role access for creating security integrations
- A non-admin user account for testing (default role must not be ACCOUNTADMIN, SECURITYADMIN, or ORGADMIN)
- URL encoding tool (e.g., [urlencoder.io](https://urlencoder.io) or Postman)

### Step 1: Create OAuth Security Integration

Create a Security Integration using the **ACCOUNTADMIN** role:

```sql
CREATE SECURITY INTEGRATION POWER_APPS_TOKEN
TYPE = OAUTH
ENABLED = TRUE
OAUTH_CLIENT = CUSTOM
OAUTH_CLIENT_TYPE = 'CONFIDENTIAL'
OAUTH_REDIRECT_URI = 'https://localhost.com'
OAUTH_ISSUE_REFRESH_TOKENS = TRUE
OAUTH_REFRESH_TOKEN_VALIDITY = 86400;
```

> **Note:** The `OAUTH_REDIRECT_URI` is where Snowflake will redirect the authorization code. We're using `https://localhost.com` for demonstration purposes.

### Step 2: Gather OAuth Configuration Details

1. **Get integration details:**
   ```sql
   DESC SECURITY INTEGRATION POWER_APPS_TOKEN;
   ```
   
   Record the following values:
   - `OAUTH_CLIENT_ID`
   - `OAUTH_REDIRECT_URI`
   - `OAUTH_AUTHORIZATION_ENDPOINT`
   - `OAUTH_TOKEN_ENDPOINT`

2. **Get client secret:**
   ```sql
   SELECT SYSTEM$SHOW_OAUTH_CLIENT_SECRETS('POWER_APPS_TOKEN');
   ```
   
   This returns 2 secrets - record either one as `OAUTH_CLIENT_SECRET`.

### Step 3: Request Authorization Code

1. **Prepare the authorization URL:**
   
   First, URL-encode these parameters:
   - `OAUTH_CLIENT_ID`
   - `OAUTH_REDIRECT_URI`
   
   Then construct the authorization URL:
   ```
   <OAUTH_AUTHORIZATION_ENDPOINT>?response_type=code&client_id=<encoded_OAUTH_CLIENT_ID>&redirect_uri=<encoded_OAUTH_REDIRECT_URI>
   ```

2. **Complete the authorization flow:**
   - Navigate to the authorization URL in your browser
   - Log in with a non-admin Snowflake user
   - Review and accept the consent prompt
   - After consent, you'll be redirected to the `OAUTH_REDIRECT_URI`

3. **Extract the authorization code:**
   
   The redirect URL will contain the authorization code:
   ```
   https://localhost.com/?code=029118413715B55DxxxxxxxxD3AD952F484380839
   ```
   
   Save the `code` parameter value for the next step.

### Step 4: Exchange Authorization Code for Access Token

Use the authorization code to get an access token:

```bash
curl -X POST \
  -H "Content-Type: application/x-www-form-urlencoded;charset=UTF-8" \
  --user "<OAUTH_CLIENT_ID>:<OAUTH_CLIENT_SECRET>" \
  --data-urlencode "grant_type=authorization_code" \
  --data-urlencode "code=<AUTHORIZATION_CODE>" \
  --data-urlencode "redirect_uri=<OAUTH_REDIRECT_URI>" \
  <OAUTH_TOKEN_ENDPOINT>
```

> **Important:** Use the original (non-encoded) values for `OAUTH_CLIENT_ID`, `OAUTH_CLIENT_SECRET`, and `OAUTH_REDIRECT_URI` in this request.

**Response:** You'll receive an access token and refresh token in the response.

### Step 5: Refresh Access Token (Optional)

Access tokens expire after 600 seconds. Use the refresh token to get a new access token without user interaction:

```bash
curl -X POST \
  -H "Content-Type: application/x-www-form-urlencoded;charset=UTF-8" \
  --user "<OAUTH_CLIENT_ID>:<OAUTH_CLIENT_SECRET>" \
  --data-urlencode "grant_type=refresh_token" \
  --data-urlencode "refresh_token=<REFRESH_TOKEN>" \
  --data-urlencode "redirect_uri=<OAUTH_REDIRECT_URI>" \
  <OAUTH_TOKEN_ENDPOINT>
```

### Security Notes

- Keep client secrets secure and never expose them in client-side code
- Access tokens are short-lived (10 minutes) by design
- Use refresh tokens to maintain long-term access without user re-authentication
- Always use HTTPS for production redirect URIs

