// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace SnowflakeV2CoreLogic.Tests.Utilities
{
    using System.Collections.Generic;
    using Microsoft.OData.Core.UriParser;
    using Microsoft.OData.Core.UriParser.Semantic;
    using Microsoft.OData.Edm;
    using Microsoft.OData.Edm.Library;
    using SnowflakeV2CoreLogic.Utilities;

    /// <summary>
    /// Unit tests for <see cref="ODataToSqlParser"/>. Filter strings are parsed into a real
    /// <see cref="FilterClause"/> (via the OData parser against a minimal EDM model) and the
    /// generated SQL is asserted, with emphasis on the string-literal escaping that prevents
    /// injection through <c>$filter</c> values. The entity type is declared <em>open</em> so that
    /// both declared (typed) and open (dynamic) property access are exercised.
    /// </summary>
    [TestClass]
    public sealed class ODataToSqlParserTest
    {
        private static readonly EdmModel Model;
        private static readonly EdmEntityType ItemType;
        private static readonly EdmEntitySet ItemSet;

        static ODataToSqlParserTest()
        {
            Model = new EdmModel();

            // isOpen: true => undeclared properties (e.g. STATUS) parse as open-property access.
            ItemType = new EdmEntityType("Snowflake", "Item", baseType: null, isAbstract: false, isOpen: true);
            var id = ItemType.AddStructuralProperty("ID", EdmPrimitiveTypeKind.Int32);
            ItemType.AddKeys(id);
            ItemType.AddStructuralProperty("NAME", EdmPrimitiveTypeKind.String);
            ItemType.AddStructuralProperty("AGE", EdmPrimitiveTypeKind.Int32);
            Model.AddElement(ItemType);

            var container = new EdmEntityContainer("Snowflake", "Container");
            ItemSet = container.AddEntitySet("Items", ItemType);
            Model.AddElement(container);
        }

        private static string ToSql(string filter)
        {
            var parser = new ODataQueryOptionParser(
                Model,
                ItemType,
                ItemSet,
                new Dictionary<string, string> { { "$filter", filter } });

            FilterClause clause = parser.ParseFilter();
            return new ODataToSqlParser().ParseFilterToSql(clause);
        }

        private static bool CannotBeConvertedToSql(string filter)
        {
            try
            {
                ToSql(filter);
                return false;
            }
            catch
            {
                // Either the OData layer rejects/reinterprets it, or the parser refuses the resulting
                // (non property-access) expression. Both mean it never reaches SQL as an identifier.
                return true;
            }
        }

        [TestMethod]
        public void ParseFilterToSql_NullClause_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, new ODataToSqlParser().ParseFilterToSql(null));
        }

        // ---- string literal escaping (the core $filter injection defense) ----

        [TestMethod]
        public void ParseFilterToSql_StringEquality_EmitsQuotedLiteral()
        {
            Assert.AreEqual("NAME = 'Acme'", ToSql("NAME eq 'Acme'"));
        }

        [TestMethod]
        public void ParseFilterToSql_EscapesSingleQuotesInStringLiteral()
        {
            Assert.AreEqual("NAME = 'O''Brien'", ToSql("NAME eq 'O''Brien'"));
        }

        [TestMethod]
        public void ParseFilterToSql_NeutralizesInjectionInStringValue()
        {
            // The doubled quotes keep the payload inside the literal; it cannot terminate the string.
            Assert.AreEqual("NAME = 'x'';DROP TABLE SECRETS--'", ToSql("NAME eq 'x'';DROP TABLE SECRETS--'"));
        }

        [TestMethod]
        public void ParseFilterToSql_EscapesBackslashInStringLiteral()
        {
            Assert.AreEqual(@"NAME = 'a\\b'", ToSql(@"NAME eq 'a\b'"));
        }

        [TestMethod]
        public void ParseFilterToSql_EscapesTrailingBackslash()
        {
            // A trailing backslash must be doubled so it cannot escape the closing quote in Snowflake.
            Assert.AreEqual(@"NAME = 'a\\'", ToSql(@"NAME eq 'a\'"));
        }

        [TestMethod]
        public void ParseFilterToSql_NeutralizesBackslashQuoteBreakout()
        {
            // OData literal '\''; DROP' -> value: \'; DROP
            // Backslash is doubled first, then the quote, so neither can break out of the literal.
            Assert.AreEqual(@"NAME = '\\''; DROP'", ToSql(@"NAME eq '\''; DROP'"));
        }

        // ---- comparison operators ----

        [DataTestMethod]
        [DataRow("AGE eq 5", "AGE = 5")]
        [DataRow("AGE ne 5", "AGE <> 5")]
        [DataRow("AGE gt 5", "AGE > 5")]
        [DataRow("AGE ge 5", "AGE >= 5")]
        [DataRow("AGE lt 5", "AGE < 5")]
        [DataRow("AGE le 5", "AGE <= 5")]
        public void ParseFilterToSql_ComparisonOperators(string filter, string expected)
        {
            Assert.AreEqual(expected, ToSql(filter));
        }

        // ---- logical operators ----

        [TestMethod]
        public void ParseFilterToSql_And()
        {
            Assert.AreEqual("(NAME = 'a') AND (AGE > 5)", ToSql("NAME eq 'a' and AGE gt 5"));
        }

        [TestMethod]
        public void ParseFilterToSql_Or()
        {
            Assert.AreEqual("(NAME = 'a') OR (AGE > 5)", ToSql("NAME eq 'a' or AGE gt 5"));
        }

        [TestMethod]
        public void ParseFilterToSql_Not()
        {
            Assert.AreEqual("NOT (AGE > 5)", ToSql("not (AGE gt 5)"));
        }

        // ---- string functions ----

        [DataTestMethod]
        [DataRow("contains(NAME,'foo')", "NAME LIKE '%foo%'")]
        [DataRow("startswith(NAME,'foo')", "NAME LIKE 'foo%'")]
        [DataRow("endswith(NAME,'foo')", "NAME LIKE '%foo'")]
        public void ParseFilterToSql_StringFunctions(string filter, string expected)
        {
            Assert.AreEqual(expected, ToSql(filter));
        }

        [TestMethod]
        public void ParseFilterToSql_ContainsEscapesQuotesInValue()
        {
            Assert.AreEqual("NAME LIKE '%a''b%'", ToSql("contains(NAME,'a''b')"));
        }

        // ---- null + open (dynamic) property ----

        [TestMethod]
        public void ParseFilterToSql_NullConstant()
        {
            Assert.AreEqual("NAME = NULL", ToSql("NAME eq null"));
        }

        [TestMethod]
        public void ParseFilterToSql_OpenPropertyName_Emitted()
        {
            Assert.AreEqual("STATUS = 'x'", ToSql("STATUS eq 'x'"));
        }

        // ---- property-name boundary: special chars/spaces/quotes cannot be expressed as a name ----

        [TestMethod]
        public void ParseFilterToSql_SingleQuotedToken_IsLiteralNotIdentifier()
        {
            // A single-quoted token is an OData string literal, not a property name.
            Assert.AreEqual("'COL' = 'x'", ToSql("'COL' eq 'x'"));
        }

        [DataTestMethod]
        [DataRow("first name eq 'x'")]          // space => adjacent tokens, syntax error
        [DataRow("first-name eq 'x'")]          // hyphen => arithmetic, not an identifier (unsupported operator)
        [DataRow("a\"b eq 'x'")]                // double quote is not a valid OData identifier char
        [DataRow("\"col\" eq 'x'")]             // leading double quote is not valid OData
        [DataRow("col;DROP eq 'x'")]            // semicolon is not a valid identifier char
        public void ParseFilterToSql_NamesWithSpacesOrSpecialChars_CannotBeExpressed(string filter)
        {
            Assert.IsTrue(
                CannotBeConvertedToSql(filter),
                "Expected the OData layer (or parser) to reject this as a property identifier.");
        }
    }
}
