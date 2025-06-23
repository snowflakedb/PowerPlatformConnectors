using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SnowflakeTestApp.Tests.Infrastructure
{
    /// <summary>
    /// Handles creation and seeding of test tables with sample data
    /// </summary>
    public class TestDataSeeder
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _bearerToken;

        /// <summary>
        /// Gets the test records that were seeded into the database
        /// This allows tests to validate against the same data structure
        /// </summary>
        public List<TestDataRecord> SeededRecords { get; private set; } = new List<TestDataRecord>();

        public TestDataSeeder(HttpClient httpClient, string baseUrl, string bearerToken)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _bearerToken = bearerToken ?? throw new ArgumentNullException(nameof(bearerToken));
        }

        /// <summary>
        /// Creates test table if it doesn't exist and seeds it with sample data
        /// </summary>
        /// <param name="tableName">Name of the table to create/seed (defaults to TestData.DefaultTable)</param>
        /// <param name="dataset">Dataset name (defaults to TestData.DefaultDataset)</param>
        public async Task<bool> EnsureTestTableExistsAndSeed(string tableName = null, string dataset = null)
        {
            // Handle default values inside the method
            tableName = tableName ?? TestData.DefaultTable;
            dataset = dataset ?? TestData.DefaultDataset;

            try
            {
                // First, try to create the table if it doesn't exist
                await CreateTableIfNotExists(tableName);

                // Clear the table
                await CleanupTestTable(tableName);

                // Get the test records to seed
                SeededRecords = SampleTestData.GetDefaultTestRecords();

                // Then seed it with sample data
                await SeedTableWithSampleData(tableName, SeededRecords);
                
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not setup test table '{tableName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Seeds the test table with the provided test records
        /// </summary>
        /// <param name="tableName">Name of the table to seed</param>
        /// <param name="records">Test records to insert</param>
        public async Task SeedCustomTestData(string tableName, List<TestDataRecord> records)
        {
            try
            {
                await CreateTableIfNotExists(tableName);
                await CleanupTestTable(tableName);
                SeededRecords = new List<TestDataRecord>(records); // Create a copy
                await SeedTableWithSampleData(tableName, records);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not seed custom test data into table '{tableName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a test table if it doesn't exist using SQL execution
        /// </summary>
        private async Task CreateTableIfNotExists(string tableName)
        {
            var createTableSql = $@"
                CREATE OR ALTER TABLE {tableName} (
                    ID NUMBER PRIMARY KEY,
                    NAME VARCHAR(255) NOT NULL,
                    EMAIL VARCHAR(255),
                    PHONE VARCHAR(50),
                    CREATED_DATE TIMESTAMP DEFAULT CURRENT_TIMESTAMP(),
                    IS_ACTIVE BOOLEAN DEFAULT TRUE,
                    BALANCE NUMBER(10,2) DEFAULT 0.00
                )";

            await ExecuteSqlStatement(createTableSql);
        }

        /// <summary>
        /// Seeds the test table with sample data from TestDataRecord objects
        /// </summary>
        private async Task SeedTableWithSampleData(string tableName, List<TestDataRecord> records)
        {
            if (records == null || records.Count == 0)
            {
                throw new ArgumentException("No records provided for seeding", nameof(records));
            }

            var values = records.Select(r => 
                $"({r.Id}, '{r.Name.Replace("'", "''")}', '{r.Email}', '{r.Phone}', {(r.IsActive ? "TRUE" : "FALSE")}, {r.Balance})"
            );

            var seedDataSql = $@"
                INSERT INTO {tableName} (ID, NAME, EMAIL, PHONE, IS_ACTIVE, BALANCE) VALUES
                {string.Join(",\n                ", values)}";

            await ExecuteSqlStatement(seedDataSql);
        }

        /// <summary>
        /// Executes a SQL statement using the SQL endpoint
        /// </summary>
        private async Task<string> ExecuteSqlStatement(string sqlStatement)
        {
            try
            {
                // Create a new HttpRequestMessage to avoid header conflicts
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/sql");
                
                // Add headers to the request message instead of the client
                request.Headers.Add("Authorization", $"Bearer {_bearerToken}");
                request.Headers.Add("Instance", TestData.DefaultSnowflakeInstance);
                request.Headers.Add("Accept", "application/json");

                // Prepare SQL payload with full context like successful test
                var sqlPayload = new
                {
                    statement = sqlStatement,
                    timeout = TestData.DefaultSqlTimeout,
                    database = TestData.DefaultDatabase,
                    schema = TestData.DefaultSchema,
                    warehouse = TestData.DefaultWarehouse,
                    role = TestData.DefaultRole
                };

                var json = JsonConvert.SerializeObject(sqlPayload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                // Execute SQL using the request message
                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"SQL execution failed. Status: {response.StatusCode}, Response: {responseContent}");
                }

                return responseContent;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execute SQL statement: {sqlStatement}. Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cleans up test data by truncating the test table
        /// </summary>
        /// <param name="tableName">Name of the table to cleanup (defaults to TestData.DefaultTable)</param>
        public async Task<bool> CleanupTestTable(string tableName = null)
        {
            // Handle default value inside the method
            tableName = tableName ?? TestData.DefaultTable;

            try
            {
                var truncateSql = $"TRUNCATE TABLE IF EXISTS {tableName}";
                await ExecuteSqlStatement(truncateSql);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not cleanup test table '{tableName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Fetches data from the test table and maps it to TestDataRecord objects
        /// This allows for direct comparison between seeded data and actual database content
        /// </summary>
        /// <param name="tableName">Name of the table to fetch data from</param>
        /// <returns>List of TestDataRecord objects representing the actual data in the database</returns>
        public async Task<List<TestDataRecord>> FetchTestDataFromDatabase(string tableName = null)
        {
            tableName = tableName ?? TestData.DefaultTable;

            try
            {
                var sqlStatement = $"SELECT ID, NAME, EMAIL, PHONE, IS_ACTIVE, BALANCE FROM {tableName} ORDER BY ID";
                var responseContent = await ExecuteSqlStatement(sqlStatement);
                
                return MapSnowflakeResponseToTestDataRecords(responseContent);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not fetch test data from table '{tableName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Fetches a specific test record by ID from the database
        /// </summary>
        /// <param name="id">ID of the record to fetch</param>
        /// <param name="tableName">Name of the table to fetch from</param>
        /// <returns>TestDataRecord if found, null otherwise</returns>
        public async Task<TestDataRecord> FetchTestRecordById(int id, string tableName = null)
        {
            tableName = tableName ?? TestData.DefaultTable;

            try
            {
                var sqlStatement = $"SELECT ID, NAME, EMAIL, PHONE, IS_ACTIVE, BALANCE FROM {tableName} WHERE ID = {id}";
                var responseContent = await ExecuteSqlStatement(sqlStatement);
                
                var records = MapSnowflakeResponseToTestDataRecords(responseContent);
                return records.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not fetch test record with ID {id} from table '{tableName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Maps the Snowflake SQL response to TestDataRecord objects
        /// This handles the JSON response structure from Snowflake
        /// </summary>
        /// <param name="snowflakeResponse">Raw JSON response from Snowflake SQL endpoint</param>
        /// <returns>List of TestDataRecord objects</returns>
        private List<TestDataRecord> MapSnowflakeResponseToTestDataRecords(string snowflakeResponse)
        {
            var records = new List<TestDataRecord>();

            try
            {
                var response = JsonConvert.DeserializeObject<SnowflakeResponse>(snowflakeResponse);
                
                // Handle the data array from Snowflake response
                if (response?.Data != null)
                {
                    foreach (var row in response.Data)
                    {
                        if (row != null && row.Length >= 6)
                        {
                            var record = new TestDataRecord
                            {
                                Id = Convert.ToInt32(row[0]),
                                Name = row[1]?.ToString() ?? string.Empty,
                                Email = row[2]?.ToString() ?? string.Empty,
                                Phone = row[3]?.ToString() ?? string.Empty,
                                IsActive = Convert.ToBoolean(row[4]),
                                Balance = Convert.ToDecimal(row[5])
                            };
                            records.Add(record);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to map Snowflake response to TestDataRecord objects. Response: {snowflakeResponse}. Error: {ex.Message}", ex);
            }

            return records;
        }

        /// <summary>
        /// Simple class to represent the Snowflake SQL response structure
        /// </summary>
        private class SnowflakeResponse
        {
            [JsonProperty("data")]
            public object[][] Data { get; set; }
        }
    }
} 