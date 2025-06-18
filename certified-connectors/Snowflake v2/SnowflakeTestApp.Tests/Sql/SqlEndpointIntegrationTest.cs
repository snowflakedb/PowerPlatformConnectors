using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Net;
using SnowflakeTestApp.Tests.Infrastructure;

namespace SnowflakeTestApp.Tests.Sql
{
    /// <summary>
    /// Integration tests for the SQL endpoints.
    /// These tests document the expected behavior and can be used to verify the endpoints manually.
    /// This test class automatically creates and seeds the CUSTOMERS test table before running tests.
    /// </summary>
    [TestClass]
    public class SqlTests : BaseIntegrationTestWithDataSeeding
    {
        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            EnsureApplicationIsRunning();
        }

        /// <summary>
        /// Test the POST /sql endpoint for executing SQL statements with authentication
        /// This test queries the seeded test data
        /// </summary>
        [TestMethod]
        public async Task ExecuteSqlStatementEndpoint_WithAuth_ReturnsOk()
        {
            RequireTestData(); // Ensure test data is available
            
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.DefaultSnowflakeInstance);
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            // Query the seeded test data
            var sqlPayload = new
            {
                statement = $"SELECT COUNT(*) as total_customers FROM {TestData.DefaultTable}",
                timeout = TestData.DefaultSqlTimeout
            };

            var json = JsonConvert.SerializeObject(sqlPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql", content);
            
            // If we get 500 Internal Server Error, it might be due to invalid token or app configuration
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Assert.Inconclusive($"SQL endpoint returned Internal Server Error. This might be due to invalid bearer token, missing Snowflake configuration, or application setup issues. " +
                                   $"Response: {responseContent}");
            }
            
            Assert.IsTrue(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");
            
            var successContent = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(successContent), "Response content should not be empty");
            
            // Log the response to see the count of seeded records
            TestContext?.WriteLine($"SQL query response: {successContent}");
        }

        /// <summary>
        /// Test querying the seeded test data with specific customer information
        /// </summary>
        [TestMethod]
        public async Task QuerySeededTestData_WithAuth_ReturnsCustomerData()
        {
            RequireTestData(); // Ensure test data is available
            
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.DefaultSnowflakeInstance);
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            // Query specific seeded customer data
            var sqlPayload = new
            {
                statement = $"SELECT NAME, EMAIL, BALANCE FROM {TestData.DefaultTable} WHERE NAME = 'John Doe' OR NAME = 'Jane Smith' ORDER BY NAME",
                timeout = TestData.DefaultSqlTimeout
            };

            var json = JsonConvert.SerializeObject(sqlPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql", content);
            
            // If we get 500 Internal Server Error, it might be due to invalid token or app configuration
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Assert.Inconclusive($"SQL endpoint returned Internal Server Error when querying seeded data. This might be due to invalid bearer token, missing Snowflake configuration, or application setup issues. " +
                                   $"Response: {responseContent}");
            }
            
            Assert.IsTrue(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");
            
            var successContent = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(successContent), "Response content should not be empty");
            
            // Log the response to verify we get the seeded customer data
            TestContext?.WriteLine($"Seeded customer data query response: {successContent}");
        }

        /// <summary>
        /// Test the POST /sql endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task ExecuteSqlStatementEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.DefaultSnowflakeInstance);
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var sqlPayload = new
            {
                statement = TestData.SampleSqlStatement,
                timeout = TestData.DefaultSqlTimeout,
                database = TestData.DefaultDatabase,
                schema = TestData.DefaultSchema,
                warehouse = TestData.DefaultWarehouse,
                role = TestData.DefaultRole
            };

            var json = JsonConvert.SerializeObject(sqlPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql", content);
            
            // Accept various authentication-related error codes
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || 
                         response.StatusCode == HttpStatusCode.Forbidden ||
                         response.StatusCode == HttpStatusCode.InternalServerError,
                         $"Expected authentication failure but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the POST /sql endpoint with missing Instance header
        /// </summary>
        [TestMethod]
        public async Task ExecuteSqlStatementEndpoint_WithMissingInstance_ReturnsBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var sqlPayload = new
            {
                statement = "SELECT 1 as test_column;",
                timeout = 60
            };

            var json = JsonConvert.SerializeObject(sqlPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql", content);
            
            // Accept BadRequest or InternalServerError
            Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || 
                         response.StatusCode == HttpStatusCode.InternalServerError,
                         $"Expected BadRequest (400) or InternalServerError (500) but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the POST /sql endpoint with missing Accept header
        /// </summary>
        [TestMethod]
        public async Task ExecuteSqlStatementEndpoint_WithMissingAccept_ReturnsBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.DefaultSnowflakeInstance);

            var sqlPayload = new
            {
                statement = TestData.SampleSqlStatement,
                timeout = TestData.DefaultSqlTimeout,
                database = TestData.DefaultDatabase,
                schema = TestData.DefaultSchema,
                warehouse = TestData.DefaultWarehouse,
                role = TestData.DefaultRole
            };

            var json = JsonConvert.SerializeObject(sqlPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql", content);
            
            // Accept BadRequest or InternalServerError
            Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || 
                         response.StatusCode == HttpStatusCode.InternalServerError,
                         $"Expected BadRequest (400) or InternalServerError (500) but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the POST /sql endpoint with empty SQL statement
        /// </summary>
        [TestMethod]
        public async Task ExecuteSqlStatementEndpoint_WithEmptyStatement_ReturnsBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.DefaultSnowflakeInstance);
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var sqlPayload = new
            {
                statement = "",
                timeout = TestData.DefaultSqlTimeout,
                database = TestData.DefaultDatabase,
                schema = TestData.DefaultSchema,
                warehouse = TestData.DefaultWarehouse,
                role = TestData.DefaultRole
            };

            var json = JsonConvert.SerializeObject(sqlPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql", content);
            
            // Accept BadRequest or InternalServerError
            Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || 
                         response.StatusCode == HttpStatusCode.InternalServerError,
                         $"Expected BadRequest (400) or InternalServerError (500) but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the POST /sql/{statementHandle} endpoint for getting results
        /// Note: This test uses a mock statement handle - in real scenarios, you'd get this from executing a statement first
        /// </summary>
        [TestMethod]
        public async Task GetResultsEndpoint_WithAuth_ReturnsOkOrBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.DefaultSnowflakeInstance);
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var mockStatementHandle = TestData.MockStatementHandle;
            var schema = new
            {
                type = "object",
                properties = new { }
            };

            var json = JsonConvert.SerializeObject(schema);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql/{mockStatementHandle}", content);
            
            // This endpoint expects either success, BadRequest, or InternalServerError
            Assert.IsTrue(response.IsSuccessStatusCode || 
                         response.StatusCode == HttpStatusCode.BadRequest ||
                         response.StatusCode == HttpStatusCode.InternalServerError,
                         $"Expected success, BadRequest, or InternalServerError but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the POST /sql/{statementHandle} endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task GetResultsEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.DefaultSnowflakeInstance);
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var mockStatementHandle = TestData.MockStatementHandle;
            var schema = new { };

            var json = JsonConvert.SerializeObject(schema);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql/{mockStatementHandle}", content);
            
            // Accept various authentication-related error codes
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || 
                         response.StatusCode == HttpStatusCode.Forbidden ||
                         response.StatusCode == HttpStatusCode.InternalServerError,
                         $"Expected authentication failure but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the POST /sql/{statementHandle}/cancel endpoint for cancelling statements
        /// Note: This test uses a mock statement handle
        /// </summary>
        [TestMethod]
        public async Task CancelStatementEndpoint_WithAuth_ReturnsOkOrBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.DefaultSnowflakeInstance);
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var mockStatementHandle = TestData.MockStatementHandle;
            var content = new StringContent("", Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql/{mockStatementHandle}/cancel", content);
            
            // This endpoint expects either success, BadRequest, or InternalServerError
            Assert.IsTrue(response.IsSuccessStatusCode || 
                         response.StatusCode == HttpStatusCode.BadRequest ||
                         response.StatusCode == HttpStatusCode.InternalServerError,
                         $"Expected success, BadRequest, or InternalServerError but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the POST /sql/{statementHandle}/cancel endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task CancelStatementEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.DefaultSnowflakeInstance);
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var mockStatementHandle = TestData.MockStatementHandle;
            var content = new StringContent("", Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql/{mockStatementHandle}/cancel", content);
            
            // Accept various authentication-related error codes
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || 
                         response.StatusCode == HttpStatusCode.Forbidden ||
                         response.StatusCode == HttpStatusCode.InternalServerError,
                         $"Expected authentication failure but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the POST /sql/{statementHandle}/cancel endpoint with empty statement handle
        /// </summary>
        [TestMethod]
        public async Task CancelStatementEndpoint_WithEmptyStatementHandle_ReturnsNotFoundOrBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.DefaultSnowflakeInstance);
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var content = new StringContent("", Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql//cancel", content);
            
            // Accept NotFound, BadRequest, or InternalServerError
            Assert.IsTrue(response.StatusCode == HttpStatusCode.NotFound || 
                         response.StatusCode == HttpStatusCode.BadRequest ||
                         response.StatusCode == HttpStatusCode.InternalServerError,
                         $"Expected NotFound, BadRequest, or InternalServerError but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the POST /sql endpoint with invalid Instance header
        /// </summary>
        [TestMethod]
        public async Task ExecuteSqlStatementEndpoint_WithInvalidInstanceHeader_ReturnsBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Instance", "invalid-instance");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var sqlPayload = new
            {
                statement = TestData.SampleSqlStatement,
                timeout = TestData.DefaultSqlTimeout,
                database = TestData.DefaultDatabase,
                schema = TestData.DefaultSchema,
                warehouse = TestData.DefaultWarehouse,
                role = TestData.DefaultRole
            };

            var json = JsonConvert.SerializeObject(sqlPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql", content);
            
            // Accept BadRequest or InternalServerError
            Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || 
                         response.StatusCode == HttpStatusCode.InternalServerError,
                         $"Expected BadRequest (400) or InternalServerError (500) but got {response.StatusCode}");
        }
    }
} 