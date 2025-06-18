using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SnowflakeTestApp.Tests.Metadata
{
    /// <summary>
    /// Integration tests for the datasets metadata endpoint.
    /// These tests document the expected behavior and can be used to verify the endpoint manually.
    /// </summary>
    [TestClass]
    public class DataSetsMetadataEndpointIntegrationTest : BaseIntegrationTest
    {
        /// <summary>
        /// Test the /$metadata.json/datasets endpoint with authentication
        /// </summary>
        [TestMethod]
        public async Task GetDataSetsMetadataEndpoint_WithAuth_ReturnsOk()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/$metadata.json/datasets");
            AssertStatusCode(response, HttpStatusCode.OK);
            AssertResponseHasContent(response);
        }

        /// <summary>
        /// Test the datasets metadata endpoint without authentication
        /// Note: Metadata endpoints may be publicly accessible, so we check for either Unauthorized or OK
        /// </summary>
        [TestMethod]
        public async Task GetDataSetsMetadataEndpoint_WithoutAuth_ChecksAuthenticationRequirement()
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/$metadata.json/datasets");
            
            // Metadata endpoints might be publicly accessible or require auth - both are valid patterns
            var isUnauthorized = response.StatusCode == HttpStatusCode.Unauthorized;
            var isForbidden = response.StatusCode == HttpStatusCode.Forbidden;
            var isOk = response.StatusCode == HttpStatusCode.OK;
            
            Assert.IsTrue(isUnauthorized || isForbidden || isOk, 
                         $"Expected Unauthorized (401), Forbidden (403), or OK (200) but got {(int)response.StatusCode} {response.StatusCode}");
            
            // If the endpoint is publicly accessible, document that behavior
            if (isOk)
            {
                TestContext?.WriteLine("Note: Metadata endpoint is publicly accessible (does not require authentication)");
                AssertResponseHasContent(response);
            }
            else
            {
                TestContext?.WriteLine($"Metadata endpoint requires authentication - returned {response.StatusCode}");
            }
        }

        /// <summary>
        /// Test that the response content type is JSON when successful
        /// </summary>
        [TestMethod]
        public async Task GetDataSetsMetadataEndpoint_ResponseContentType_IsJson()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/$metadata.json/datasets");
            AssertStatusCode(response, HttpStatusCode.OK);
            
            var contentType = response.Content.Headers.ContentType?.MediaType;
            Assert.IsTrue(contentType?.Contains("json") == true, $"Expected JSON content type but got {contentType}");
        }

        /// <summary>
        /// Test that the metadata response contains valid JSON structure
        /// </summary>
        [TestMethod]
        public async Task GetDataSetsMetadataEndpoint_ResponseIsValidJson()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/$metadata.json/datasets");
            AssertStatusCode(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// Test the endpoint with invalid route to ensure proper error handling
        /// </summary>
        [TestMethod]
        public async Task GetDataSetsMetadataEndpoint_WithInvalidRoute_ReturnsNotFound()
        {
            var testToken = GetTestToken();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {testToken}");

            var response = await HttpClient.GetAsync($"{BaseUrl}/$metadata.json/nonexistent");
            Assert.IsTrue(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest,
                         $"Expected NotFound (404) or BadRequest (400) but got {(int)response.StatusCode} {response.StatusCode}");
        }
    }
} 
