using System;
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
                await CreateTableIfNotExists(tableName, dataset);
                
                // Then seed it with sample data
                await SeedTableWithSampleData(tableName, dataset);
                
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not setup test table '{tableName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a test table if it doesn't exist using SQL execution
        /// </summary>
        private async Task CreateTableIfNotExists(string tableName, string dataset)
        {
            var createTableSql = $@"
                CREATE TABLE IF NOT EXISTS {tableName} (
                    ID NUMBER AUTOINCREMENT PRIMARY KEY,
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
        /// Seeds the test table with sample data
        /// </summary>
        private async Task SeedTableWithSampleData(string tableName, string dataset)
        {
            // Check if table already has data
            var countSql = $"SELECT COUNT(*) as record_count FROM {tableName}";
            var countResult = await ExecuteSqlStatement(countSql);
            
            // Only seed if table is empty (this is a simple check)
            // In a real scenario, you might want more sophisticated logic
            
            var seedDataSql = $@"
                INSERT INTO {tableName} (NAME, EMAIL, PHONE, IS_ACTIVE, BALANCE) VALUES
                ('John Doe', 'john.doe@example.com', '+1-555-0101', TRUE, 1500.50),
                ('Jane Smith', 'jane.smith@example.com', '+1-555-0102', TRUE, 2750.00),
                ('Bob Johnson', 'bob.johnson@example.com', '+1-555-0103', TRUE, 890.25),
                ('Alice Brown', 'alice.brown@example.com', '+1-555-0104', FALSE, 0.00),
                ('Charlie Wilson', 'charlie.wilson@example.com', '+1-555-0105', TRUE, 3200.75),
                ('Diana Davis', 'diana.davis@example.com', '+1-555-0106', TRUE, 1100.00),
                ('Eve Miller', 'eve.miller@example.com', '+1-555-0107', TRUE, 4500.25),
                ('Frank Garcia', 'frank.garcia@example.com', '+1-555-0108', FALSE, 250.00),
                ('Grace Lee', 'grace.lee@example.com', '+1-555-0109', TRUE, 1875.50),
                ('Henry Taylor', 'henry.taylor@example.com', '+1-555-0110', TRUE, 3350.00)";

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
        /// Drops the test table completely
        /// </summary>
        /// <param name="tableName">Name of the table to drop (defaults to TestData.DefaultTable)</param>
        public async Task<bool> DropTestTable(string tableName = null)
        {
            // Handle default value inside the method
            tableName = tableName ?? TestData.DefaultTable;

            try
            {
                var dropSql = $"DROP TABLE IF EXISTS {tableName}";
                await ExecuteSqlStatement(dropSql);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not drop test table '{tableName}': {ex.Message}", ex);
            }
        }
    }
} 