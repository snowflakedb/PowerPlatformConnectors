using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace SnowflakeTestApp.Tests
{
    /// <summary>
    /// Integration tests for the /testconnection endpoint.
    /// These tests document the expected behavior and can be used to verify the endpoint manually.
    /// </summary>
    [TestClass]
    public class TestConnectionEndpointIntegrationTest : BaseIntegrationTest
    {
        /// <summary>
        /// This test shows how to manually test the endpoint with authentication if the application is running
        /// To run this test:
        /// 1. Start the SnowflakeTestApp application
        /// 2. Create a test-secrets.json file in the test directory with your bearer token
        /// 3. Run this test
        /// </summary>
        [TestMethod]
        public async Task TestConnectionEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/testconnection");
            AssertStatusCode(response, HttpStatusCode.OK);
            AssertResponseHasContent(response);
        }

        /// <summary>
        /// This test verifies that the testconnection endpoint requires authentication
        /// </summary>
        [TestMethod]
        public async Task TestConnectionEndpoint_WithoutAuth_ReturnsUnauthorized()
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/testconnection");
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden,
                         $"Expected Unauthorized (401) or Forbidden (403) but got {(int)response.StatusCode} {response.StatusCode}");
        }

        /// <summary>
        /// Test the endpoint with invalid authorization token
        /// </summary>
        [TestMethod]
        public async Task TestConnectionEndpoint_WithInvalidAuth_ReturnsUnauthorized()
        {
            HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer invalid-token");

            var response = await HttpClient.GetAsync($"{BaseUrl}/testconnection");
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden,
                         $"Expected Unauthorized (401) or Forbidden (403) but got {(int)response.StatusCode} {response.StatusCode}");
        }

        /// <summary>
        /// Test the endpoint with malformed authorization header
        /// </summary>
        [TestMethod]
        public async Task TestConnectionEndpoint_WithMalformedAuth_ReturnsUnauthorized()
        {
            HttpClient.DefaultRequestHeaders.Add("Authorization", "InvalidFormat token-here");

            var response = await HttpClient.GetAsync($"{BaseUrl}/testconnection");
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.BadRequest,
                         $"Expected Unauthorized (401) or BadRequest (400) but got {(int)response.StatusCode} {response.StatusCode}");
        }

        /// <summary>
        /// Test that successful connection returns valid JSON response
        /// </summary>
        [TestMethod]
        public async Task TestConnectionEndpoint_WithAuth_ReturnsValidJson()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/testconnection");
            AssertStatusCode(response, HttpStatusCode.OK);
            AssertValidJsonResponse(response);
        }

        /// <summary>
        /// Test HTTP method restrictions (should only accept GET)
        /// </summary>
        [TestMethod]
        public async Task TestConnectionEndpoint_WithPostMethod_ReturnsMethodNotAllowed()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.PostAsync($"{BaseUrl}/testconnection", new StringContent(""));
            Assert.IsTrue(response.StatusCode == HttpStatusCode.MethodNotAllowed || response.StatusCode == HttpStatusCode.NotFound,
                         $"Expected MethodNotAllowed (405) or NotFound (404) but got {(int)response.StatusCode} {response.StatusCode}");
        }
    }
} 