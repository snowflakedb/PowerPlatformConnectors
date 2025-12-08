# OAuth Same Tenant Authentication Tests

This directory contains integration tests for the new OAuth (Same Tenant) authentication type added to the Snowflake V2 connector.

## Overview

The `OAuthSameTenantAuthenticationTest.cs` file contains comprehensive tests for the new `oauthUserSameTenant` authentication method, which provides a simplified OAuth flow for Canvas Apps and Power Automate in same-tenant scenarios.

## Test Coverage

### 1. Test Connection Tests
- ✅ Valid OAuth token connection
- ✅ Invalid OAuth token handling
- ✅ Missing authentication header handling

### 2. SQL Operations Tests
- ✅ SELECT queries
- ✅ INSERT operations
- ✅ Query execution with proper authentication

### 3. Virtual Table / Tabular Operations Tests
- ✅ Verifies that Virtual Table operations are blocked
- ✅ Ensures metadata operations are blocked
- ✅ Confirms table data operations are blocked
- 🔒 **Critical**: OAuth Same Tenant should NOT support Virtual Tables

### 4. Authentication Type Comparison
- 📝 Documents differences from Service Principal
- 📝 Documents differences from User Delegated auth

### 5. Error Handling Tests
- ✅ Missing required headers
- ✅ Expired token handling
- ✅ Invalid credentials

### 6. Cross-Tenant Tests
- 📝 Documents same-tenant limitation

### 7. Performance Tests
- ✅ Query execution time validation

## Running the Tests

### Prerequisites

1. **Snowflake Account**: You need access to a Snowflake instance
2. **Azure AD Application**: Configure an Azure AD app for OAuth
3. **Test Configuration**: Update `TestData.cs` with your connection details:
   - `SnowflakeInstance`: Your Snowflake account URL (e.g., `myaccount.snowflakecomputing.com`)
   - `Database`: Test database name
   - `Schema`: Test schema name
   - `Warehouse`: Snowflake warehouse name
   - `Role`: Snowflake role to use
   - `TenantId`: Azure AD tenant ID
   - `ClientId`: OAuth client ID
   - `ClientSecret`: OAuth client secret
   - `Scope`: OAuth scope (typically the Resource URI)

4. **Running Test Application**: Ensure `SnowflakeTestApp` is running locally or deployed

### Running Individual Tests

```bash
# Run all OAuth authentication tests
dotnet test --filter "FullyQualifiedName~OAuthSameTenantAuthenticationTest"

# Run specific test
dotnet test --filter "FullyQualifiedName~OAuthSameTenantAuthenticationTest.TestConnection_WithOAuthSameTenant_ReturnsOK"

# Run SQL operation tests only
dotnet test --filter "FullyQualifiedName~OAuthSameTenantAuthenticationTest.SqlOperation"

# Run Virtual Table blocking tests
dotnet test --filter "FullyQualifiedName~OAuthSameTenantAuthenticationTest.TabularOperation"
```

### Running in Visual Studio

1. Open the solution in Visual Studio
2. Open Test Explorer (Test > Test Explorer)
3. Navigate to `SnowflakeTestApp.Tests > Authentication > OAuthSameTenantAuthenticationTest`
4. Right-click and select "Run" or "Debug"

## Setting Up OAuth for Testing

### Step 1: Configure Azure AD Application

1. Create an Azure AD app registration (or use existing)
2. Note the **Application (client) ID**
3. Create a client secret and note the value
4. Configure the **Resource URL** (Application ID URI)
5. Set up the redirect URI: `https://global.consent.azure-apim.net/redirect/snowflakev2`

### Step 2: Configure Snowflake Security Integration

```sql
-- Create security integration for OAuth Same Tenant
CREATE OR REPLACE SECURITY INTEGRATION oauth_azure_integration
  TYPE = EXTERNAL_OAUTH
  ENABLED = TRUE
  EXTERNAL_OAUTH_TYPE = AZURE
  EXTERNAL_OAUTH_ISSUER = 'https://sts.windows.net/<TENANT_ID>/'
  EXTERNAL_OAUTH_JWS_KEYS_URL = 'https://login.microsoftonline.com/<TENANT_ID>/discovery/v2.0/keys'
  EXTERNAL_OAUTH_AUDIENCE_LIST = ('<RESOURCE_URL>')
  EXTERNAL_OAUTH_TOKEN_USER_MAPPING_CLAIM = 'upn'
  EXTERNAL_OAUTH_SNOWFLAKE_USER_MAPPING_ATTRIBUTE = 'login_name';

-- Verify the integration
DESC SECURITY INTEGRATION oauth_azure_integration;
```

### Step 3: Update Test Configuration

Update the `ConnectionParametersProviderMock.cs` file in `SnowflakeTestApp`:

```csharp
public string GetProperty<T>(string key)
{
    switch (key)
    {
        case "$parameterSet":
            return "oauthUserSameTenant"; // Use OAuth Same Tenant
        case "server":
            return "myaccount.snowflakecomputing.com";
        case "database":
            return "MY_DATABASE";
        case "warehouse":
            return "MY_WAREHOUSE";
        case "role":
            return "MY_ROLE";
        case "schema":
            return "MY_SCHEMA";
        default:
            return null;
    }
}
```

### Step 4: Obtain Test Token

The test automatically obtains a token using the `AccessTokenService`. Ensure your test configuration has valid credentials:

```csharp
// In TestData.cs
public static string TenantId = "your-tenant-id";
public static string ClientId = "your-client-id";
public static string ClientSecret = "your-client-secret";
public static string Scope = "your-resource-url/.default";
```

## Expected Test Results

### Passing Tests (✅)
- All SQL operation tests should pass
- Test connection should return 200 OK
- Authentication with valid token succeeds

### Expected Failures (by Design)
- Virtual Table operations should be **blocked**
- Tabular/CDP operations should return errors
- This is **expected behavior** - OAuth Same Tenant does not support Virtual Tables

## Troubleshooting

### Common Issues

#### 1. "Invalid OAuth access token"
**Solution**:
- Verify your Azure AD app configuration
- Check that the client secret hasn't expired
- Ensure the scope/resource URL is correct
- Verify the token is being passed correctly

#### 2. "Cannot use Tabular calls with OAuth Same Tenant"
**Solution**: This is **expected behavior**. OAuth Same Tenant does not support Virtual Tables. Use Service Principal if you need Virtual Table support.

#### 3. Test connection fails
**Solution**:
- Verify Snowflake credentials are correct
- Check that the security integration is properly configured
- Ensure the user has appropriate permissions
- Verify warehouse is running

#### 4. "Bearer token is missing"
**Solution**:
- Check that the Authorization header is being set correctly
- Verify the test token is being generated properly
- Check `AccessTokenService` configuration

#### 5. Timeout errors
**Solution**:
- Increase timeout in test configuration
- Check Snowflake warehouse status
- Verify network connectivity

### Debug Mode

To run tests with detailed logging:

1. Set breakpoints in test methods
2. Run tests in Debug mode (F5 in Visual Studio)
3. Check test output window for detailed error messages
4. Review HTTP request/response in debugger

### Viewing Test Output

```bash
# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run with trx output for detailed results
dotnet test --logger "trx;LogFileName=TestResults.trx"
```

## Test Maintenance

### Adding New Tests

When adding new tests:
1. Follow the existing test structure
2. Use descriptive test method names
3. Add appropriate test categories/regions
4. Document expected behavior
5. Include error scenarios

### Updating Tests

When the authentication logic changes:
1. Update affected test methods
2. Add tests for new scenarios
3. Update this README with new information
4. Verify all existing tests still pass

## Related Documentation

- [Snowflake V2 Connector Documentation](../ConnectorArtifacts/intro.md)
- [OAuth Design Document](../DESIGN_OAUTH_SAME_TENANT_SUPPORT.md)
- [Solution Documentation](../SolutionDocumentation.md)
- [Snowflake OAuth Documentation](https://docs.snowflake.com/en/user-guide/oauth-azure)

## Support

For issues or questions:
1. Check existing test failures for similar issues
2. Review Snowflake logs for connection errors
3. Verify Azure AD configuration
4. Check connector documentation
5. Review design document for authentication flow details
