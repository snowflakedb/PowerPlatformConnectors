using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SnowflakeTestApp.Tests.Metadata
{
    /// <summary>
    /// Integration tests for the table metadata endpoints.
    /// These tests document the expected behavior and can be used to verify the endpoints manually.
    /// Based on actual API testing, unauthenticated requests return 404 Not Found.
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
        /// Note: This test uses the globally seeded CUSTOMERS table
        /// </summary>
        [TestMethod]
        public async Task GetTableMetadataEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/$metadata.json/datasets/default/tables/CUSTOMERS");
            
            AssertStatusCode(response, HttpStatusCode.OK);
            AssertResponseHasContent(response);
        }

        /// <summary>
        /// Test the /$metadata.json/datasets/{dataset}/tables/{table} endpoint without authentication
        /// Based on actual API testing, returns 404 Not Found (not 500 Internal Server Error)
        /// </summary>
        [TestMethod]
        public async Task GetTableMetadataEndpoint_WithoutAuth_ReturnsNotFound()
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/$metadata.json/datasets/default/tables/CUSTOMERS");
            
            AssertStatusCode(response, HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Test the /$metadata.json/datasets/{dataset}/tables/{table} endpoint with invalid table
        /// </summary>
        [TestMethod]
        public async Task GetTableMetadataEndpoint_WithInvalidTable_ReturnsBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/$metadata.json/datasets/default/tables/INVALID_TABLE");
            
            AssertStatusCode(response, HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Test the /$metadata.json/datasets/{dataset}/tables/{table} endpoint with non-existent dataset
        /// </summary>
        [TestMethod]
        public async Task GetTableMetadataEndpoint_WithNonExistentDataset_ReturnsNotFound()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/$metadata.json/datasets/nonexistent/tables/CUSTOMERS");
            
            AssertStatusCode(response, HttpStatusCode.NotFound);
        }
    }
} 
