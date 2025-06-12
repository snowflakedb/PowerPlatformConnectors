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

        private class TestSecrets
        {
            public string BearerToken { get; set; }
        }

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
        /// Gets the test token from the secrets file, looking in the correct location
        /// </summary>
        protected string GetTestToken()
        {
            // Try multiple possible locations for the test secrets file
            var possiblePaths = new[]
            {
                Path.Combine(TestContext.TestDir, "..", "..", "..", "test-secrets.json"), // From bin/Debug/net48 back to project root
                Path.Combine(Directory.GetCurrentDirectory(), "test-secrets.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-secrets.json"),
                "test-secrets.json"
            };

            string secretsPath = null;
            foreach (var path in possiblePaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    secretsPath = fullPath;
                    break;
                }
            }

            if (secretsPath == null)
            {
                // Create a more helpful error message with all attempted paths
                var attemptedPaths = "";
                for (int i = 0; i < possiblePaths.Length; i++)
                {
                    attemptedPaths += $"\n  {i + 1}. {Path.GetFullPath(possiblePaths[i])}";
                }
                
                Assert.Inconclusive($"Test secrets file not found. Attempted paths:{attemptedPaths}\n\n" +
                                   "Create a 'test-secrets.json' file in the SnowflakeTestApp.Tests project directory with your bearer token:\n" +
                                   "{\n  \"BearerToken\": \"your-token-here\"\n}");
            }

            var secretsJson = File.ReadAllText(secretsPath);
            var secrets = JsonConvert.DeserializeObject<TestSecrets>(secretsJson);
            
            if (string.IsNullOrEmpty(secrets?.BearerToken))
            {
                Assert.Inconclusive("BearerToken not found in test-secrets.json file. Please ensure the file contains:\n" +
                                   "{\n  \"BearerToken\": \"your-token-here\"\n}");
            }

            return secrets.BearerToken;
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