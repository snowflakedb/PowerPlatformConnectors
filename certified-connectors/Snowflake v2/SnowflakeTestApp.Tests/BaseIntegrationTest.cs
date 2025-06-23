using System;
using System.IO;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SnowflakeTestApp.Tests.Infrastructure;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace SnowflakeTestApp.Tests
{
    /// <summary>
    /// Base class for integration tests that provides common functionality
    /// Data is seeded once before all tests run
    /// </summary>
    [TestClass]
    public abstract class BaseIntegrationTest
    {
        protected string BaseUrl => TestData.BaseUrl;
        protected HttpClient HttpClient;
        protected static TestDataSeeder DataSeeder;

        /// <summary>
        /// Gets the test records that were seeded into the database
        /// Use this in tests to validate against the expected data
        /// </summary>
        protected static List<TestDataRecord> SeededTestData => DataSeeder?.SeededRecords ?? new List<TestDataRecord>();

        /// <summary>
        /// Initializes test data once before all tests in the assembly run
        /// </summary>
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            try
            {
                // Get test token for data seeding
                var token = TestData.DefaultBearerToken;
                
                // Check if the token is configured
                if (!string.IsNullOrEmpty(token) && 
                    !token.Equals("your-token-here", StringComparison.OrdinalIgnoreCase) &&
                    !token.Equals("your-actual-bearer-token-here", StringComparison.OrdinalIgnoreCase))
                {
                    // Initialize HTTP client for data seeding - don't dispose it as DataSeeder will use it
                    var httpClient = new HttpClient
                    {
                        Timeout = TimeSpan.FromSeconds(TestData.DefaultTimeoutSeconds)
                    };
                    
                    DataSeeder = new TestDataSeeder(httpClient, TestData.BaseUrl, token);
                    
                    // Seed test data for the default table
                    var success = DataSeeder.EnsureTestTableExistsAndSeed(TestData.DefaultTable, TestData.DefaultDataset).GetAwaiter().GetResult();
                    
                    if (!success)
                    {
                        throw new InvalidOperationException($"Failed to setup test table '{TestData.DefaultTable}'");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Bearer token not configured - cannot proceed with test data setup");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Test data seeding failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cleans up test data after all tests in the assembly complete
        /// </summary>
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            try
            {
                if (DataSeeder != null)
                {
                    DataSeeder.CleanupTestTable(TestData.DefaultTable).GetAwaiter().GetResult();
                    DataSeeder.Dispose(); // Dispose the DataSeeder and its HttpClient
                }
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
        }

        [TestInitialize]
        public virtual void TestInitialize()
        {
            HttpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(TestData.DefaultTimeoutSeconds)
            };
        }

        [TestCleanup]
        public virtual void TestCleanup()
        {
            HttpClient?.Dispose();
        }

        /// <summary>
        /// Gets the test token from ConnectionParametersProviderMock
        /// </summary>
        protected string GetTestToken()
        {
            var token = TestData.DefaultBearerToken;
            
            // Check if the token is still the placeholder value
            if (string.IsNullOrEmpty(token) || 
                token.Equals("your-token-here", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("your-actual-bearer-token-here", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Inconclusive("Bearer token not configured. Please update ConnectionParametersProviderMock.TestBearerToken with a valid OAuth bearer token. " +
                                   "See README.md for instructions on generating OAuth tokens.");
            }
            
            return token;
        }

        /// <summary>
        /// Gets the TestContext for accessing test run information
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Helper method to check if the SnowflakeTestApp is running
        /// </summary>
        protected void EnsureApplicationIsRunning()
        {
            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                {
                    var response = client.GetAsync(BaseUrl).Result;
                    // We don't care about the specific response, just that we can reach the app
                }
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"SnowflakeTestApp is not running at {BaseUrl}. Please start the application before running tests. Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to deserialize JSON response content
        /// </summary>
        protected T DeserializeResponse<T>(HttpResponseMessage response)
        {
            var content = response.Content.ReadAsStringAsync().Result;
            try
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (JsonException ex)
            {
                Assert.Fail($"Failed to deserialize response content as {typeof(T).Name}. Content: {content}. Error: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Helper method to create JSON content for POST requests
        /// </summary>
        protected StringContent CreateJsonContent(object data)
        {
            var json = JsonConvert.SerializeObject(data);
            return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        /// <summary>
        /// Fetches actual data from Snowflake and returns it as TestDataRecord objects
        /// Use this to validate that the data in the database matches expectations
        /// </summary>
        /// <param name="tableName">Optional table name (defaults to the test table)</param>
        /// <returns>List of TestDataRecord objects from the database</returns>
        protected async Task<List<TestDataRecord>> FetchActualDataFromDatabase(string tableName = null)
        {
            // Create a fresh HttpClient for this operation to avoid disposal issues
            using (var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(TestData.DefaultTimeoutSeconds) })
            {
                var tempSeeder = new TestDataSeeder(httpClient, TestData.BaseUrl, TestData.DefaultBearerToken);
                return await tempSeeder.FetchTestDataFromDatabase(tableName);
            }
        }

        /// <summary>
        /// Fetches a specific record by ID from Snowflake
        /// </summary>
        /// <param name="id">ID of the record to fetch</param>
        /// <param name="tableName">Optional table name (defaults to the test table)</param>
        /// <returns>TestDataRecord if found, null otherwise</returns>
        protected async Task<TestDataRecord> FetchActualRecordById(int id, string tableName = null)
        {
            // Create a fresh HttpClient for this operation to avoid disposal issues
            using (var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(TestData.DefaultTimeoutSeconds) })
            {
                var tempSeeder = new TestDataSeeder(httpClient, TestData.BaseUrl, TestData.DefaultBearerToken);
                return await tempSeeder.FetchTestRecordById(id, tableName);
            }
        }

        /// <summary>
        /// Validates that the actual data from database matches the expected seeded data
        /// </summary>
        /// <param name="expectedRecords">Expected records (usually from SeededTestData)</param>
        /// <param name="actualRecords">Actual records from database</param>
        /// <param name="message">Optional custom assertion message</param>
        protected void ValidateDataMatches(List<TestDataRecord> expectedRecords, List<TestDataRecord> actualRecords, string message = null)
        {
            message = message ?? "Database records should match seeded test data";
            
            Assert.AreEqual(expectedRecords.Count, actualRecords.Count, $"{message}: Record count mismatch");
            
            for (int i = 0; i < expectedRecords.Count; i++)
            {
                var expected = expectedRecords[i];
                var actual = actualRecords.FirstOrDefault(r => r.Id == expected.Id);
                
                Assert.IsNotNull(actual, $"{message}: Record with ID {expected.Id} not found in database");
                Assert.AreEqual(expected, actual, $"{message}: Record with ID {expected.Id} does not match expected values");
            }
        }

        /// <summary>
        /// Validates that a single record matches the expected values
        /// </summary>
        /// <param name="expected">Expected record</param>
        /// <param name="actual">Actual record from database</param>
        /// <param name="message">Optional custom assertion message</param>
        protected void ValidateRecordMatches(TestDataRecord expected, TestDataRecord actual, string message = null)
        {
            message = message ?? $"Record with ID {expected?.Id} should match expected values";
            
            Assert.IsNotNull(actual, $"{message}: Record not found in database");
            Assert.AreEqual(expected, actual, message);
        }
    }
} 