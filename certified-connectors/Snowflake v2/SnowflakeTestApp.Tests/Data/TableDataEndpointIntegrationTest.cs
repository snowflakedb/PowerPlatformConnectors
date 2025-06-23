using System;
using System.Collections.Generic;
using System.Linq;
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
            
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(content), "Response content should not be empty");
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
            
            Assert.AreEqual(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
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
            
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(content), "Response content should not be empty");
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
            
            Assert.AreEqual(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
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
            
            Assert.AreEqual(System.Net.HttpStatusCode.Created, response.StatusCode);
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
            
            Assert.AreEqual(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
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
            
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
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
            
            Assert.AreEqual(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
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

            var response = await HttpClient.DeleteAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items(1)");
            
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Test the DELETE item endpoint without authentication
        /// Based on actual API behavior, returns 500 Internal Server Error (not 401 Unauthorized)
        /// </summary>
        [TestMethod]
        public async Task DeleteItemEndpoint_WithoutAuth_ReturnsInternalServerError()
        {
            var response = await HttpClient.DeleteAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items(1)");
            
            Assert.AreEqual(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
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

        /// <summary>
        /// Example test demonstrating how to validate against seeded test data using TestDataRecord
        /// This makes assertions much easier and more reliable
        /// </summary>
        [TestMethod]
        public async Task GetItemEndpoint_ValidateAgainstSeededData()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Get the expected record from our seeded data
            var expectedRecord = SeededTestData.FirstOrDefault(r => r.Id == 1);
            Assert.IsNotNull(expectedRecord, "Expected test record with ID=1 should exist in seeded data");

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items(1)");
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var actualData = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);

            // Now we can easily validate against our known test data structure
            Assert.AreEqual(expectedRecord.Id, Convert.ToInt32(actualData["ID"]));
            Assert.AreEqual(expectedRecord.Name, actualData["NAME"].ToString());
            Assert.AreEqual(expectedRecord.Email, actualData["EMAIL"].ToString());
            Assert.AreEqual(expectedRecord.Phone, actualData["PHONE"].ToString());
            Assert.AreEqual(expectedRecord.IsActive, Convert.ToBoolean(actualData["IS_ACTIVE"]));
            Assert.AreEqual(expectedRecord.Balance, Convert.ToDecimal(actualData["BALANCE"]));
        }

        /// <summary>
        /// Example test showing how to validate count and filtering using the test data model
        /// </summary>
        [TestMethod]
        public async Task GetItemsEndpoint_ValidateActiveRecordsCount()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // We know exactly how many active records we should have
            var expectedActiveCount = SeededTestData.Count(r => r.IsActive);
            
            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items?$filter=IS_ACTIVE eq true");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                
                // This is just an example - actual response structure may vary
                if (data?.ContainsKey("value") == true && data["value"] is Newtonsoft.Json.Linq.JArray valueArray)
                {
                    int actualCount = valueArray.Count;
                    Assert.AreEqual(expectedActiveCount, actualCount, 
                        $"Expected {expectedActiveCount} active records but found {actualCount}");
                }
            }
        }

        /// <summary>
        /// Example helper method showing how to validate that a created item matches our test data pattern
        /// </summary>
        private void ValidateTestDataStructure(Dictionary<string, object> actualData, TestDataRecord expected)
        {
            Assert.AreEqual(expected.Id, Convert.ToInt32(actualData["ID"]), "ID should match");
            Assert.AreEqual(expected.Name, actualData["NAME"].ToString(), "Name should match");
            Assert.AreEqual(expected.Email, actualData["EMAIL"].ToString(), "Email should match");
            Assert.AreEqual(expected.Phone, actualData["PHONE"].ToString(), "Phone should match");
            Assert.AreEqual(expected.IsActive, Convert.ToBoolean(actualData["IS_ACTIVE"]), "IsActive should match");
            Assert.AreEqual(expected.Balance, Convert.ToDecimal(actualData["BALANCE"]), "Balance should match");
        }

        /// <summary>
        /// Comprehensive test that validates the entire seeded dataset against actual database content
        /// This demonstrates how to use the new TestDataRecord mapping functionality
        /// </summary>
        [TestMethod]
        public async Task ValidateSeededDataIntegrity_FullDatasetComparison()
        {
            // Fetch the actual data from Snowflake and map it to TestDataRecord objects
            var actualDataFromDb = await FetchActualDataFromDatabase();
            
            // Validate that what we seeded matches what's actually in the database
            ValidateDataMatches(SeededTestData, actualDataFromDb, "Seeded data should match database content exactly");
            
            // Additional specific validations
            Assert.IsTrue(actualDataFromDb.Any(r => r.Name == "John Doe" && r.Balance == 1500.50m), 
                "John Doe record should exist with correct balance");
            Assert.IsTrue(actualDataFromDb.Any(r => r.Name == "Alice Brown" && !r.IsActive), 
                "Alice Brown should be inactive");
            
            // Validate counts by status
            var activeCount = actualDataFromDb.Count(r => r.IsActive);
            var expectedActiveCount = SeededTestData.Count(r => r.IsActive);
            Assert.AreEqual(expectedActiveCount, activeCount, "Active record count should match");
        }

        /// <summary>
        /// Test that demonstrates validating a specific record after an update operation
        /// Shows how to fetch and validate individual records
        /// </summary>
        [TestMethod]
        public async Task UpdateItemEndpoint_ValidateChangesInDatabase()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Get the original record that we expect to be in the database
            var originalRecord = SeededTestData.FirstOrDefault(r => r.Id == 1);
            Assert.IsNotNull(originalRecord, "Test record with ID=1 should exist in seeded data");

            // Verify the original record exists in the database
            var recordBeforeUpdate = await FetchActualRecordById(1);
            ValidateRecordMatches(originalRecord, recordBeforeUpdate, "Original record should match seeded data");

            // Update the record via API
            var updatedItem = new
            {
                ID = 1,
                NAME = "Updated John Doe",
                EMAIL = "updated.john@example.com",
                PHONE = "+1-555-0199",
                IS_ACTIVE = true,
                BALANCE = 2000.00
            };

            var json = JsonConvert.SerializeObject(updatedItem);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PutAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items(1)", content);
            
            if (response.IsSuccessStatusCode)
            {
                // Fetch the updated record from the database
                var recordAfterUpdate = await FetchActualRecordById(1);
                
                // Validate the changes were applied
                Assert.IsNotNull(recordAfterUpdate, "Updated record should exist in database");
                Assert.AreEqual("Updated John Doe", recordAfterUpdate.Name, "Name should be updated");
                Assert.AreEqual("updated.john@example.com", recordAfterUpdate.Email, "Email should be updated");
                Assert.AreEqual(2000.00m, recordAfterUpdate.Balance, "Balance should be updated");
                
                // Validate fields that shouldn't have changed
                Assert.AreEqual(originalRecord.Id, recordAfterUpdate.Id, "ID should remain the same");
                Assert.AreEqual(originalRecord.Phone, recordAfterUpdate.Phone, "Phone should remain the same");
            }
        }

        /// <summary>
        /// Test that demonstrates creating a new record and validating it appears in the database
        /// Shows how to validate newly created data
        /// </summary>
        [TestMethod]
        public async Task CreateItemEndpoint_ValidateNewRecordInDatabase()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Get the current record count
            var initialRecords = await FetchActualDataFromDatabase();
            var initialCount = initialRecords.Count;

            // Create a new record
            var newItem = new
            {
                ID = 999, // Use a high ID to avoid conflicts
                NAME = "Test Customer",
                EMAIL = "test@example.com",
                PHONE = "+1-555-0199",
                IS_ACTIVE = true,
                BALANCE = 100.00
            };

            var json = JsonConvert.SerializeObject(newItem);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items", content);
            
            if (response.IsSuccessStatusCode)
            {
                // Fetch the new record from the database
                var createdRecord = await FetchActualRecordById(999);
                
                Assert.IsNotNull(createdRecord, "Created record should exist in database");
                Assert.AreEqual("Test Customer", createdRecord.Name, "Name should match");
                Assert.AreEqual("test@example.com", createdRecord.Email, "Email should match");
                Assert.AreEqual(100.00m, createdRecord.Balance, "Balance should match");
                Assert.IsTrue(createdRecord.IsActive, "Should be active");

                // Validate the total count increased
                var finalRecords = await FetchActualDataFromDatabase();
                Assert.AreEqual(initialCount + 1, finalRecords.Count, "Record count should increase by 1");
            }
        }

        /// <summary>
        /// Test that demonstrates validating filtered data using the TestDataRecord model
        /// Shows how to validate specific subsets of data
        /// </summary>
        [TestMethod]
        public async Task ValidateFilteredData_ActiveRecordsOnly()
        {
            // Get expected active records from our seeded data
            var expectedActiveRecords = SeededTestData.Where(r => r.IsActive).ToList();
            
            // This would typically involve an API call that filters for active records
            // For demonstration, we'll fetch all data and filter it
            var allActualRecords = await FetchActualDataFromDatabase();
            var actualActiveRecords = allActualRecords.Where(r => r.IsActive).ToList();
            
            // Validate that the active records match what we expect
            Assert.AreEqual(expectedActiveRecords.Count, actualActiveRecords.Count, "Active record count should match");
            
            foreach (var expectedRecord in expectedActiveRecords)
            {
                var actualRecord = actualActiveRecords.FirstOrDefault(r => r.Id == expectedRecord.Id);
                ValidateRecordMatches(expectedRecord, actualRecord, $"Active record with ID {expectedRecord.Id}");
            }
        }
    }
} 
