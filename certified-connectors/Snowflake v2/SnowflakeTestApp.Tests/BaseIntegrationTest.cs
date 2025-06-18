using System;
using System.IO;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace SnowflakeTestApp.Tests
{
    /// <summary>
    /// Base class for integration tests that provides common functionality
    /// </summary>
    public abstract class BaseIntegrationTest
    {
        protected string BaseUrl => TestData.BaseUrl;
        protected HttpClient HttpClient;

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