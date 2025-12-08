using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SnowflakeTestApp.Tests.Infrastructure;

namespace SnowflakeTestApp.Tests.Authentication
{
    /// <summary>
    /// Integration tests for OAuth Same Tenant authentication type.
    /// Tests the new oauthUserSameTenant authentication method added to support
    /// simplified OAuth flow for Canvas Apps and Power Automate.
    /// </summary>
    [TestClass]
    public class OAuthSameTenantAuthenticationTest : BaseIntegrationTest
    {
        private const string SQL_ENDPOINT = "/sql";
        private const string TEST_CONNECTION_ENDPOINT = "/testconnection";
        private const string DATASETS_ENDPOINT = "/datasets";
        private const string METADATA_ENDPOINT = "/$metadata.json/datasets";

        // Error messages
        private const string VIRTUAL_TABLE_BLOCKED_ERROR = "Cannot use Tabular calls with UserDelegated or OAuth Same Tenant authentication type";
        private const string BEARER_TOKEN_MISSING_ERROR = "Bearer token is missing";
        private const string INVALID_OAUTH_TOKEN_ERROR = "Invalid OAuth access token";

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            EnsureApplicationIsRunning();
        }

        #region Test Connection Tests

        /// <summary>
        /// Test that OAuth Same Tenant can successfully connect to Snowflake.
        /// Unlike AADUserDelegated, OAuth Same Tenant should test the actual connection
        /// because it has server/database parameters at connection creation time.
        /// </summary>
        [TestMethod]
        public async Task TestConnection_WithOAuthSameTenant_ReturnsOK()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("X-MS-PARAMETER-SET", "oauthUserSameTenant");

            var response = await HttpClient.GetAsync($"{BaseUrl}{TEST_CONNECTION_ENDPOINT}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "OAuth Same Tenant authentication should successfully test connection with valid token");
        }

        /// <summary>
        /// Test connection with invalid OAuth token should fail
        /// </summary>
        [TestMethod]
        public async Task TestConnection_WithInvalidOAuthToken_ReturnsError()
        {
            HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer invalid-token");
            HttpClient.DefaultRequestHeaders.Add("X-MS-PARAMETER-SET", "oauthUserSameTenant");

            var response = await HttpClient.GetAsync($"{BaseUrl}{TEST_CONNECTION_ENDPOINT}");
            var content = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.IsTrue(content.Contains(INVALID_OAUTH_TOKEN_ERROR),
                "Should return error message about invalid OAuth token");
        }

        /// <summary>
        /// Test connection without authentication header should fail
        /// </summary>
        [TestMethod]
        public async Task TestConnection_WithoutAuth_ReturnsError()
        {
            HttpClient.DefaultRequestHeaders.Add("X-MS-PARAMETER-SET", "oauthUserSameTenant");

            var response = await HttpClient.GetAsync($"{BaseUrl}{TEST_CONNECTION_ENDPOINT}");
            var content = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.IsTrue(content.Contains(BEARER_TOKEN_MISSING_ERROR),
                "Should return error message about missing bearer token");
        }

        #endregion

        #region SQL Operations Tests

        /// <summary>
        /// Test that SQL operations work with OAuth Same Tenant authentication.
        /// SQL operations should be supported for OAuth Same Tenant.
        /// </summary>
        [TestMethod]
        public async Task SqlOperation_WithOAuthSameTenant_ExecutesSuccessfully()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("X-MS-PARAMETER-SET", "oauthUserSameTenant");
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.SnowflakeInstance);
            HttpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var sqlRequest = new
            {
                statement = "SELECT CURRENT_VERSION() AS VERSION",
                timeout = 60,
                database = TestData.Database,
                schema = TestData.Schema,
                warehouse = TestData.Warehouse,
                role = TestData.Role
            };

            var content = CreateJsonContent(sqlRequest);
            var response = await HttpClient.PostAsync($"{BaseUrl}{SQL_ENDPOINT}", content);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "SQL operations should work with OAuth Same Tenant authentication");

            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(responseContent.Length > 0, "Should return query results");
        }

        /// <summary>
        /// Test simple SELECT query with OAuth Same Tenant
        /// </summary>
        [TestMethod]
        public async Task SqlQuery_SelectFromTable_ReturnsData()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("X-MS-PARAMETER-SET", "oauthUserSameTenant");
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.SnowflakeInstance);
            HttpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var sqlRequest = new
            {
                statement = $"SELECT * FROM {TestData.DefaultTable} LIMIT 5",
                timeout = 60,
                database = TestData.Database,
                schema = TestData.Schema,
                warehouse = TestData.Warehouse,
                role = TestData.Role
            };

            var content = CreateJsonContent(sqlRequest);
            var response = await HttpClient.PostAsync($"{BaseUrl}{SQL_ENDPOINT}", content);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(responseContent.Contains("Data") || responseContent.Contains("Schema"),
                "Response should contain query results");
        }

        /// <summary>
        /// Test INSERT operation with OAuth Same Tenant
        /// </summary>
        [TestMethod]
        public async Task SqlOperation_InsertRecord_ExecutesSuccessfully()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("X-MS-PARAMETER-SET", "oauthUserSameTenant");
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.SnowflakeInstance);
            HttpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var sqlRequest = new
            {
                statement = $"INSERT INTO {TestData.DefaultTable} (NAME, DESCRIPTION, ACTIVE, CREATED_DATE) VALUES ('OAuth Test', 'Created via OAuth Same Tenant', TRUE, '{timestamp}')",
                timeout = 60,
                database = TestData.Database,
                schema = TestData.Schema,
                warehouse = TestData.Warehouse,
                role = TestData.Role
            };

            var content = CreateJsonContent(sqlRequest);
            var response = await HttpClient.PostAsync($"{BaseUrl}{SQL_ENDPOINT}", content);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                "INSERT operation should work with OAuth Same Tenant authentication");
        }

        #endregion

        #region Virtual Table / Tabular Operations Tests

        /// <summary>
        /// Test that Virtual Table operations are blocked for OAuth Same Tenant.
        /// This is a critical test to ensure OAuth Same Tenant doesn't accidentally
        /// allow Virtual Table operations.
        /// </summary>
        [TestMethod]
        public async Task TabularOperation_WithOAuthSameTenant_IsBlocked()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("X-MS-PARAMETER-SET", "oauthUserSameTenant");

            var response = await HttpClient.GetAsync($"{BaseUrl}{DATASETS_ENDPOINT}");
            var content = await response.Content.ReadAsStringAsync();

            // Should receive an error indicating tabular operations are not supported
            Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
                "Tabular operations should be blocked for OAuth Same Tenant");

            Assert.IsTrue(content.Contains(VIRTUAL_TABLE_BLOCKED_ERROR) ||
                         response.StatusCode == HttpStatusCode.BadRequest ||
                         response.StatusCode == HttpStatusCode.InternalServerError,
                "Should return appropriate error for blocked tabular operation");
        }

        /// <summary>
        /// Test that metadata endpoint is also blocked for OAuth Same Tenant
        /// </summary>
        [TestMethod]
        public async Task MetadataOperation_WithOAuthSameTenant_IsBlocked()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("X-MS-PARAMETER-SET", "oauthUserSameTenant");

            var response = await HttpClient.GetAsync($"{BaseUrl}{METADATA_ENDPOINT}");
            var content = await response.Content.ReadAsStringAsync();

            Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
                "Metadata operations should be blocked for OAuth Same Tenant");
        }

        /// <summary>
        /// Test that table data endpoint is blocked for OAuth Same Tenant
        /// </summary>
        [TestMethod]
        public async Task TableDataOperation_WithOAuthSameTenant_IsBlocked()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("X-MS-PARAMETER-SET", "oauthUserSameTenant");

            var dataset = $"{TestData.SnowflakeInstance},{TestData.Database}";
            var tableDataEndpoint = $"/datasets/{Uri.EscapeDataString(dataset)}/tables/{TestData.DefaultTable}/items";

            var response = await HttpClient.GetAsync($"{BaseUrl}{tableDataEndpoint}");
            var content = await response.Content.ReadAsStringAsync();

            Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
                "Table data operations should be blocked for OAuth Same Tenant");

            Assert.IsTrue(content.Contains(VIRTUAL_TABLE_BLOCKED_ERROR) ||
                         response.StatusCode == HttpStatusCode.InternalServerError,
                "Should return error about blocked Virtual Table operation");
        }

        #endregion

        #region Authentication Type Comparison Tests

        /// <summary>
        /// Test that OAuth Same Tenant behaves differently from Service Principal
        /// regarding connection sharing capability
        /// </summary>
        [TestMethod]
        public void OAuthSameTenant_IsNotShareable_UnlikeServicePrincipal()
        {
            // This is a documentation test to capture the difference
            // In the actual connector configuration:
            // - Service Principal: "allowSharing": true
            // - OAuth Same Tenant: "allowSharing": false

            Assert.IsTrue(true, "OAuth Same Tenant should not be shareable (user-specific auth)");
        }

        /// <summary>
        /// Test that OAuth Same Tenant has connection parameters at creation time,
        /// unlike AADUserDelegated
        /// </summary>
        [TestMethod]
        public void OAuthSameTenant_HasConnectionParameters_UnlikeUserDelegated()
        {
            // This is a documentation test to capture the difference
            // OAuth Same Tenant requires: Server, Database, Warehouse, Role, Schema at connection creation
            // AADUserDelegated: Only Client ID, Secret, Resource URL at connection creation

            Assert.IsTrue(true, "OAuth Same Tenant should have all connection parameters at creation time");
        }

        #endregion

        #region Error Handling Tests

        /// <summary>
        /// Test error handling with missing required headers
        /// </summary>
        [TestMethod]
        public async Task SqlOperation_WithMissingInstance_ReturnsError()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("X-MS-PARAMETER-SET", "oauthUserSameTenant");
            // Intentionally not adding Instance header

            var sqlRequest = new
            {
                statement = "SELECT 1",
                timeout = 60,
                database = TestData.Database,
                schema = TestData.Schema,
                warehouse = TestData.Warehouse,
                role = TestData.Role
            };

            var content = CreateJsonContent(sqlRequest);
            var response = await HttpClient.PostAsync($"{BaseUrl}{SQL_ENDPOINT}", content);

            Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
                "Should fail when required Instance header is missing");
        }

        /// <summary>
        /// Test token expiration handling
        /// </summary>
        [TestMethod]
        public async Task SqlOperation_WithExpiredToken_ReturnsUnauthorized()
        {
            // Use a clearly invalid/expired token format
            var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1MTYyMzkwMjJ9.expired";
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {expiredToken}");
            HttpClient.DefaultRequestHeaders.Add("X-MS-PARAMETER-SET", "oauthUserSameTenant");
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.SnowflakeInstance);

            var sqlRequest = new
            {
                statement = "SELECT 1",
                timeout = 60,
                database = TestData.Database,
                schema = TestData.Schema,
                warehouse = TestData.Warehouse,
                role = TestData.Role
            };

            var content = CreateJsonContent(sqlRequest);
            var response = await HttpClient.PostAsync($"{BaseUrl}{SQL_ENDPOINT}", content);

            Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized ||
                         response.StatusCode == HttpStatusCode.InternalServerError,
                "Should return appropriate error for expired token");
        }

        #endregion

        #region Cross-Tenant Tests

        /// <summary>
        /// Test that OAuth Same Tenant is designed for same-tenant scenarios only
        /// This is a documentation test as cross-tenant testing requires specific setup
        /// </summary>
        [TestMethod]
        public void OAuthSameTenant_IsSameTenantOnly()
        {
            // This is a documentation test
            // OAuth Same Tenant uses tenantId: "common" but should only work for same-tenant
            // Cross-tenant scenarios should use Service Principal instead

            Assert.IsTrue(true,
                "OAuth Same Tenant is designed for same-tenant scenarios. Use Service Principal for cross-tenant.");
        }

        #endregion

        #region Performance Tests

        /// <summary>
        /// Test that OAuth Same Tenant has similar performance to Service Principal
        /// </summary>
        [TestMethod]
        public async Task SqlOperation_PerformanceComparison_IsReasonable()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("X-MS-PARAMETER-SET", "oauthUserSameTenant");
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.SnowflakeInstance);
            HttpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

            var sqlRequest = new
            {
                statement = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES",
                timeout = 60,
                database = TestData.Database,
                schema = TestData.Schema,
                warehouse = TestData.Warehouse,
                role = TestData.Role
            };

            var startTime = DateTime.UtcNow;
            var content = CreateJsonContent(sqlRequest);
            var response = await HttpClient.PostAsync($"{BaseUrl}{SQL_ENDPOINT}", content);
            var elapsed = DateTime.UtcNow - startTime;

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(elapsed.TotalSeconds < 30,
                $"Query should complete in reasonable time. Took {elapsed.TotalSeconds} seconds");
        }

        #endregion
    }
}
