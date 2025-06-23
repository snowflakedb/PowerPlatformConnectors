using SnowflakeTestApp.Mocks;

namespace SnowflakeTestApp.Tests
{
    /// <summary>
    /// Common test data and constants used across integration tests
    /// Configuration values come from ConnectionParametersProviderMock.cs
    /// </summary>
    public static class TestData
    {
        // ====== TEST APPLICATION CONFIGURATION ======

        /// <summary>
        /// Base URL for the test application
        /// </summary>
        public const string BaseUrl = "https://localhost:44362";

        /// <summary>
        /// Default timeout in seconds for HTTP requests during tests
        /// </summary>
        public const int DefaultTimeoutSeconds = 30;

        // ====== TEST DATA CONFIGURATION ======

        /// <summary>
        /// Default test dataset name for connector operations
        /// </summary>
        public const string DefaultDataset = "default";

        /// <summary>
        /// Default test table name - will be created and seeded automatically
        /// </summary>
        public const string DefaultTable = "CUSTOMERS";

        /// <summary>
        /// Mock statement handle for testing SQL operations
        /// Note: This is a fake handle for testing error scenarios
        /// </summary>
        public const string MockStatementHandle = "01b1ebc6-0000-7065-0000-438300e4e0c6";

        /// <summary>
        /// Test timeout for SQL operations (seconds)
        /// </summary>
        public const int DefaultSqlTimeout = 10;

        /// <summary>
        /// Sample SQL statement for testing
        /// </summary>
        public const string SampleSqlStatement = "SELECT 1 as test_column, CURRENT_TIMESTAMP() as current_time, CURRENT_USER() as current_user";

        // ====== SNOWFLAKE CONNECTION VALUES (from ConnectionParametersProviderMock) ======

        /// <summary>
        /// Default Snowflake instance for testing - configured in ConnectionParametersProviderMock.cs
        /// </summary>
        public static string DefaultSnowflakeInstance => ConnectionParametersProviderMock.TestSnowflakeInstance;

        /// <summary>
        /// Database name from ConnectionParametersProviderMock
        /// </summary>
        public static string DefaultDatabase => ConnectionParametersProviderMock.TestDatabase;

        /// <summary>
        /// Schema name from ConnectionParametersProviderMock
        /// </summary>
        public static string DefaultSchema => ConnectionParametersProviderMock.TestSchema;

        /// <summary>
        /// Warehouse name from ConnectionParametersProviderMock
        /// </summary>
        public static string DefaultWarehouse => ConnectionParametersProviderMock.TestWarehouse;

        /// <summary>
        /// Role name from ConnectionParametersProviderMock
        /// </summary>
        public static string DefaultRole => ConnectionParametersProviderMock.TestRole;

        /// <summary>
        /// Bearer token from ConnectionParametersProviderMock
        /// </summary>
        public static string DefaultBearerToken => ConnectionParametersProviderMock.TestBearerToken;
    }
} 