namespace SnowflakeTestApp.Tests
{
    /// <summary>
    /// Common test data and constants used across integration tests
    /// </summary>
    public static class TestData
    {
        /// <summary>
        /// Default test dataset name
        /// </summary>
        public const string DefaultDataset = "default";

        /// <summary>
        /// Default test table name - adjust for your test environment
        /// </summary>
        public const string DefaultTable = "CUSTOMERS";

        /// <summary>
        /// Default Snowflake instance for testing - adjust for your environment
        /// </summary>
        public const string DefaultSnowflakeInstance = "your-snowflake-instance.snowflakecomputing.com";

        /// <summary>
        /// Sample SQL statement for testing
        /// </summary>
        public const string SampleSqlStatement = "SELECT 1 as test_column;";

        /// <summary>
        /// Mock statement handle for testing SQL operations
        /// </summary>
        public const string MockStatementHandle = "01b1ebc6-0000-7065-0000-438300e4e0c6";

        /// <summary>
        /// Test timeout for SQL operations (seconds)
        /// </summary>
        public const int DefaultSqlTimeout = 60;
    }
} 