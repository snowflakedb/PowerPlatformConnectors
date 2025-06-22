using System;
using System.IO;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SnowflakeTestApp.Tests.Infrastructure;
using System.Threading.Tasks;

namespace SnowflakeTestApp.Tests
{
    /// <summary>
    /// Base class for integration tests that provides common functionality
    /// Data is seeded once before all tests run
    /// </summary>
    public abstract class BaseIntegrationTest
    {
        protected string BaseUrl => TestData.BaseUrl;
        protected HttpClient HttpClient;
        private static TestDataSeeder DataSeeder;

        /// <summary>
        /// Initializes test data once before all tests in the assembly run
        /// </summary>
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            try
            {
                // Initialize HTTP client for data seeding
                using (var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(TestData.DefaultTimeoutSeconds)
                })
                {
                    // Get test token for data seeding
                    var token = TestData.DefaultBearerToken;
                    
                    // Check if the token is configured
                    if (!string.IsNullOrEmpty(token) && 
                        !token.Equals("your-token-here", StringComparison.OrdinalIgnoreCase) &&
                        !token.Equals("your-actual-bearer-token-here", StringComparison.OrdinalIgnoreCase))
                    {
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
                    using (var httpClient = new HttpClient
                    {
                        Timeout = TimeSpan.FromSeconds(TestData.DefaultTimeoutSeconds)
                    })
                    {
                        var cleanupSeeder = new TestDataSeeder(httpClient, TestData.BaseUrl, TestData.DefaultBearerToken);
                        cleanupSeeder.CleanupTestTable(TestData.DefaultTable).GetAwaiter().GetResult();
                    }
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
        /// Helper method to assert that a response has the expected status code
        /// </summary>
        protected void AssertStatusCode(HttpResponseMessage response, System.Net.HttpStatusCode expectedStatusCode)
        {
            if (response.StatusCode != expectedStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                Assert.Fail($"Expected status code {expectedStatusCode} but got {response.StatusCode}. Response: {content}");
            }
        }

        /// <summary>
        /// Helper method to assert that a response has content
        /// </summary>
        protected void AssertResponseHasContent(HttpResponseMessage response)
        {
            var content = response.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(content), "Response content should not be empty");
        }

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
    }
} 