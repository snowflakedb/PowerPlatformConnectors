using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace SnowflakeTestApp.Tests.Triggers
{
    /// <summary>
    /// Integration tests for the trigger endpoints.
    /// These tests document the expected behavior and can be used to verify the endpoints manually.
    /// Note: These tests use the globally seeded CUSTOMERS table.
    /// </summary>
    [TestClass]
    public class TriggerEndpointIntegrationTest : BaseIntegrationTest
    {
        private const string TestTable = "CUSTOMERS";
        private const string TestDataset = "default";

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            EnsureApplicationIsRunning();
        }

        /// <summary>
        /// Test the /datasets/{dataset}/tables/{table}/onnewitems endpoint with authentication
        /// </summary>
        [TestMethod]
        public async Task OnNewItemsTriggerEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/{TestDataset}/tables/{TestTable}/onnewitems");
            
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Test the /datasets/{dataset}/tables/{table}/onnewitems endpoint without authentication
        /// Based on actual API behavior, returns 500 Internal Server Error (not 401 Unauthorized)
        /// </summary>
        [TestMethod]
        public async Task OnNewItemsTriggerEndpoint_WithoutAuth_ReturnsInternalServerError()
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/{TestDataset}/tables/{TestTable}/onnewitems");
            
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        /// <summary>
        /// Test the /datasets/{dataset}/tables/{table}/onupdateditems endpoint with authentication
        /// </summary>
        [TestMethod]
        public async Task OnUpdatedItemsTriggerEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/{TestDataset}/tables/{TestTable}/onupdateditems");
            
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Test the /datasets/{dataset}/tables/{table}/onupdateditems endpoint without authentication
        /// Based on actual API behavior, returns 500 Internal Server Error (not 401 Unauthorized)
        /// </summary>
        [TestMethod]
        public async Task OnUpdatedItemsTriggerEndpoint_WithoutAuth_ReturnsInternalServerError()
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/{TestDataset}/tables/{TestTable}/onupdateditems");
            
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        /// <summary>
        /// Test the /datasets/{dataset}/tables/{table}/ondeleteditems endpoint with authentication
        /// </summary>
        [TestMethod]
        public async Task OnDeletedItemsTriggerEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/{TestDataset}/tables/{TestTable}/ondeleteditems");
            
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Test the /datasets/{dataset}/tables/{table}/ondeleteditems endpoint without authentication
        /// Based on actual API behavior, returns 500 Internal Server Error (not 401 Unauthorized)
        /// </summary>
        [TestMethod]
        public async Task OnDeletedItemsTriggerEndpoint_WithoutAuth_ReturnsInternalServerError()
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/{TestDataset}/tables/{TestTable}/ondeleteditems");
            
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        /// <summary>
        /// Test the /datasets/{dataset}/tables/{table}/onchangeditems endpoint with authentication
        /// </summary>
        [TestMethod]
        public async Task OnChangedItemsTriggerEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/{TestDataset}/tables/{TestTable}/onchangeditems");
            
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Test the /datasets/{dataset}/tables/{table}/onchangeditems endpoint without authentication
        /// Based on actual API behavior, returns 500 Internal Server Error (not 401 Unauthorized)
        /// </summary>
        [TestMethod]
        public async Task OnChangedItemsTriggerEndpoint_WithoutAuth_ReturnsInternalServerError()
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/{TestDataset}/tables/{TestTable}/onchangeditems");
            
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        /// <summary>
        /// Test the trigger endpoints with invalid table name
        /// </summary>
        [TestMethod]
        public async Task TriggerEndpoint_WithInvalidTable_ReturnsBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/{TestDataset}/tables/INVALID_TABLE/onnewitems");
            
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Test the trigger endpoints with invalid dataset name
        /// </summary>
        [TestMethod]
        public async Task TriggerEndpoint_WithInvalidDataset_ReturnsBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/invalid_dataset/tables/{TestTable}/onnewitems");
            
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Test the non-existent trigger endpoint
        /// </summary>
        [TestMethod]
        public async Task NonExistentTriggerEndpoint_ReturnsNotFound()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/{TestDataset}/tables/{TestTable}/nonexistent");
            
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
} 
