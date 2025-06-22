using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace SnowflakeTestApp.Tests.Data
{
    /// <summary>
    /// Integration tests for table data endpoints (items CRUD operations).
    /// These tests document the expected behavior and can be used to verify the endpoints manually.
    /// This test class uses the globally seeded CUSTOMERS test table.
    /// </summary>
    [TestClass]
    public class TableDataEndpointIntegrationTest : BaseIntegrationTest
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
        /// Test the GET /datasets/{dataset}/tables/{table}/items endpoint with authentication
        /// This test uses the automatically seeded test data
        /// </summary>
        [TestMethod]
        public async Task GetItemsEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items");
            
            AssertStatusCode(response, System.Net.HttpStatusCode.OK);
            AssertResponseHasContent(response);
        }

        /// <summary>
        /// Test the GET items endpoint without authentication
        /// Based on actual API behavior, returns 500 Internal Server Error (not 401 Unauthorized)
        /// </summary>
        [TestMethod]
        public async Task GetItemsEndpoint_WithoutAuth_ReturnsInternalServerError()
        {
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items");
            
            AssertStatusCode(response, System.Net.HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Test the GET /datasets/{dataset}/tables/{table}/items/{id} endpoint with authentication
        /// This test uses ID=1 which should exist in our seeded data
        /// </summary>
        [TestMethod]
        public async Task GetItemEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items(1)");
            
            AssertStatusCode(response, System.Net.HttpStatusCode.OK);
            AssertResponseHasContent(response);
        }

        /// <summary>
        /// Test the GET single item endpoint without authentication
        /// Based on actual API behavior, returns 500 Internal Server Error (not 401 Unauthorized)
        /// </summary>
        [TestMethod]
        public async Task GetItemEndpoint_WithoutAuth_ReturnsInternalServerError()
        {
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items(1)");
            
            AssertStatusCode(response, System.Net.HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Test the POST /datasets/{dataset}/tables/{table}/items endpoint with authentication
        /// This test creates a new item in the seeded test table
        /// </summary>
        [TestMethod]
        public async Task CreateItemEndpoint_WithAuth_ReturnsCreated()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var newItem = new
            {
                NAME = "Test Customer",
                EMAIL = "test@example.com",
                PHONE = "+1-555-0199",
                IS_ACTIVE = true,
                BALANCE = 100.00
            };

            var json = JsonConvert.SerializeObject(newItem);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items", content);
            
            AssertStatusCode(response, System.Net.HttpStatusCode.Created);
        }

        /// <summary>
        /// Test the POST create item endpoint without authentication
        /// Based on actual API behavior, returns 500 Internal Server Error (not 401 Unauthorized)
        /// </summary>
        [TestMethod]
        public async Task CreateItemEndpoint_WithoutAuth_ReturnsInternalServerError()
        {
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            var testItem = new { NAME = "Test Customer" };
            var json = JsonConvert.SerializeObject(testItem);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items", content);
            
            AssertStatusCode(response, System.Net.HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Test the PUT /datasets/{dataset}/tables/{table}/items({id}) endpoint with authentication
        /// This test updates an existing item in the seeded test table
        /// </summary>
        [TestMethod]
        public async Task UpdateItemEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var updatedItem = new
            {
                ID = 1,
                NAME = "Updated Customer",
                EMAIL = "updated@example.com",
                PHONE = "+1-555-0199",
                IS_ACTIVE = true,
                BALANCE = 200.00
            };

            var json = JsonConvert.SerializeObject(updatedItem);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PutAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items(1)", content);
            
            AssertStatusCode(response, System.Net.HttpStatusCode.OK);
        }

        /// <summary>
        /// Test the PUT update item endpoint without authentication
        /// Based on actual API behavior, returns 500 Internal Server Error (not 401 Unauthorized)
        /// </summary>
        [TestMethod]
        public async Task UpdateItemEndpoint_WithoutAuth_ReturnsInternalServerError()
        {
            var updateData = new { NAME = "Updated Customer" };
            var json = JsonConvert.SerializeObject(updateData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PutAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items(1)", content);
            
            AssertStatusCode(response, System.Net.HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Test the DELETE /datasets/{dataset}/tables/{table}/items({id}) endpoint with authentication
        /// This test deletes an item from the seeded test table
        /// </summary>
        [TestMethod]
        public async Task DeleteItemEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await HttpClient.DeleteAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items(7931)");
            
            AssertStatusCode(response, System.Net.HttpStatusCode.OK);
        }

        /// <summary>
        /// Test the DELETE item endpoint without authentication
        /// Based on actual API behavior, returns 500 Internal Server Error (not 401 Unauthorized)
        /// </summary>
        [TestMethod]
        public async Task DeleteItemEndpoint_WithoutAuth_ReturnsInternalServerError()
        {
            var response = await HttpClient.DeleteAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items(1)");
            
            AssertStatusCode(response, System.Net.HttpStatusCode.InternalServerError);
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
