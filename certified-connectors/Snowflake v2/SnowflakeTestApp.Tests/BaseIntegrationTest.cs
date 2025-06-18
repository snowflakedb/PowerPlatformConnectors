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
            return TestConfiguration.BearerToken;
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
            Assert.IsFalse(string.IsNullOrEmpty(content), "Response content should not be empty");
        }

        /// <summary>
        /// Helper method to validate JSON response content
        /// </summary>
        protected void AssertValidJsonResponse(HttpResponseMessage response)
        {
            var content = response.Content.ReadAsStringAsync().Result;
            Assert.IsFalse(string.IsNullOrEmpty(content), "Response content should not be empty");
            
            try
            {
                JsonConvert.DeserializeObject(content);
            }
            catch (JsonException ex)
            {
                Assert.Fail($"Response content is not valid JSON: {ex.Message}\nContent: {content}");
            }
        }
    }
} 