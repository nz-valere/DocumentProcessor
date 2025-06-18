using System.Text.Json.Serialization;

namespace ImageOcrMicroservice.Models
{
    /// <summary>
    /// Represents the structured data extracted from a document.
    /// Each property is a list to handle cases where multiple instances of the same data type are found.
    /// </summary>
    public class DocumentMetadata
    {
        [JsonPropertyName("niu_numbers")]
        public List<string> NiuNumbers { get; set; } = new List<string>();

        [JsonPropertyName("rccm_numbers")]
        public List<string> RccmNumbers { get; set; } = new List<string>();

        [JsonPropertyName("business_names")]
        public List<string> BusinessNames { get; set; } = new List<string>();

        [JsonPropertyName("dates")]
        public List<string> Dates { get; set; } = new List<string>();

        [JsonPropertyName("raw_text")]
        public string RawText { get; set; } = string.Empty;
    }
}
