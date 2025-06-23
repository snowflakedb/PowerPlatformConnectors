using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SnowflakeTestApp.Tests.Infrastructure
{
    /// <summary>
    /// Integration tests specifically for validating TestDataRecord mapping and database validation functionality
    /// These tests demonstrate how to use the new data model features for robust test assertions
    /// </summary>
    [TestClass]
    public class TestDataValidationIntegrationTest : BaseIntegrationTest
    {
        /// <summary>
        /// Seeds fresh test data before any tests in this class run
        /// This ensures clean and predictable data for all test methods
        /// </summary>
        [ClassInitialize]
        public static async Task ClassInitialize(TestContext context)
        {
            // Ensure we have a valid data seeder from the base class
            if (DataSeeder != null)
            {
                // Seed fresh test data specifically for this test class
                await DataSeeder.EnsureTestTableExistsAndSeed(TestData.DefaultTable, TestData.DefaultDataset);
            }
        }

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            EnsureApplicationIsRunning();
        }

        /// <summary>
        /// Test that demonstrates basic data fetching and mapping functionality
        /// </summary>
        [TestMethod]
        public async Task FetchTestDataFromDatabase_ShouldReturnMappedRecords()
        {
            // Fetch data from the database
            var actualRecords = await FetchActualDataFromDatabase();
            
            // Basic validations
            Assert.IsNotNull(actualRecords, "Fetched records should not be null");
            Assert.IsTrue(actualRecords.Count > 0, "Should have fetched some records");
            
            // Validate structure of first record
            var firstRecord = actualRecords.First();
            Assert.IsTrue(firstRecord.Id > 0, "ID should be positive");
            Assert.IsFalse(string.IsNullOrEmpty(firstRecord.Name), "Name should not be empty");
            Assert.IsFalse(string.IsNullOrEmpty(firstRecord.Email), "Email should not be empty");
            Assert.IsTrue(firstRecord.Balance >= 0, "Balance should be non-negative");
        }

        /// <summary>
        /// Test that demonstrates fetching a specific record by ID
        /// </summary>
        [TestMethod]
        public async Task FetchTestRecordById_ShouldReturnCorrectRecord()
        {
            // Fetch a specific record
            var actualRecord = await FetchActualRecordById(1);
            
            Assert.IsNotNull(actualRecord, "Record with ID 1 should exist");
            Assert.AreEqual(1, actualRecord.Id, "Retrieved record should have correct ID");
            
            // Get the expected record from seeded data
            var expectedRecord = SeededTestData.FirstOrDefault(r => r.Id == 1);
            ValidateRecordMatches(expectedRecord, actualRecord, "Fetched record should match seeded data");
        }

        /// <summary>
        /// Test that demonstrates full dataset validation
        /// </summary>
        [TestMethod]
        public async Task ValidateCompleteDataset_ShouldMatchSeededData()
        {
            // Fetch all records from database
            var actualRecords = await FetchActualDataFromDatabase();
            
            // Validate against seeded data
            ValidateDataMatches(SeededTestData, actualRecords, "Complete dataset validation");
            
            // Additional validations
            Assert.AreEqual(SeededTestData.Count, actualRecords.Count, "Record counts should match");
            
            // Validate specific known records
            var johnDoe = actualRecords.FirstOrDefault(r => r.Name == "Frank Garcia");
            Assert.IsNotNull(johnDoe, "Frank Garcia should exist in database");
            Assert.AreEqual(250.00m, johnDoe.Balance, "Frank Garcia should have correct balance");
            
            var aliceBrown = actualRecords.FirstOrDefault(r => r.Name == "Alice Brown");
            Assert.IsNotNull(aliceBrown, "Alice Brown should exist in database");
            Assert.IsFalse(aliceBrown.IsActive, "Alice Brown should be inactive");
        }

        /// <summary>
        /// Test that demonstrates validating active vs inactive records
        /// </summary>
        [TestMethod]
        public async Task ValidateActiveInactiveRecords_ShouldMatchExpectedCounts()
        {
            var actualRecords = await FetchActualDataFromDatabase();
            
            var actualActiveRecords = actualRecords.Where(r => r.IsActive).ToList();
            var actualInactiveRecords = actualRecords.Where(r => !r.IsActive).ToList();
            
            var expectedActiveRecords = SampleTestData.GetActiveTestRecords();
            var expectedInactiveRecords = SampleTestData.GetInactiveTestRecords();
            
            Assert.AreEqual(expectedActiveRecords.Count, actualActiveRecords.Count, "Active record count should match");
            Assert.AreEqual(expectedInactiveRecords.Count, actualInactiveRecords.Count, "Inactive record count should match");
            
            // Validate each active record
            foreach (var expectedActive in expectedActiveRecords)
            {
                var actualActive = actualActiveRecords.FirstOrDefault(r => r.Id == expectedActive.Id);
                ValidateRecordMatches(expectedActive, actualActive, $"Active record with ID {expectedActive.Id}");
            }
        }

        /// <summary>
        /// Test that demonstrates using the helper methods from SampleTestData
        /// </summary>
        [TestMethod]
        public async Task ValidateHelperMethods_ShouldProvideCorrectData()
        {
            var actualRecords = await FetchActualDataFromDatabase();
            
            // Test GetTestRecordById helper
            var expectedRecord = SampleTestData.GetTestRecordById(2);
            var actualRecord = actualRecords.FirstOrDefault(r => r.Id == 2);
            ValidateRecordMatches(expectedRecord, actualRecord, "Helper method should return correct record");
            
            // Test GetTestRecordsWithBalanceGreaterThan helper
            var expectedHighBalanceRecords = SampleTestData.GetTestRecordsWithBalanceGreaterThan(2000m);
            var actualHighBalanceRecords = actualRecords.Where(r => r.Balance > 2000m).ToList();
            
            Assert.AreEqual(expectedHighBalanceRecords.Count, actualHighBalanceRecords.Count, 
                "High balance record counts should match");
            
            foreach (var expected in expectedHighBalanceRecords)
            {
                var actual = actualHighBalanceRecords.FirstOrDefault(r => r.Id == expected.Id);
                ValidateRecordMatches(expected, actual, $"High balance record with ID {expected.Id}");
            }
        }

        /// <summary>
        /// Test that demonstrates error handling when record is not found
        /// </summary>
        [TestMethod]
        public async Task FetchNonExistentRecord_ShouldReturnNull()
        {
            // Try to fetch a record that doesn't exist
            var nonExistentRecord = await FetchActualRecordById(99999);
            
            Assert.IsNull(nonExistentRecord, "Non-existent record should return null");
        }

        /// <summary>
        /// Test that demonstrates the TestDataRecord equality functionality
        /// </summary>
        [TestMethod]
        public void TestDataRecordEquality_ShouldWorkCorrectly()
        {
            var record1 = new TestDataRecord(1, "John Doe", "john@example.com", "+1-555-0101", true, 1500.50m);
            var record2 = new TestDataRecord(1, "John Doe", "john@example.com", "+1-555-0101", true, 1500.50m);
            var record3 = new TestDataRecord(2, "Jane Smith", "jane@example.com", "+1-555-0102", true, 2750.00m);
            
            // Test equality
            Assert.AreEqual(record1, record2, "Identical records should be equal");
            Assert.AreNotEqual(record1, record3, "Different records should not be equal");
            
            // Test hash codes
            Assert.AreEqual(record1.GetHashCode(), record2.GetHashCode(), "Equal records should have same hash code");
            
            // Test ToString
            var stringRepresentation = record1.ToString();
            Assert.IsTrue(stringRepresentation.Contains("John Doe"), "ToString should contain record data");
            Assert.IsTrue(stringRepresentation.Contains("1500.50"), "ToString should contain balance");
        }
    }
} 