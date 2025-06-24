using SnowflakeTestApp.Mocks;

namespace SnowflakeTestApp.Tests
{
    /// <summary>
    /// Common test data and constants used across integration tests.
    /// Configuration values come from ConnectionParametersProviderMock.cs
    /// </summary>
    public static class TestData
    {
        public const string BaseUrl = "https://localhost:44362";
        public const int DefaultTimeoutSeconds = 30;
        public const string DefaultDataset = "default";
        public const string DefaultTable = "CUSTOMERS";
        public const string MockStatementHandle = "01b1ebc6-0000-7065-0000-438300e4e0c6";
        public const int DefaultSqlTimeout = 10;
        public const string SampleSqlStatement = "SELECT 1 as test_column, CURRENT_TIMESTAMP() as current_time, CURRENT_USER() as current_user";

        public static string DefaultSnowflakeInstance => ConnectionParametersProviderMock.TestSnowflakeInstance;
        public static string DefaultDatabase => ConnectionParametersProviderMock.TestDatabase;
        public static string DefaultSchema => ConnectionParametersProviderMock.TestSchema;
        public static string DefaultWarehouse => ConnectionParametersProviderMock.TestWarehouse;
        public static string DefaultRole => ConnectionParametersProviderMock.TestRole;
        public static string DefaultBearerToken => ConnectionParametersProviderMock.TestBearerToken;
    }
} 