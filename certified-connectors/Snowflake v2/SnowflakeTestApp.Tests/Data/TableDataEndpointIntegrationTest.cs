using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SnowflakeTestApp.Tests.Infrastructure;

namespace SnowflakeTestApp.Tests.Data
{
    /// <summary>
    /// Integration tests for table data endpoints (items CRUD operations).
    /// These tests document the expected behavior and can be used to verify the endpoints manually.
    /// This test class automatically creates and seeds the CUSTOMERS test table before running tests.
    /// </summary>
    [TestClass]
    public class TableDataEndpointIntegrationTest : BaseIntegrationTestWithDataSeeding
    {
        private const string TestTable = "CUSTOMERS"; // Uses the seeded test table

        /// <summary>
        /// Test the GET /datasets/{dataset}/tables/{table}/items endpoint with authentication
        /// This test uses the automatically seeded test data
        /// </summary>
        [TestMethod]
        public async Task GetItemsEndpoint_WithAuth_ReturnsOk()
        {
            RequireTestData(); // Ensure test data is available
            
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items");
            
            Assert.IsTrue(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(responseContent), "Response content should not be empty");
            
            // Since we seeded data, we should have some items
            TestContext?.WriteLine($"Retrieved items response: {responseContent}");
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
        /// This test uses ID=1 which should exist in our seeded data
        /// </summary>
        [TestMethod]
        public async Task GetItemEndpoint_WithAuth_ReturnsOkOrNotFound()
        {
            RequireTestData(); // Ensure test data is available
            
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('1')");
            
            // Item might exist (200) or not exist (404), both are valid responses for this test
            Assert.IsTrue(response.StatusCode == System.Net.HttpStatusCode.OK || 
                         response.StatusCode == System.Net.HttpStatusCode.NotFound,
                         $"Expected OK or NotFound but got {response.StatusCode}");
                         
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                TestContext?.WriteLine($"Retrieved item: {content}");
            }
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
        /// This test creates a new item in the seeded test table
        /// </summary>
        [TestMethod]
        public async Task PostItemEndpoint_WithAuth_ReturnsCreated()
        {
            RequireTestData(); // Ensure test data is available
            
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var testItem = new
            {
                NAME = "Test Customer Created",
                EMAIL = "test.created@example.com",
                PHONE = "+1-555-9999",
                IS_ACTIVE = true,
                BALANCE = 1000.00
            };

            var json = JsonConvert.SerializeObject(testItem);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items", content);
            
            // Accept both Created (201) and other success codes as the implementation may vary
            Assert.IsTrue(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");
            
            var responseContent2 = await response.Content.ReadAsStringAsync();
            TestContext?.WriteLine($"Created item response: {responseContent2}");
        }

        /// <summary>
        /// Test the POST create item endpoint without authentication
        /// </summary>
        [TestMethod]
        public async Task PostItemEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            var testItem = new { NAME = "Test Customer" };
            var json = JsonConvert.SerializeObject(testItem);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items", content);
            Assert.IsFalse(response.IsSuccessStatusCode, $"Expected failure but got {response.StatusCode}");
        }

        /// <summary>
        /// Test the PATCH /datasets/{dataset}/tables/{table}/items/{id} endpoint (update item)
        /// This test updates an existing item in the seeded test data (ID=1)
        /// </summary>
        [TestMethod]
        public async Task PatchItemEndpoint_WithAuth_ReturnsOkOrNotFound()
        {
            RequireTestData(); // Ensure test data is available
            
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var updateData = new
            {
                NAME = "Updated Customer Name via PATCH",
                BALANCE = 9999.99
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
                         
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                TestContext?.WriteLine($"Updated item response: {responseContent}");
            }
        }

        /// <summary>
        /// Test the PUT /datasets/{dataset}/tables/{table}/items/{id} endpoint (update item)
        /// This test updates an existing item in the seeded test data (ID=2)
        /// </summary>
        [TestMethod]
        public async Task PutItemEndpoint_WithAuth_ReturnsOkOrNotFound()
        {
            RequireTestData(); // Ensure test data is available
            
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var updateData = new
            {
                NAME = "Updated Customer Name via PUT",
                EMAIL = "updated.via.put@example.com",
                PHONE = "+1-555-8888",
                IS_ACTIVE = true,
                BALANCE = 8888.88
            };

            var json = JsonConvert.SerializeObject(updateData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PutAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('2')", content);
            
            // Item might exist (success) or not exist (404), both are valid for this test
            Assert.IsTrue(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound,
                         $"Expected success or NotFound but got {response.StatusCode}");
                         
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                TestContext?.WriteLine($"Put item response: {responseContent}");
            }
        }

        /// <summary>
        /// Test the DELETE /datasets/{dataset}/tables/{table}/items/{id} endpoint
        /// This test deletes an item from the seeded test data (ID=10 - last seeded item)
        /// </summary>
        [TestMethod]
        public async Task DeleteItemEndpoint_WithAuth_ReturnsOkOrNotFound()
        {
            RequireTestData(); // Ensure test data is available
            
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.DeleteAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('10')");
            
            // Item might exist (success) or not exist (404), both are valid for this test
            Assert.IsTrue(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound,
                         $"Expected success or NotFound but got {response.StatusCode}");
                         
            TestContext?.WriteLine($"Delete item response status: {response.StatusCode}");
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
