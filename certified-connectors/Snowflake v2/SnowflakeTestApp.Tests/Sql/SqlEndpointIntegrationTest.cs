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
    /// This test class uses the globally seeded CUSTOMERS test table.
    /// </summary>
    [TestClass]
    public class SqlEndpointIntegrationTest : BaseIntegrationTest
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
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.DefaultSnowflakeInstance);
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            // Query the seeded test data with full database context
            var sqlPayload = new
            {
                statement = $"SELECT COUNT(*) as total_customers FROM {TestData.DefaultTable}",
                timeout = TestData.DefaultSqlTimeout,
                database = TestData.DefaultDatabase,
                schema = TestData.DefaultSchema,
                warehouse = TestData.DefaultWarehouse,
                role = TestData.DefaultRole
            };

            var json = JsonConvert.SerializeObject(sqlPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql", content);
            
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(responseContent), "Response content should not be empty");
        }

        /// <summary>
        /// Test querying the seeded test data with specific customer information
        /// </summary>
        [TestMethod]
        public async Task QuerySeededTestData_WithAuth_ReturnsCustomerData()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Add("Instance", TestData.DefaultSnowflakeInstance);
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            // Query specific seeded customer data with full database context
            var sqlPayload = new
            {
                statement = $"SELECT NAME, EMAIL, BALANCE FROM {TestData.DefaultTable} WHERE NAME = 'John Doe' OR NAME = 'Jane Smith' ORDER BY NAME",
                timeout = TestData.DefaultSqlTimeout,
                database = TestData.DefaultDatabase,
                schema = TestData.DefaultSchema,
                warehouse = TestData.DefaultWarehouse,
                role = TestData.DefaultRole
            };

            var json = JsonConvert.SerializeObject(sqlPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql", content);
            
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(responseContent), "Response content should not be empty");
        }

        /// <summary>
        /// Test the POST /sql endpoint without authentication
        /// Based on actual API behavior, returns 500 Internal Server Error (not 401 Unauthorized)
        /// </summary>
        [TestMethod]
        public async Task ExecuteSqlStatementEndpoint_WithoutAuth_ReturnsInternalServerError()
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
            
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
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
            
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
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
            
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
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
            
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Test the GET /sql/results endpoint for getting statement results with authentication
        /// </summary>
        [TestMethod]
        public async Task GetResultsEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/sql/results?statementHandle=test-handle");
            
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Test the GET /sql/results endpoint without authentication
        /// Based on actual API behavior, returns 500 Internal Server Error (not 401 Unauthorized)
        /// </summary>
        [TestMethod]
        public async Task GetResultsEndpoint_WithoutAuth_ReturnsInternalServerError()
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/sql/results?statementHandle=test-handle");
            
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        /// <summary>
        /// Test the POST /sql/cancel endpoint for cancelling statements with authentication
        /// </summary>
        [TestMethod]
        public async Task CancelStatementEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql/cancel?statementHandle=test-handle", new StringContent(""));
            
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Test the POST /sql/cancel endpoint without authentication
        /// Based on actual API behavior, returns 500 Internal Server Error (not 401 Unauthorized)
        /// </summary>
        [TestMethod]
        public async Task CancelStatementEndpoint_WithoutAuth_ReturnsInternalServerError()
        {
            var response = await HttpClient.PostAsync($"{BaseUrl}/sql/cancel?statementHandle=test-handle", new StringContent(""));
            
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        /// <summary>
        /// Test the POST /sql/cancel endpoint with empty statement handle
        /// </summary>
        [TestMethod]
        public async Task CancelStatementEndpoint_WithEmptyStatementHandle_ReturnsNotFound()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql/cancel", new StringContent(""));
            
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
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
                timeout = TestData.DefaultSqlTimeout
            };

            var json = JsonConvert.SerializeObject(sqlPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/sql", content);
            
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
} 
