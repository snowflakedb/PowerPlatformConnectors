using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SnowflakeTestApp.Tests.Infrastructure
{
    /// <summary>
    /// Represents the OData response from the Snowflake connector API
    /// </summary>
    public class ODataResponse<T>
    {
        [JsonProperty("@odata.context")]
        public string ODataContext { get; set; }

        [JsonProperty("value")]
        public List<T> Value { get; set; }
    }

    /// <summary>
    /// Represents a customer item from the API response
    /// </summary>
    public class CustomerItem
    {
        [JsonProperty("ItemInternalId")]
        public string ItemInternalId { get; set; }

        [JsonProperty("ID")]
        public int ID { get; set; }

        [JsonProperty("NAME")]
        public string NAME { get; set; }

        [JsonProperty("EMAIL")]
        public string EMAIL { get; set; }

        [JsonProperty("PHONE")]
        public string PHONE { get; set; }

        [JsonProperty("CREATED_DATE")]
        public string CREATED_DATE { get; set; }

        [JsonProperty("IS_ACTIVE")]
        public string IS_ACTIVE { get; set; } // Note: This comes as string "true"/"false"

        [JsonProperty("BALANCE")]
        public decimal BALANCE { get; set; }

        /// <summary>
        /// Converts this CustomerItem to a TestDataRecord for easy comparison
        /// </summary>
        /// <returns>TestDataRecord equivalent</returns>
        public TestDataRecord ToTestDataRecord()
        {
            return new TestDataRecord
            {
                Id = this.ID,
                Name = this.NAME,
                Email = this.EMAIL,
                Phone = this.PHONE,
                IsActive = this.IS_ACTIVE?.Equals("true", StringComparison.OrdinalIgnoreCase) == true,
                Balance = this.BALANCE
            };
        }
    }
} 