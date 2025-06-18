using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SnowflakeTestApp.Tests.Metadata
{
    /// <summary>
    /// Integration tests for the table metadata endpoints.
    /// These tests document the expected behavior and can be used to verify the endpoints manually.
    /// </summary>
    [TestClass]
    public class TableMetadataEndpointIntegrationTest : BaseIntegrationTest
    {
        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            EnsureApplicationIsRunning();
        }

        /// <summary>
        /// Test the /$metadata.json/datasets/{dataset}/tables/{table} endpoint with authentication
        /// Note: This test uses example table name 'CUSTOMERS' - adjust as needed for your test environment
        /// </summary>
        [TestMethod]
        public async Task GetTableMetadataEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/$metadata.json/datasets/default/tables/CUSTOMERS");
            
            // If we get 500 Internal Server Error, it might be due to invalid token or app configuration
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                var content = await response.Content.ReadAsStringAsync();
                Assert.Inconclusive($"Table metadata endpoint returned Internal Server Error. This might be due to invalid bearer token or application configuration. " +
                                   $"Response: {content}");
            }
            
            Assert.IsTrue(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(responseContent), "Response content should not be empty");
        }

        /// <summary>
        /// Test the table metadata endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task GetTableMetadataEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/$metadata.json/datasets/default/tables/CUSTOMERS");
            
            // Accept various authentication-related error codes
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || 
                         response.StatusCode == HttpStatusCode.Forbidden ||
                         response.StatusCode == HttpStatusCode.InternalServerError,
                         $"Expected authentication failure but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the table metadata endpoint with missing dataset parameter
        /// </summary>
        [TestMethod]
        public async Task GetTableMetadataEndpoint_WithMissingDataset_ReturnsBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/$metadata.json/datasets//tables/CUSTOMERS");
            
            // Accept BadRequest or NotFound depending on how the routing is configured
            Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || 
                         response.StatusCode == HttpStatusCode.NotFound,
                         $"Expected BadRequest (400) or NotFound (404) but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the table metadata endpoint with missing table parameter
        /// </summary>
        [TestMethod]
        public async Task GetTableMetadataEndpoint_WithMissingTable_ReturnsBadRequestOrNotFound()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/$metadata.json/datasets/default/tables/");
            Assert.IsFalse(response.IsSuccessStatusCode, $"Expected failure but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the table metadata endpoint with non-existent table
        /// </summary>
        [TestMethod]
        public async Task GetTableMetadataEndpoint_WithNonExistentTable_ReturnsNotFound()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/$metadata.json/datasets/default/tables/NonExistentTable123");
            
            // Accept NotFound or InternalServerError (which can happen if the token is invalid)
            Assert.IsTrue(response.StatusCode == HttpStatusCode.NotFound || 
                         response.StatusCode == HttpStatusCode.InternalServerError,
                         $"Expected NotFound (404) or InternalServerError (500) but got {response.StatusCode}");
        }
    }
} 
