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
        protected string BaseUrl => TestConfiguration.BaseUrl;
        protected HttpClient HttpClient;

        [TestInitialize]
        public virtual void TestInitialize()
        {
            HttpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(TestConfiguration.DefaultTimeoutSeconds)
            };
        }

        [TestCleanup]
        public virtual void TestCleanup()
        {
            HttpClient?.Dispose();
        }

        /// <summary>
        /// Gets the test token from TestConfiguration
        /// </summary>
        protected string GetTestToken()
        {
            var token = TestConfiguration.BearerToken;
            
            // Check if the token is still the placeholder value
            if (string.IsNullOrEmpty(token) || 
                token.Equals("your-token-here", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("your-actual-bearer-token-here", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Inconclusive("Bearer token not configured. Please update TestConfiguration.BearerToken with a valid OAuth bearer token. " +
                                   "See README.md for instructions on generating OAuth tokens.");
            }
            
            return token;
        }

        /// <summary>
        /// Gets the TestContext for accessing test run information
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Helper method to validate that a response has the expected status code
        /// </summary>
        protected void AssertStatusCode(HttpResponseMessage response, System.Net.HttpStatusCode expectedStatusCode, string additionalMessage = null)
        {
            var message = $"Expected HTTP {(int)expectedStatusCode} {expectedStatusCode} but got {(int)response.StatusCode} {response.StatusCode}";
            if (!string.IsNullOrEmpty(additionalMessage))
            {
                message += $". {additionalMessage}";
            }
            
            Assert.AreEqual(expectedStatusCode, response.StatusCode, message);
        }

        /// <summary>
        /// Helper method to check if response content is not empty
        /// </summary>
        protected void AssertResponseHasContent(HttpResponseMessage response)
        {
            var content = response.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(content), $"Response content should not be empty. Status: {response.StatusCode}. Content: '{content}'");
        }

        /// <summary>
        /// Helper method to validate JSON response content
        /// </summary>
        protected void AssertValidJsonResponse(HttpResponseMessage response)
        {
            var content = response.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(content), $"Response content should not be empty. Status: {response.StatusCode}");
            
            try
            {
                JsonConvert.DeserializeObject(content);
            }
            catch (JsonException ex)
            {
                Assert.Fail($"Response content is not valid JSON: {ex.Message}\nStatus: {response.StatusCode}\nContent: {content}");
            }
        }

        /// <summary>
        /// Helper method to check if the application is running at the expected URL
        /// </summary>
        protected void EnsureApplicationIsRunning()
        {
            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                {
                    var response = client.GetAsync(BaseUrl).Result;
                    // Any response (even error) means the app is running
                }
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"SnowflakeTestApp is not running at {BaseUrl}. " +
                                   "Please start the application before running integration tests. " +
                                   $"Error: {ex.Message}");
            }
        }
    }
} 