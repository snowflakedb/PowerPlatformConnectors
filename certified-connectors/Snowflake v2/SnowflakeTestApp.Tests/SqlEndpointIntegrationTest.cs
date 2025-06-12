using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace SnowflakeTestApp.Tests
{
    /// <summary>
    /// Integration tests for the SQL endpoints.
    /// These tests document the expected behavior and can be used to verify the endpoints manually.
    /// </summary>
    [TestClass]
    public class SqlEndpointIntegrationTest : BaseIntegrationTest
    {
        /// <summary>
        /// Test the POST /sql endpoint for executing SQL statements with authentication
        /// </summary>
        [TestMethod]
        public async Task ExecuteSqlStatementEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Instance", "your-snowflake-instance.snowflakecomputing.com");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var sqlPayload = new
            {
                statement = "SELECT 1 as test_column;",
                timeout = 60
            };

            var json = JsonConvert.SerializeObject(sqlPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql", content);
            Assert.IsTrue(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(responseContent), "Response content should not be empty");
        }

        /// <summary>
        /// Test the POST /sql endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task ExecuteSqlStatementEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            HttpClient.DefaultRequestHeaders.Add("Instance", "your-snowflake-instance.snowflakecomputing.com");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var sqlPayload = new
            {
                statement = "SELECT 1 as test_column;",
                timeout = 60
            };

            var json = JsonConvert.SerializeObject(sqlPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql", content);
            Assert.IsFalse(response.IsSuccessStatusCode, $"Expected failure but got {response.StatusCode}");
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
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode, "Expected HTTP 400 Bad Request");
        }

        /// <summary>
        /// Test the POST /sql endpoint with missing Accept header
        /// </summary>
        [TestMethod]
        public async Task ExecuteSqlStatementEndpoint_WithMissingAccept_ReturnsBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Instance", "your-snowflake-instance.snowflakecomputing.com");

            var sqlPayload = new
            {
                statement = "SELECT 1 as test_column;",
                timeout = 60
            };

            var json = JsonConvert.SerializeObject(sqlPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql", content);
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode, "Expected HTTP 400 Bad Request");
        }

        /// <summary>
        /// Test the POST /sql endpoint with empty SQL statement
        /// </summary>
        [TestMethod]
        public async Task ExecuteSqlStatementEndpoint_WithEmptyStatement_ReturnsBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Instance", "your-snowflake-instance.snowflakecomputing.com");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var sqlPayload = new
            {
                statement = "",
                timeout = 60
            };

            var json = JsonConvert.SerializeObject(sqlPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql", content);
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode, "Expected HTTP 400 Bad Request");
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
            HttpClient.DefaultRequestHeaders.Add("Instance", "your-snowflake-instance.snowflakecomputing.com");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var mockStatementHandle = "01b1ebc6-0000-7065-0000-438300e4e0c6";
            var schema = new
            {
                type = "object",
                properties = new { }
            };

            var json = JsonConvert.SerializeObject(schema);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql/{mockStatementHandle}", content);
            
            // This endpoint expects either success (if statement handle is valid) or bad request (if invalid)
            Assert.IsTrue(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest,
                         $"Expected success or BadRequest but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the POST /sql/{statementHandle} endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task GetResultsEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            HttpClient.DefaultRequestHeaders.Add("Instance", "your-snowflake-instance.snowflakecomputing.com");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var mockStatementHandle = "01b1ebc6-0000-7065-0000-438300e4e0c6";
            var schema = new { };

            var json = JsonConvert.SerializeObject(schema);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql/{mockStatementHandle}", content);
            Assert.IsFalse(response.IsSuccessStatusCode, $"Expected failure but got {response.StatusCode}");
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
            HttpClient.DefaultRequestHeaders.Add("Instance", "your-snowflake-instance.snowflakecomputing.com");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var mockStatementHandle = "01b1ebc6-0000-7065-0000-438300e4e0c6";
            var content = new StringContent("", Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql/{mockStatementHandle}/cancel", content);
            
            // This endpoint expects either success (if statement handle is valid) or bad request (if invalid)
            Assert.IsTrue(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest,
                         $"Expected success or BadRequest but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the POST /sql/{statementHandle}/cancel endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task CancelStatementEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            HttpClient.DefaultRequestHeaders.Add("Instance", "your-snowflake-instance.snowflakecomputing.com");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var mockStatementHandle = "01b1ebc6-0000-7065-0000-438300e4e0c6";
            var content = new StringContent("", Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql/{mockStatementHandle}/cancel", content);
            Assert.IsFalse(response.IsSuccessStatusCode, $"Expected failure but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the POST /sql/{statementHandle}/cancel endpoint with missing statement handle
        /// </summary>
        [TestMethod]
        public async Task CancelStatementEndpoint_WithEmptyStatementHandle_ReturnsNotFoundOrBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Instance", "your-snowflake-instance.snowflakecomputing.com");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var content = new StringContent("", Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync($"{BaseUrl}/sql//cancel", content);
            
            // Expect either NotFound (404) or BadRequest (400) for malformed URL
            Assert.IsTrue(response.StatusCode == System.Net.HttpStatusCode.NotFound || 
                         response.StatusCode == System.Net.HttpStatusCode.BadRequest,
                         $"Expected NotFound or BadRequest but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the POST /sql endpoint with instance header containing protocol prefix (should fail)
        /// </summary>
        [TestMethod]
        public async Task ExecuteSqlStatementEndpoint_WithInvalidInstanceHeader_ReturnsBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Instance", "https://your-snowflake-instance.snowflakecomputing.com");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var sqlPayload = new
            {
                statement = "SELECT 1 as test_column;",
                timeout = 60
            };

            var json = JsonConvert.SerializeObject(sqlPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql", content);
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode, "Expected HTTP 400 Bad Request for invalid instance header");
        }
    }
} 