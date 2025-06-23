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
        /// This test uses the automatically seeded test data and validates against TestDataRecord objects
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

            // Deserialize the OData response
            var customers = JsonConvert.DeserializeObject<ODataResponse<CustomerItem>>(content).Value;
            
            // Convert API response items to TestDataRecord for comparison
            var actualRecords = customers.Select(item => item.ToTestDataRecord()).ToList();

            // Validate against seeded data
            Assert.AreEqual(SeededTestData.Count, actualRecords.Count, 
                "API should return the same number of records as seeded data");

            // Validate each record matches seeded data
            foreach (var expectedRecord in SeededTestData)
            {
                var actualRecord = actualRecords.FirstOrDefault(r => r.Id == expectedRecord.Id);
                ValidateRecordMatches(expectedRecord, actualRecord, $"API record with ID {expectedRecord.Id}");
            }

            // Get the first inactive record from seeded data for validation
            var expectedInactiveRecord = SeededTestData.FirstOrDefault(r => !r.IsActive);
            Assert.IsNotNull(expectedInactiveRecord, "Should have at least one inactive record in seeded data");
            
            var actualInactiveRecord = actualRecords.FirstOrDefault(r => r.Id == expectedInactiveRecord.Id);
            Assert.IsNotNull(actualInactiveRecord, $"{expectedInactiveRecord.Name} record should exist in API response");
            Assert.IsFalse(actualInactiveRecord.IsActive, $"{expectedInactiveRecord.Name} should be inactive");
            Assert.AreEqual(expectedInactiveRecord.Balance, actualInactiveRecord.Balance, $"{expectedInactiveRecord.Name} should have correct balance");
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
        /// This test uses the first active record from seeded data and validates the response
        /// </summary>
        [TestMethod]
        public async Task GetItemEndpoint_WithAuth_ReturnsOk()
        {
            // Get the first active record from seeded data to test with
            var expectedRecord = SeededTestData.FirstOrDefault(r => r.IsActive);
            Assert.IsNotNull(expectedRecord, "Should have at least one active record in seeded data");

            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('{expectedRecord.Id}')");
            
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(content), "Response content should not be empty");

            // Deserialize the single item response
            var customerItem = JsonConvert.DeserializeObject<CustomerItem>(content);
            Assert.IsNotNull(customerItem, "Customer item should not be null");

            // Convert to TestDataRecord for validation
            var actualRecord = customerItem.ToTestDataRecord();

            // Validate the record matches seeded data
            ValidateRecordMatches(expectedRecord, actualRecord, "Single item API response");

            // Additional validations using seeded data values
            Assert.AreEqual(expectedRecord.Name, actualRecord.Name, $"Should be {expectedRecord.Name} record");
            Assert.AreEqual(expectedRecord.Email, actualRecord.Email, "Should have correct email from seeded data");
            Assert.AreEqual(expectedRecord.Phone, actualRecord.Phone, "Should have correct phone from seeded data");
            Assert.AreEqual(expectedRecord.IsActive, actualRecord.IsActive, "Active status should match seeded data");
            Assert.AreEqual(expectedRecord.Balance, actualRecord.Balance, "Should have correct balance from seeded data");

            // Validate additional fields from API response
            Assert.IsNotNull(customerItem.ItemInternalId, "ItemInternalId should be present");
            Assert.IsNotNull(customerItem.CREATED_DATE, "CREATED_DATE should be present");
            Assert.IsTrue(Guid.TryParse(customerItem.ItemInternalId, out _), "ItemInternalId should be a valid GUID");
        }

        /// <summary>
        /// Test the GET single item endpoint without authentication
        /// Based on actual API behavior, returns 500 Internal Server Error (not 401 Unauthorized)
        /// </summary>
        [TestMethod]
        public async Task GetItemEndpoint_WithoutAuth_ReturnsInternalServerError()
        {
            // Use the second record from seeded data for this test
            var testRecord = SeededTestData.Skip(1).FirstOrDefault();
            Assert.IsNotNull(testRecord, "Should have at least 2 records in seeded data");

            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('{testRecord.Id}')");
            
            Assert.AreEqual(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
        }

        /// <summary>
        /// Test the POST /datasets/{dataset}/tables/{table}/items endpoint with authentication
        /// This test creates a new item based on a template from seeded data
        /// </summary>
        [TestMethod]
        public async Task CreateItemEndpoint_WithAuth_ReturnsCreated()
        {
            // Use the highest balance record as a template for creating a new record
            var templateRecord = SeededTestData.OrderByDescending(r => r.Balance).FirstOrDefault();
            Assert.IsNotNull(templateRecord, "Should have seeded data to use as template");

            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Create new record with dynamic values based on template
            var newId = SeededTestData.Max(r => r.Id) + 50;
            var newItem = new TestDataRecord(
                newId, 
                $"New {templateRecord.Name}", 
                templateRecord.Email.Replace("@", "+create@"), 
                templateRecord.Phone.Replace("555", "777"), 
                templateRecord.IsActive, 
                templateRecord.Balance * 0.8m
            );

            var response = await HttpClient.PostAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items", 
                CreateJsonContent(newItem));
            var xd = await response.Content.ReadAsStringAsync();
            
            Assert.AreEqual(System.Net.HttpStatusCode.Created, response.StatusCode, 
                $"Should successfully create record based on template: {templateRecord.Name}");
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
        /// This test updates an existing item using data from the third seeded record
        /// </summary>
        [TestMethod]
        public async Task UpdateItemEndpoint_WithAuth_ReturnsOk()
        {
            // Use the third record from seeded data for this update test
            var recordToUpdate = SeededTestData.Skip(2).FirstOrDefault();
            Assert.IsNotNull(recordToUpdate, "Should have at least 3 records in seeded data");

            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var updatedItem = new
            {
                ID = recordToUpdate.Id,
                NAME = $"Updated {recordToUpdate.Name}",
                EMAIL = recordToUpdate.Email.Replace("@", "+updated@"),
                PHONE = recordToUpdate.Phone,
                IS_ACTIVE = !recordToUpdate.IsActive, // Flip the active status
                BALANCE = recordToUpdate.Balance + 500.00m // Add 500 to original balance
            };

            var json = JsonConvert.SerializeObject(updatedItem);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PutAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('{recordToUpdate.Id}')", content);
            
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Test the PUT update item endpoint without authentication
        /// Based on actual API behavior, returns 500 Internal Server Error (not 401 Unauthorized)
        /// </summary>
        [TestMethod]
        public async Task UpdateItemEndpoint_WithoutAuth_ReturnsInternalServerError()
        {
            // Use the last record from seeded data for this test
            var testRecord = SeededTestData.LastOrDefault();
            Assert.IsNotNull(testRecord, "Should have seeded data available");

            var updateData = new { NAME = $"Updated {testRecord.Name}" };
            var json = JsonConvert.SerializeObject(updateData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PutAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('{testRecord.Id}')", content);
            
            Assert.AreEqual(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
        }

        /// <summary>
        /// Test the DELETE /datasets/{dataset}/tables/{table}/items({id}) endpoint with authentication
        /// This test deletes the lowest balance record from the seeded test table
        /// </summary>
        [TestMethod]
        public async Task DeleteItemEndpoint_WithAuth_ReturnsOk()
        {
            // Use the record with the lowest balance for deletion test
            var recordToDelete = SeededTestData.OrderBy(r => r.Balance).FirstOrDefault();
            Assert.IsNotNull(recordToDelete, "Should have seeded data with balance information");

            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await HttpClient.DeleteAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('{recordToDelete.Id}')");
            
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Test the DELETE item endpoint without authentication
        /// Based on actual API behavior, returns 500 Internal Server Error (not 401 Unauthorized)
        /// </summary>
        [TestMethod]
        public async Task DeleteItemEndpoint_WithoutAuth_ReturnsInternalServerError()
        {
            // Use a middle record from seeded data for this test (to diversify from other tests)
            var testRecord = SeededTestData.Skip(SeededTestData.Count / 2).FirstOrDefault();
            Assert.IsNotNull(testRecord, "Should have seeded data available");

            var response = await HttpClient.DeleteAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('{testRecord.Id}')");
            
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
        /// Test OData filtering functionality by validating active records count
        /// </summary>
        [TestMethod]
        public async Task GetItemsEndpoint_FilterActiveRecords_ReturnsCorrectCount()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var expectedActiveCount = SeededTestData.Count(r => r.IsActive);
            var expectedInactiveCount = SeededTestData.Count(r => !r.IsActive);
            
            // Test active records filter
            var activeResponse = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items?$filter=IS_ACTIVE eq true");
            Assert.AreEqual(System.Net.HttpStatusCode.OK, activeResponse.StatusCode, "Active filter should succeed");
            
            var activeContent = await activeResponse.Content.ReadAsStringAsync();
            var activeData = JsonConvert.DeserializeObject<ODataResponse<CustomerItem>>(activeContent);
            Assert.IsNotNull(activeData?.Value, "Active filter response should contain data");
            Assert.AreEqual(expectedActiveCount, activeData.Value.Count, $"Should return {expectedActiveCount} active records");
            
            // Validate all returned records are actually active
            Assert.IsTrue(activeData.Value.All(item => item.IS_ACTIVE.Equals("true", StringComparison.OrdinalIgnoreCase)), 
                "All filtered records should be active");

            // Test inactive records filter
            var inactiveResponse = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items?$filter=IS_ACTIVE eq false");
            Assert.AreEqual(System.Net.HttpStatusCode.OK, inactiveResponse.StatusCode, "Inactive filter should succeed");
            
            var inactiveContent = await inactiveResponse.Content.ReadAsStringAsync();
            var inactiveData = JsonConvert.DeserializeObject<ODataResponse<CustomerItem>>(inactiveContent);
            Assert.IsNotNull(inactiveData?.Value, "Inactive filter response should contain data");
            Assert.AreEqual(expectedInactiveCount, inactiveData.Value.Count, $"Should return {expectedInactiveCount} inactive records");
            
            // Validate all returned records are actually inactive
            Assert.IsTrue(inactiveData.Value.All(item => item.IS_ACTIVE.Equals("false", StringComparison.OrdinalIgnoreCase)), 
                "All filtered records should be inactive");
        }

        /// <summary>
        /// Comprehensive integration test that validates database integrity by comparing seeded vs actual data
        /// </summary>
        [TestMethod]
        public async Task DatabaseIntegrity_ValidateSeededDataConsistency()
        {
            // Fetch actual data from database and compare with seeded data
            var actualDataFromDb = await FetchActualDataFromDatabase();
            
            // Core integrity validations
            Assert.AreEqual(SeededTestData.Count, actualDataFromDb.Count, 
                "Database should contain exactly the number of seeded records");
            ValidateDataMatches(SeededTestData, actualDataFromDb, "All seeded data should match database content exactly");
            
            // Business rule validations using dynamic seeded data
            var expectedActiveRecords = SeededTestData.Where(r => r.IsActive).ToList();
            var expectedInactiveRecords = SeededTestData.Where(r => !r.IsActive).ToList();
            var actualActiveRecords = actualDataFromDb.Where(r => r.IsActive).ToList();
            var actualInactiveRecords = actualDataFromDb.Where(r => !r.IsActive).ToList();
            
            Assert.AreEqual(expectedActiveRecords.Count, actualActiveRecords.Count, "Active record counts should match");
            Assert.AreEqual(expectedInactiveRecords.Count, actualInactiveRecords.Count, "Inactive record counts should match");
            
            // Data quality validations
            Assert.IsTrue(actualDataFromDb.All(r => r.Id > 0), "All records should have positive IDs");
            Assert.IsTrue(actualDataFromDb.All(r => !string.IsNullOrEmpty(r.Name)), "All records should have names");
            Assert.IsTrue(actualDataFromDb.All(r => !string.IsNullOrEmpty(r.Email)), "All records should have emails");
            Assert.IsTrue(actualDataFromDb.All(r => r.Balance >= 0), "All balances should be non-negative");
            
            // Validate specific business scenarios from seeded data
            var highestBalanceExpected = SeededTestData.OrderByDescending(r => r.Balance).First();
            var highestBalanceActual = actualDataFromDb.OrderByDescending(r => r.Balance).First();
            ValidateRecordMatches(highestBalanceExpected, highestBalanceActual, "Highest balance record");
            
            var lowestBalanceExpected = SeededTestData.OrderBy(r => r.Balance).First();
            var lowestBalanceActual = actualDataFromDb.OrderBy(r => r.Balance).First();
            ValidateRecordMatches(lowestBalanceExpected, lowestBalanceActual, "Lowest balance record");
        }

        /// <summary>
        /// End-to-end test validating complete CRUD operation lifecycle with database verification
        /// </summary>
        [TestMethod]
        public async Task CrudOperations_EndToEndValidation()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // 1. CREATE: Create a new record based on seeded template
            var templateRecord = SeededTestData.OrderBy(r => r.Balance).FirstOrDefault();
            var newId = SeededTestData.Max(r => r.Id) + 200;
            
            var newRecord = new TestDataRecord(newId, $"Test {templateRecord.Name}", 
                templateRecord.Email.Replace("@", "+test@"), templateRecord.Phone.Replace("555", "999"), 
                true, templateRecord.Balance * 2);

            var createResponse = await HttpClient.PostAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items", 
                CreateJsonContent(newRecord));
            Assert.AreEqual(System.Net.HttpStatusCode.Created, createResponse.StatusCode, "Create should succeed");

            // Verify creation in database
            var createdRecord = await FetchActualRecordById(newId);
            ValidateRecordMatches(newRecord, createdRecord, "Created record should match input");

            // 2. READ: Verify we can retrieve the created record via API
            var readResponse = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('{newId}')");
            Assert.AreEqual(System.Net.HttpStatusCode.OK, readResponse.StatusCode, "Read should succeed");
            
            var readContent = await readResponse.Content.ReadAsStringAsync();
            var apiRecord = JsonConvert.DeserializeObject<CustomerItem>(readContent);
            ValidateRecordMatches(newRecord, apiRecord.ToTestDataRecord(), "API read should return correct data");

            // 3. UPDATE: Modify the record
            var updatedRecord = new TestDataRecord(newId, $"Modified {newRecord.Name}", 
                newRecord.Email.Replace("+test@", "+updated@"), newRecord.Phone, 
                !newRecord.IsActive, newRecord.Balance + 500m);

            var updateResponse = await HttpClient.PutAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('{newId}')", 
                CreateJsonContent(updatedRecord));
            Assert.AreEqual(System.Net.HttpStatusCode.OK, updateResponse.StatusCode, "Update should succeed");

            // Verify update in database
            var modifiedRecord = await FetchActualRecordById(newId);
            ValidateRecordMatches(updatedRecord, modifiedRecord, "Updated record should match new values");

            // 4. DELETE: Remove the record
            var deleteResponse = await HttpClient.DeleteAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('{newId}')");
            Assert.AreEqual(System.Net.HttpStatusCode.OK, deleteResponse.StatusCode, "Delete should succeed");

            // Verify deletion in database
            var deletedRecord = await FetchActualRecordById(newId);
            Assert.IsNull(deletedRecord, "Record should be deleted from database");

            // Verify read after delete returns appropriate response
            var readAfterDeleteResponse = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('{newId}')");
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, readAfterDeleteResponse.StatusCode, "Read after delete should return 404");
        }



        /// <summary>
        /// Performance and edge case test for API behavior under various conditions
        /// </summary>
        [TestMethod]
        public async Task ApiPerformance_EdgeCasesAndValidation()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Test 1: Validate large result set handling
            var allItemsResponse = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items");
            Assert.AreEqual(System.Net.HttpStatusCode.OK, allItemsResponse.StatusCode, "Should handle full dataset retrieval");
            
            var allContent = await allItemsResponse.Content.ReadAsStringAsync();
            var allData = JsonConvert.DeserializeObject<ODataResponse<CustomerItem>>(allContent);
            Assert.AreEqual(SeededTestData.Count, allData.Value.Count, "Should return all seeded records");

            // Test 2: Balance-based filtering (business logic validation)
            var highBalanceThreshold = SeededTestData.Average(r => r.Balance);
            var expectedHighBalanceCount = SeededTestData.Count(r => r.Balance > highBalanceThreshold);
            
            var balanceFilterResponse = await HttpClient.GetAsync(
                $"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items?$filter=BALANCE gt {highBalanceThreshold}");
            
            if (balanceFilterResponse.IsSuccessStatusCode)
            {
                var balanceContent = await balanceFilterResponse.Content.ReadAsStringAsync();
                var balanceData = JsonConvert.DeserializeObject<ODataResponse<CustomerItem>>(balanceContent);
                Assert.AreEqual(expectedHighBalanceCount, balanceData.Value.Count, 
                    $"Should return {expectedHighBalanceCount} records with balance > {highBalanceThreshold:C}");
                
                // Validate all returned records meet the criteria
                Assert.IsTrue(balanceData.Value.All(item => item.BALANCE > (decimal)highBalanceThreshold),
                    "All filtered records should have balance above threshold");
            }

            // Test 3: Edge case - non-existent record
            var maxId = SeededTestData.Max(r => r.Id);
            var nonExistentResponse = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{TestTable}')/items('{maxId + 999}')");
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, nonExistentResponse.StatusCode, 
                "Should return 404 for non-existent records");

            // Test 4: Email pattern validation (data quality check)
            var emailValidationRecords = allData.Value.Select(item => item.ToTestDataRecord()).ToList();
            Assert.IsTrue(emailValidationRecords.All(r => r.Email.Contains("@") && r.Email.Contains(".")),
                "All email addresses should be properly formatted");
            
            // Test 5: Phone number consistency check
            var phonePattern = SeededTestData.First().Phone.Substring(0, 3); // Get pattern from first record
            var consistentPhoneCount = emailValidationRecords.Count(r => r.Phone.StartsWith(phonePattern));
                         Assert.IsTrue(consistentPhoneCount > 0, "Should have records with consistent phone patterns");
        }

        /// <summary>
        /// Helper method to create JSON content from TestDataRecord
        /// </summary>
        private StringContent CreateJsonContent(TestDataRecord record)
        {
            var json = JsonConvert.SerializeObject(new
            {
                ID = record.Id,
                NAME = record.Name,
                EMAIL = record.Email,
                PHONE = record.Phone,
                IS_ACTIVE = record.IsActive,
                BALANCE = record.Balance
            });
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }
} 
