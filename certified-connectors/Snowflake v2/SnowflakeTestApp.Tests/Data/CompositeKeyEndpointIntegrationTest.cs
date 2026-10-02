using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace SnowflakeTestApp.Tests.Data
{
    /// <summary>
    /// Integration tests for single item operations on tables with a composite primary key.
    /// Reading, updating and deleting individual items is rejected for such tables, because the item id
    /// only identifies a single key column and would otherwise match every row sharing that value.
    /// Every test creates its own tables, which are dropped afterwards.
    /// </summary>
    [TestClass]
    public class CompositeKeyEndpointIntegrationTest : BaseIntegrationTest
    {
        private const string TestDataset = "default";
        private const string OrderLinesTable = "CK_ORDER_LINES";
        private const string CompositeKeyError = "has a composite primary key";

        private readonly List<string> createdTables = new List<string>();

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            EnsureApplicationIsRunning();

            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {GetTestToken()}");
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            foreach (var table in createdTables)
            {
                try
                {
                    DataSeeder.ExecuteSqlStatement($"DROP TABLE IF EXISTS {table}").GetAwaiter().GetResult();
                }
                catch (Exception)
                {
                    // Ignore cleanup errors to prevent masking test failures
                }
            }

            createdTables.Clear();
            base.TestCleanup();
        }

        [DataRow("1")]
        [DataRow("1,2")]
        [DataTestMethod]
        public async Task GetItem_CompositePrimaryKey_ReturnsBadRequest(string id)
        {
            await CreateOrderLinesTable();

            var response = await HttpClient.GetAsync(ItemUrl(OrderLinesTable, id));

            await AssertCompositeKeyRejected(response);
        }

        [DataRow("1")]
        [DataRow("1,2")]
        [DataTestMethod]
        public async Task PatchItem_CompositePrimaryKey_ReturnsBadRequestAndChangesNothing(string id)
        {
            await CreateOrderLinesTable();

            var response = await Patch(ItemUrl(OrderLinesTable, id), new { NOTE = "updated" });

            await AssertCompositeKeyRejected(response);
            await AssertOrderLinesUnchanged();
        }

        [DataRow("1")]
        [DataRow("1,2")]
        [DataTestMethod]
        public async Task PutItem_CompositePrimaryKey_ReturnsBadRequestAndChangesNothing(string id)
        {
            await CreateOrderLinesTable();

            var response = await HttpClient.PutAsync(
                ItemUrl(OrderLinesTable, id),
                CreateJsonContent(new { ORDER_ID = 1, LINE_NO = 2, NOTE = "updated" }));

            await AssertCompositeKeyRejected(response);
            await AssertOrderLinesUnchanged();
        }

        [DataRow("1")]
        [DataRow("1,2")]
        [DataTestMethod]
        public async Task DeleteItem_CompositePrimaryKey_ReturnsBadRequestAndDeletesNothing(string id)
        {
            await CreateOrderLinesTable();

            var response = await HttpClient.DeleteAsync(ItemUrl(OrderLinesTable, id));

            await AssertCompositeKeyRejected(response);
            await AssertOrderLinesUnchanged();
        }

        [TestMethod]
        public async Task DeleteItem_ThreeColumnPrimaryKey_ReturnsBadRequestAndDeletesNothing()
        {
            await CreateTable("CK_THREE_KEYS",
                "TENANT VARCHAR NOT NULL, ORDER_ID NUMBER(38,0) NOT NULL, LINE_NO NUMBER(38,0) NOT NULL, NOTE VARCHAR, PRIMARY KEY (TENANT, ORDER_ID, LINE_NO)");
            await Sql("INSERT INTO CK_THREE_KEYS VALUES ('acme', 1, 1, 'a'), ('acme', 1, 2, 'b'), ('other', 1, 1, 'c')");

            var response = await HttpClient.DeleteAsync(ItemUrl("CK_THREE_KEYS", "acme"));

            await AssertCompositeKeyRejected(response);
            Assert.AreEqual(3, await Count("CK_THREE_KEYS"));
        }

        [TestMethod]
        public async Task DeleteItem_CompositeKeyDeclaredInDifferentOrderThanColumns_ReturnsBadRequestAndDeletesNothing()
        {
            await CreateTable("CK_REORDERED",
                "LINE_NO NUMBER(38,0) NOT NULL, NOTE VARCHAR, ORDER_ID NUMBER(38,0) NOT NULL, PRIMARY KEY (ORDER_ID, LINE_NO)");
            await Sql("INSERT INTO CK_REORDERED (LINE_NO, NOTE, ORDER_ID) VALUES (1, 'a', 1), (2, 'b', 1)");

            var response = await HttpClient.DeleteAsync(ItemUrl("CK_REORDERED", "1"));

            await AssertCompositeKeyRejected(response);
            Assert.AreEqual(2, await Count("CK_REORDERED"));
        }

        [TestMethod]
        public async Task CompositePrimaryKey_ListItemsAndMetadata_StillWork()
        {
            await CreateOrderLinesTable();

            var list = await HttpClient.GetAsync($"{BaseUrl}/datasets('{TestDataset}')/tables('{OrderLinesTable}')/items");
            Assert.AreEqual(HttpStatusCode.OK, list.StatusCode, await list.Content.ReadAsStringAsync());
            var rows = (JArray)JObject.Parse(await list.Content.ReadAsStringAsync())["value"];
            Assert.AreEqual(4, rows.Count);

            var metadata = await HttpClient.GetAsync($"{BaseUrl}/$metadata.json/datasets/{TestDataset}/tables/{OrderLinesTable}");
            Assert.AreEqual(HttpStatusCode.OK, metadata.StatusCode, await metadata.Content.ReadAsStringAsync());
        }

        [TestMethod]
        public async Task SingleColumnPrimaryKey_GetPatchDelete_StillWork()
        {
            await CreateTable("CK_SINGLE_KEY", "ID NUMBER(38,0) NOT NULL, NOTE VARCHAR, PRIMARY KEY (ID)");
            await Sql("INSERT INTO CK_SINGLE_KEY VALUES (1, 'a'), (2, 'b')");

            var get = await HttpClient.GetAsync(ItemUrl("CK_SINGLE_KEY", "1"));
            Assert.AreEqual(HttpStatusCode.OK, get.StatusCode, await get.Content.ReadAsStringAsync());
            Assert.AreEqual("a", (string)JObject.Parse(await get.Content.ReadAsStringAsync())["NOTE"]);

            var patch = await Patch(ItemUrl("CK_SINGLE_KEY", "1"), new { NOTE = "patched" });
            Assert.AreEqual(HttpStatusCode.OK, patch.StatusCode, await patch.Content.ReadAsStringAsync());
            Assert.AreEqual("patched", await Note("CK_SINGLE_KEY", "ID = 1"));
            Assert.AreEqual("b", await Note("CK_SINGLE_KEY", "ID = 2"));

            var delete = await HttpClient.DeleteAsync(ItemUrl("CK_SINGLE_KEY", "1"));
            Assert.AreEqual(HttpStatusCode.OK, delete.StatusCode, await delete.Content.ReadAsStringAsync());
            Assert.AreEqual(1, await Count("CK_SINGLE_KEY"));
            Assert.AreEqual(0, await Count("CK_SINGLE_KEY", "ID = 1"));
        }

        [TestMethod]
        public async Task SingleColumnPrimaryKey_ValueContainingComma_StillWorks()
        {
            await CreateTable("CK_COMMA_SINGLE", "CODE VARCHAR NOT NULL, NOTE VARCHAR, PRIMARY KEY (CODE)");
            await Sql("INSERT INTO CK_COMMA_SINGLE VALUES ('a,b', 'comma'), ('a', 'plain'), ('b', 'other')");

            var get = await HttpClient.GetAsync(ItemUrl("CK_COMMA_SINGLE", "a,b"));
            Assert.AreEqual(HttpStatusCode.OK, get.StatusCode, await get.Content.ReadAsStringAsync());
            Assert.AreEqual("comma", (string)JObject.Parse(await get.Content.ReadAsStringAsync())["NOTE"]);

            var delete = await HttpClient.DeleteAsync(ItemUrl("CK_COMMA_SINGLE", "a,b"));
            Assert.AreEqual(HttpStatusCode.OK, delete.StatusCode, await delete.Content.ReadAsStringAsync());
            Assert.AreEqual(2, await Count("CK_COMMA_SINGLE"));
            Assert.AreEqual(0, await Count("CK_COMMA_SINGLE", "CODE = 'a,b'"));
        }

        [TestMethod]
        public async Task TableWithoutPrimaryKey_Delete_StillFailsAndDeletesNothing()
        {
            await CreateTable("CK_NO_PK", "ID NUMBER(38,0), NOTE VARCHAR");
            await Sql("INSERT INTO CK_NO_PK VALUES (1, 'a'), (1, 'b'), (2, 'c')");

            var response = await HttpClient.DeleteAsync(ItemUrl("CK_NO_PK", "1"));

            Assert.IsFalse(response.IsSuccessStatusCode, "DELETE without a primary key must fail");
            Assert.AreEqual(3, await Count("CK_NO_PK"));
        }

        private async Task CreateOrderLinesTable()
        {
            await CreateTable(OrderLinesTable,
                "ORDER_ID NUMBER(38,0) NOT NULL, LINE_NO NUMBER(38,0) NOT NULL, NOTE VARCHAR, PRIMARY KEY (ORDER_ID, LINE_NO)");
            await Sql($"INSERT INTO {OrderLinesTable} VALUES (1, 1, 'a'), (1, 2, 'b'), (2, 1, 'c'), (2, 2, 'd')");
        }

        private async Task AssertOrderLinesUnchanged()
        {
            Assert.AreEqual(4, await Count(OrderLinesTable), "No row may be deleted");
            Assert.AreEqual("a", await Note(OrderLinesTable, "ORDER_ID = 1 AND LINE_NO = 1"), "Row (1,1)");
            Assert.AreEqual("b", await Note(OrderLinesTable, "ORDER_ID = 1 AND LINE_NO = 2"), "Row (1,2)");
            Assert.AreEqual("c", await Note(OrderLinesTable, "ORDER_ID = 2 AND LINE_NO = 1"), "Row (2,1)");
            Assert.AreEqual("d", await Note(OrderLinesTable, "ORDER_ID = 2 AND LINE_NO = 2"), "Row (2,2)");
        }

        private static async Task AssertCompositeKeyRejected(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, content);
            StringAssert.Contains(content, CompositeKeyError);
        }

        private async Task CreateTable(string name, string columns)
        {
            createdTables.Add(name);
            await Sql($"CREATE OR REPLACE TABLE {name} ({columns})");
        }

        private Task<string> Sql(string statement)
        {
            return DataSeeder.ExecuteSqlStatement(statement);
        }

        private async Task<long> Count(string table, string where = null)
        {
            var sql = $"SELECT COUNT(*) AS RESULT FROM {table}" + (where == null ? string.Empty : $" WHERE {where}");
            return Convert.ToInt64(await Scalar(sql));
        }

        private Task<string> Note(string table, string where)
        {
            return Scalar($"SELECT NOTE AS RESULT FROM {table} WHERE {where}");
        }

        private async Task<string> Scalar(string sql)
        {
            var response = JObject.Parse(await Sql(sql));
            var rows = response.GetValue("Data", StringComparison.OrdinalIgnoreCase) as JArray;
            Assert.IsNotNull(rows, $"Unexpected SQL response for: {sql}");
            Assert.IsTrue(rows.Count <= 1, $"Expected at most one row for: {sql}");

            if (rows.Count == 0)
            {
                return null;
            }

            var value = ((JObject)rows[0]).Properties().First().Value;
            return value.Type == JTokenType.Null ? null : value.ToString();
        }

        private string ItemUrl(string table, string id)
        {
            return $"{BaseUrl}/datasets('{TestDataset}')/tables('{table}')/items('{id}')";
        }

        private Task<HttpResponseMessage> Patch(string url, object body)
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
            {
                Content = CreateJsonContent(body),
            };

            return HttpClient.SendAsync(request);
        }
    }
}
