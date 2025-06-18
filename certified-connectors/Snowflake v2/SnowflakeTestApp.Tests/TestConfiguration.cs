using System;

namespace SnowflakeTestApp.Tests
{
    /// <summary>
    /// Configuration settings for integration tests
    /// </summary>
    public static class TestConfiguration
    {
        /// <summary>
        /// Base URL for the test application
        /// </summary>
        public static string BaseUrl => "https://localhost:44362";

        /// <summary>
        /// Default timeout in seconds for HTTP requests during tests
        /// </summary>
        public static int DefaultTimeoutSeconds => 30;
    }
} 