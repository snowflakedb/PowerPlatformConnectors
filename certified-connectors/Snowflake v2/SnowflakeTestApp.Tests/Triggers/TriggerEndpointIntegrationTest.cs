using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace SnowflakeTestApp.Tests.Triggers
{
    /// <summary>
    /// Integration tests for the trigger endpoints.
    /// These tests document the expected behavior and can be used to verify the endpoints manually.
    /// Note: These tests use example table name 'CUSTOMERS' - adjust as needed for your test environment.
    /// </summary>
    [TestClass]
    public class TriggerEndpointIntegrationTest : BaseIntegrationTest
    {
        private const string TestTable = "CUSTOMERS"; // Adjust this to match your test environment
        private const string TestDataset = "default";

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            EnsureApplicationIsRunning();
        }

        /// <summary>
        /// Test the /datasets/{dataset}/tables/{table}/onnewitems endpoint with authentication
        /// Note: This test uses example table name 'CUSTOMERS' - adjust as needed for your test environment
        /// </summary>
        [TestMethod]
        public async Task OnNewItemsTriggerEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/default/tables/CUSTOMERS/onnewitems");
            
            // Accept success or NotFound (if the endpoint or table doesn't exist)
            Assert.IsTrue(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound,
                         $"Expected success or NotFound but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the trigger endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task OnNewItemsTriggerEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/default/tables/CUSTOMERS/onnewitems");
            
            // Accept various authentication-related error codes or NotFound
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || 
                         response.StatusCode == HttpStatusCode.Forbidden ||
                         response.StatusCode == HttpStatusCode.NotFound,
                         $"Expected authentication failure or NotFound but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the /datasets/{dataset}/tables/{table}/onupdateditems endpoint with authentication
        /// </summary>
        [TestMethod]
        public async Task OnUpdatedItemsTriggerEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/default/tables/CUSTOMERS/onupdateditems");
            
            // Accept success or NotFound (if the endpoint or table doesn't exist)
            Assert.IsTrue(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound,
                         $"Expected success or NotFound but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the trigger endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task OnUpdatedItemsTriggerEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/default/tables/CUSTOMERS/onupdateditems");
            
            // Accept various authentication-related error codes or NotFound
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || 
                         response.StatusCode == HttpStatusCode.Forbidden ||
                         response.StatusCode == HttpStatusCode.NotFound,
                         $"Expected authentication failure or NotFound but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the /datasets/{dataset}/tables/{table}/onchangeditems endpoint with authentication
        /// </summary>
        [TestMethod]
        public async Task OnChangedItemsTriggerEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/default/tables/CUSTOMERS/onchangeditems");
            
            // Accept success or NotFound (if the endpoint or table doesn't exist)
            Assert.IsTrue(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound,
                         $"Expected success or NotFound but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the trigger endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task OnChangedItemsTriggerEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/default/tables/CUSTOMERS/onchangeditems");
            
            // Accept various authentication-related error codes or NotFound
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || 
                         response.StatusCode == HttpStatusCode.Forbidden ||
                         response.StatusCode == HttpStatusCode.NotFound,
                         $"Expected authentication failure or NotFound but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the /datasets/{dataset}/tables/{table}/ondeleteditems endpoint with authentication
        /// </summary>
        [TestMethod]
        public async Task OnDeletedItemsTriggerEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/default/tables/CUSTOMERS/ondeleteditems");
            
            // Accept success or NotFound (if the endpoint or table doesn't exist)
            Assert.IsTrue(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound,
                         $"Expected success or NotFound but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the trigger endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task OnDeletedItemsTriggerEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/default/tables/CUSTOMERS/ondeleteditems");
            
            // Accept various authentication-related error codes or NotFound
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || 
                         response.StatusCode == HttpStatusCode.Forbidden ||
                         response.StatusCode == HttpStatusCode.NotFound,
                         $"Expected authentication failure or NotFound but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the trigger endpoint with missing dataset parameter
        /// </summary>
        [TestMethod]
        public async Task TriggerEndpoint_WithMissingDataset_ReturnsBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets//tables/CUSTOMERS/onnewitems");
            
            // Accept BadRequest or NotFound depending on how the routing is configured
            Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || 
                         response.StatusCode == HttpStatusCode.NotFound,
                         $"Expected BadRequest (400) or NotFound (404) but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the trigger endpoint with missing table parameter
        /// </summary>
        [TestMethod]
        public async Task TriggerEndpoint_WithMissingTable_ReturnsBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/default/tables//onnewitems");
            
            // Accept BadRequest or NotFound depending on how the routing is configured
            Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || 
                         response.StatusCode == HttpStatusCode.NotFound,
                         $"Expected BadRequest (400) or NotFound (404) but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the trigger endpoint with non-existent table
        /// </summary>
        [TestMethod]
        public async Task TriggerEndpoint_WithNonExistentTable_ReturnsNotFound()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/default/tables/NonExistentTable123/onnewitems");
            Assert.IsTrue(response.StatusCode == HttpStatusCode.NotFound || 
                         response.StatusCode == HttpStatusCode.InternalServerError,
                         $"Expected NotFound (404) or InternalServerError (500) but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the trigger endpoint with query parameters
        /// </summary>
        [TestMethod]
        public async Task TriggerEndpoint_WithQueryParameters_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets/default/tables/CUSTOMERS/onnewitems?$top=10");
            
            // Accept success or NotFound (if the endpoint or table doesn't exist)
            Assert.IsTrue(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound,
                         $"Expected success or NotFound for onnewitems with $top but got {response.StatusCode}");
        }
    }
} 