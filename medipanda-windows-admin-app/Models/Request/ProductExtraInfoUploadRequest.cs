using System.Text.Json.Serialization;

namespace medipanda_windows_admin.Models.Request
{
    public class ProductExtraInfoUploadRequest
    {
        [JsonPropertyName("manufacturerName")]
        public string? ManufacturerName { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("productName")]
        public string? ProductName { get; set; }

        [JsonPropertyName("composition")]
        public string? Composition { get; set; }

        [JsonPropertyName("price")]
        public int? Price { get; set; }

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = "";

        [JsonPropertyName("feeRate")]
        public string? FeeRate { get; set; }

        [JsonPropertyName("changedFeeRate")]
        public string? ChangedFeeRate { get; set; }

        [JsonPropertyName("changedMonth")]
        public int? ChangedMonth { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }
    }
}