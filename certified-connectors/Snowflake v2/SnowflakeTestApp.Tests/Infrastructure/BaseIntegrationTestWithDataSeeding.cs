using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SnowflakeTestApp.Tests.Infrastructure
{
    /// <summary>
    /// Base class for integration tests that require test data seeding
    /// Automatically creates and seeds test tables before running tests
    /// </summary>
    public abstract class BaseIntegrationTestWithDataSeeding : BaseIntegrationTest
    {
        protected TestDataSeeder DataSeeder;
        protected bool IsDataSeeded = false;

        /// <summary>
        /// Override this property to specify custom table names for seeding
        /// Default is TestData.DefaultTable ("CUSTOMERS")
        /// </summary>
        protected virtual string[] TestTables => new[] { TestData.DefaultTable };

        /// <summary>
        /// Override this property to specify custom dataset name
        /// Default is TestData.DefaultDataset ("default")
        /// </summary>
        protected virtual string TestDataset => TestData.DefaultDataset;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            
            // Initialize the data seeder
            try
            {
                var testToken = GetTestToken();
                DataSeeder = new TestDataSeeder(HttpClient, BaseUrl, testToken);
                
                // Seed test data for all specified tables
                SeedTestDataAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // If seeding fails, mark as inconclusive but provide helpful message
                Assert.Inconclusive($"Test data seeding failed. This might be due to invalid bearer token, missing Snowflake configuration, or permission issues. " +
                                   $"Error: {ex.Message}. " +
                                   "Some tests may still pass if they don't require seeded data.");
            }
        }

        /// <summary>
        /// Seeds test data for all configured test tables
        /// </summary>
        private async Task SeedTestDataAsync()
        {
            foreach (var tableName in TestTables)
            {
                try
                {
                    TestContext?.WriteLine($"Setting up test table: {tableName}");
                    var success = await DataSeeder.EnsureTestTableExistsAndSeed(tableName, TestDataset);
                    
                    if (success)
                    {
                        TestContext?.WriteLine($"✓ Test table '{tableName}' created and seeded successfully");
                        IsDataSeeded = true;
                    }
                    else
                    {
                        TestContext?.WriteLine($"⚠ Warning: Could not setup test table '{tableName}' - some tests may fail");
                    }
                }
                catch (Exception ex)
                {
                    TestContext?.WriteLine($"⚠ Warning: Failed to setup test table '{tableName}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Helper method to check if test data was successfully seeded
        /// Tests can use this to skip or modify behavior if seeding failed
        /// </summary>
        protected bool IsTestDataAvailable()
        {
            return IsDataSeeded;
        }

        /// <summary>
        /// Helper method to assert that test data is available, skipping the test if not
        /// </summary>
        protected void RequireTestData()
        {
            if (!IsDataSeeded)
            {
                Assert.Inconclusive("Test data is not available. This test requires seeded test tables to run properly. " +
                                   "Please check your bearer token configuration and Snowflake connection.");
            }
        }

        /// <summary>
        /// Method to clean up test data after each test
        /// </summary>
        [TestCleanup]
        public override void TestCleanup()
        {
            if (DataSeeder != null && IsDataSeeded)
            {
                foreach (var tableName in TestTables)
                {
                    try
                    {
                        DataSeeder.CleanupTestTable(tableName).GetAwaiter().GetResult();
                        TestContext?.WriteLine($"✓ Cleaned up test table: {tableName}");
                    }
                    catch (Exception ex)
                    {
                        TestContext?.WriteLine($"⚠ Warning: Could not cleanup test table '{tableName}': {ex.Message}");
                    }
                }
            }
            
            base.TestCleanup();
        }

        /// <summary>
        /// Manually trigger cleanup of test tables
        /// Useful for tests that want to clean up explicitly
        /// </summary>
        protected async Task<bool> CleanupTestDataAsync()
        {
            if (DataSeeder == null) return false;

            bool allSuccess = true;
            foreach (var tableName in TestTables)
            {
                var success = await DataSeeder.CleanupTestTable(tableName);
                if (!success) allSuccess = false;
            }
            return allSuccess;
        }

        /// <summary>
        /// Manually trigger dropping of test tables
        /// Use with caution - this completely removes the tables
        /// </summary>
        protected async Task<bool> DropTestTablesAsync()
        {
            if (DataSeeder == null) return false;

            bool allSuccess = true;
            foreach (var tableName in TestTables)
            {
                var success = await DataSeeder.DropTestTable(tableName);
                if (!success) allSuccess = false;
            }
            return allSuccess;
        }
    }
} 