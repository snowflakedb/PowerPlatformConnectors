using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SnowflakeTestApp.Tests
{
    /// <summary>
    /// Integration tests for the table metadata endpoints.
    /// These tests document the expected behavior and can be used to verify the endpoints manually.
    /// </summary>
    [TestClass]
    public class TableMetadataEndpointIntegrationTest : BaseIntegrationTest
    {
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
            Assert.IsTrue(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(content), "Response content should not be empty");
        }

        /// <summary>
        /// Test the table metadata endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task GetTableMetadataEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/$metadata.json/datasets/default/tables/CUSTOMERS");
            Assert.IsFalse(response.IsSuccessStatusCode, $"Expected failure but got {response.StatusCode}");
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
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode, "Expected HTTP 400 Bad Request");
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
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode, "Expected HTTP 404 Not Found");
        }
    }
} 