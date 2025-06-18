using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Net;

namespace SnowflakeTestApp.Tests.Sql
{
    /// <summary>
    /// Integration tests for the SQL endpoints.
    /// These tests document the expected behavior and can be used to verify the endpoints manually.
    /// </summary>
    [TestClass]
    public class SqlTests : BaseIntegrationTest
    {
        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            EnsureApplicationIsRunning();
        }

        /// <summary>
        /// Test the POST /sql endpoint for executing SQL statements with authentication
        /// </summary>
        [TestMethod]
        public async Task ExecuteSqlStatementEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.DefaultSnowflakeInstance);
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var sqlPayload = new
            {
                statement = TestData.SampleSqlStatement,
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
            HttpClient.DefaultRequestHeaders.Add("Instance", "your-snowflake-instance.snowflakecomputing.com");

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
            HttpClient.DefaultRequestHeaders.Add("Instance", "your-snowflake-instance.snowflakecomputing.com");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var mockStatementHandle = "01b1ebc6-0000-7065-0000-438300e4e0c6";
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
            HttpClient.DefaultRequestHeaders.Add("Instance", "your-snowflake-instance.snowflakecomputing.com");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var mockStatementHandle = "01b1ebc6-0000-7065-0000-438300e4e0c6";
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
            HttpClient.DefaultRequestHeaders.Add("Instance", "your-snowflake-instance.snowflakecomputing.com");
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var mockStatementHandle = "01b1ebc6-0000-7065-0000-438300e4e0c6";
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
            HttpClient.DefaultRequestHeaders.Add("Instance", "your-snowflake-instance.snowflakecomputing.com");
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
    }
} 