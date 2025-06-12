using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace SnowflakeTestApp.Tests
{
    /// <summary>
    /// Integration tests for table data endpoints (items CRUD operations).
    /// These tests document the expected behavior and can be used to verify the endpoints manually.
    /// Note: These tests use example table name 'CUSTOMERS' - adjust as needed for your test environment.
    /// </summary>
    [TestClass]
    public class TableDataEndpointIntegrationTest : BaseIntegrationTest
    {
        private const string TestTable = "CUSTOMERS"; // Adjust this to match your test environment
        private const string TestDataset = "default";

        /// <summary>
        /// Test the GET /datasets/{dataset}/tables/{table}/items endpoint with authentication
        /// </summary>
        [TestMethod]
        public async Task GetItemsEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items");
            Assert.IsTrue(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(content), "Response content should not be empty");
        }

        /// <summary>
        /// Test the GET items endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task GetItemsEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items");
            Assert.IsFalse(response.IsSuccessStatusCode, $"Expected failure but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the GET /datasets/{dataset}/tables/{table}/items/{id} endpoint with authentication
        /// Note: This test assumes item with ID '1' exists - adjust as needed
        /// </summary>
        [TestMethod]
        public async Task GetItemEndpoint_WithAuth_ReturnsOkOrNotFound()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('1')");
            
            // Item might exist (200) or not exist (404), both are valid responses for this test
            Assert.IsTrue(response.StatusCode == System.Net.HttpStatusCode.OK || 
                         response.StatusCode == System.Net.HttpStatusCode.NotFound,
                         $"Expected OK or NotFound but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the GET single item endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task GetItemEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('1')");
            Assert.IsFalse(response.IsSuccessStatusCode, $"Expected failure but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the POST /datasets/{dataset}/tables/{table}/items endpoint (create item)
        /// Note: This test creates a test item - ensure your test table supports this schema
        /// </summary>
        [TestMethod]
        public async Task PostItemEndpoint_WithAuth_ReturnsCreated()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var testItem = new
            {
                Name = "Test Customer",
                Email = "test@example.com"
            };

            var json = JsonConvert.SerializeObject(testItem);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items", content);
            
            // Accept both Created (201) and other success codes as the implementation may vary
            Assert.IsTrue(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the POST create item endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task PostItemEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            // Set a shorter timeout to prevent hanging
            HttpClient.Timeout = TimeSpan.FromSeconds(10);
            
            var testItem = new { Name = "Test Customer" };
            var json = JsonConvert.SerializeObject(testItem);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items", content);
            Assert.IsFalse(response.IsSuccessStatusCode, $"Expected failure but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the PATCH /datasets/{dataset}/tables/{table}/items/{id} endpoint (update item)
        /// Note: This test assumes item with ID '1' exists - adjust as needed
        /// </summary>
        [TestMethod]
        public async Task PatchItemEndpoint_WithAuth_ReturnsOkOrNotFound()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var updateData = new
            {
                Name = "Updated Customer Name"
            };

            var json = JsonConvert.SerializeObject(updateData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('1')")
            {
                Content = content
            };

            var response = await HttpClient.SendAsync(request);
            
            // Item might exist (success) or not exist (404), both are valid for this test
            Assert.IsTrue(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound,
                         $"Expected success or NotFound but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the PUT /datasets/{dataset}/tables/{table}/items/{id} endpoint (update item)
        /// Note: This test assumes item with ID '1' exists - adjust as needed
        /// </summary>
        [TestMethod]
        public async Task PutItemEndpoint_WithAuth_ReturnsOkOrNotFound()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var updateData = new
            {
                Name = "Updated Customer Name via PUT",
                Email = "updated@example.com"
            };

            var json = JsonConvert.SerializeObject(updateData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PutAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('1')", content);
            
            // Item might exist (success) or not exist (404), both are valid for this test
            Assert.IsTrue(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound,
                         $"Expected success or NotFound but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the DELETE /datasets/{dataset}/tables/{table}/items/{id} endpoint
        /// Note: This test assumes item with ID '999' either exists or doesn't exist
        /// </summary>
        [TestMethod]
        public async Task DeleteItemEndpoint_WithAuth_ReturnsOkOrNotFound()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.DeleteAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('999')");
            
            // Item might exist (success) or not exist (404), both are valid for this test
            Assert.IsTrue(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound,
                         $"Expected success or NotFound but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the DELETE item endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task DeleteItemEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            var response = await HttpClient.DeleteAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('1')");
            Assert.IsFalse(response.IsSuccessStatusCode, $"Expected failure but got {response.StatusCode}");
        }

        /// <summary>
        /// Test items endpoint with missing dataset parameter
        /// </summary>
        [TestMethod]
        public async Task GetItemsEndpoint_WithMissingDataset_ReturnsBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets('')/tables('{TestTable}')/items");
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode, "Expected HTTP 400 Bad Request");
        }

        /// <summary>
        /// Test items endpoint with missing table parameter
        /// </summary>
        [TestMethod]
        public async Task GetItemsEndpoint_WithMissingTable_ReturnsBadRequest()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('')/items");
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode, "Expected HTTP 400 Bad Request");
        }
    }
} 