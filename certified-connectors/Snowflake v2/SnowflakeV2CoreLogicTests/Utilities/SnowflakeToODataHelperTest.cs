namespace SnowflakeV2CoreLogic.Tests.Utilities
{
    using SnowflakeV2CoreLogic;
    using SnowflakeV2CoreLogic.Utilities;

    /// <summary>
    /// Unit tests for <see cref="SnowflakeToODataHelper"/> timestamp conversion logic.
    ///
    /// These tests reproduce and guard against the issue reported on connector v2.2.0 where
    /// TIMESTAMP_NTZ columns with a scale other than the default 9 caused either an
    /// IndexOutOfRangeException (scale 0, no fractional part returned by Snowflake) or silent
    /// precision corruption (scales 1-8 were interpreted as raw nanoseconds).
    ///
    /// Snowflake returns these timestamps as "secondsSinceEpoch[.fractionalSeconds]" where the
    /// number of fractional digits matches the column scale. The base second value used below is
    /// 1700000000 == 2023-11-14 22:13:20 UTC.
    /// </summary>
    [TestClass]
    public sealed class SnowflakeToODataHelperTest
    {
        // Scale 0: Snowflake omits the fractional component entirely (no '.'). This is the exact
        // input that triggered the reported IndexOutOfRangeException.
        [DataRow("1700000000", "2023-11-14 22:13:20.0000000", DisplayName = "Scale 0 (second precision, no fractional part)")]
        [DataRow("1700000000.5", "2023-11-14 22:13:20.5000000", DisplayName = "Scale 1")]
        [DataRow("1700000000.05", "2023-11-14 22:13:20.0500000", DisplayName = "Scale 2")]
        [DataRow("1700000000.123", "2023-11-14 22:13:20.1230000", DisplayName = "Scale 3")]
        [DataRow("1700000000.123456", "2023-11-14 22:13:20.1234560", DisplayName = "Scale 6 (microsecond precision)")]
        [DataRow("1700000000.123456789", "2023-11-14 22:13:20.1234567", DisplayName = "Scale 9 (truncated to .NET 100ns tick resolution)")]
        [DataTestMethod]
        public void CastSnowflakeDataToCorrectType_TimestampNtz_SupportsAllScales(string snowflakeValue, string expected)
        {
            var result = SnowflakeToODataHelper.CastSnowflakeDataToCorrectType(
                Constants.SFDataTypeTimestampNoTimeZone,
                precision: null,
                data: snowflakeValue);

            Assert.AreEqual(expected, result);
        }

        // TIMESTAMP_LTZ is parsed through the same NTZ code path, so it must support every scale too.
        [DataRow("1700000000", "2023-11-14 22:13:20.0000000", DisplayName = "Scale 0")]
        [DataRow("1700000000.1", "2023-11-14 22:13:20.1000000", DisplayName = "Scale 1")]
        [DataRow("1700000000.12", "2023-11-14 22:13:20.1200000", DisplayName = "Scale 2")]
        [DataRow("1700000000.123", "2023-11-14 22:13:20.1230000", DisplayName = "Scale 3")]
        [DataRow("1700000000.123456", "2023-11-14 22:13:20.1234560", DisplayName = "Scale 6")]
        [DataRow("1700000000.123456789", "2023-11-14 22:13:20.1234567", DisplayName = "Scale 9")]
        [DataTestMethod]
        public void CastSnowflakeDataToCorrectType_TimestampLtz_SupportsAllScales(string snowflakeValue, string expected)
        {
            var result = SnowflakeToODataHelper.CastSnowflakeDataToCorrectType(
                Constants.SFDataTypeTimestampLocalTimeZone,
                precision: null,
                data: snowflakeValue);

            Assert.AreEqual(expected, result);
        }

        // TIMESTAMP_TZ carries a trailing time zone offset which is ignored (Snowflake stores UTC).
        // Scale 0 has no fractional part, only "seconds +offset".
        [DataRow("1700000000 +0000", "2023-11-14 22:13:20.0000000", DisplayName = "Scale 0")]
        [DataRow("1700000000.5 -0800", "2023-11-14 22:13:20.5000000", DisplayName = "Scale 1")]
        [DataRow("1700000000.05 +0530", "2023-11-14 22:13:20.0500000", DisplayName = "Scale 2")]
        [DataRow("1700000000.123 +0000", "2023-11-14 22:13:20.1230000", DisplayName = "Scale 3")]
        [DataRow("1700000000.123456 +0000", "2023-11-14 22:13:20.1234560", DisplayName = "Scale 6")]
        [DataRow("1700000000.123456789 +0000", "2023-11-14 22:13:20.1234567", DisplayName = "Scale 9")]
        [DataTestMethod]
        public void CastSnowflakeDataToCorrectType_TimestampTz_SupportsAllScales(string snowflakeValue, string expected)
        {
            var result = SnowflakeToODataHelper.CastSnowflakeDataToCorrectType(
                Constants.SFDataTypeTimestampWithTimezone,
                precision: null,
                data: snowflakeValue);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CastSnowflakeDataToCorrectType_NullData_ReturnsNull()
        {
            var result = SnowflakeToODataHelper.CastSnowflakeDataToCorrectType(
                Constants.SFDataTypeTimestampNoTimeZone,
                precision: null,
                data: null!);

            Assert.IsNull(result);
        }
    }
}
