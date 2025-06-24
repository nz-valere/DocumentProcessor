using System.Text.Json.Serialization;

namespace ImageOcrMicroservice.Models
{

    public class DocumentMetadata
    {
        [JsonPropertyName("document_name")]
        public string DocumentName { get; set; } = string.Empty;

        [JsonPropertyName("document_type")]
        public string DocumentType { get; set; } = string.Empty;

        [JsonPropertyName("niu_numbers")]
        public List<string> NiuNumbers { get; set; } = new List<string>();

        [JsonPropertyName("rccm_numbers")]
        public List<string> RccmNumbers { get; set; } = new List<string>();

        [JsonPropertyName("business_names")]
        public List<string> BusinessNames { get; set; } = new List<string>();

        [JsonPropertyName("dates")]
        public List<string> Dates { get; set; } = new List<string>();

        [JsonPropertyName("registration_numbers")]
        public List<string> RegistrationNumbers { get; set; } = new List<string>();

        [JsonPropertyName("company_addresses")]
        public List<string> CompanyAddresses { get; set; } = new List<string>();

        [JsonPropertyName("legal_forms")]
        public List<string> LegalForms { get; set; } = new List<string>();

        [JsonPropertyName("capital_amounts")]
        public List<string> CapitalAmounts { get; set; } = new List<string>();

        [JsonPropertyName("registration_dates")]
        public List<string> RegistrationDates { get; set; } = new List<string>();

        [JsonPropertyName("delivered_dates")]
        public List<string> DeliveredDates { get; set; } = new List<string>();

        [JsonPropertyName("company_duration")]
        public List<string> CompanyDuration { get; set; } = new List<string>();

        [JsonPropertyName("tribunal_names")]
        public List<string> TribunalNames { get; set; } = new List<string>();

        [JsonPropertyName("activity_codes")]
        public List<string> ActivityCodes { get; set; } = new List<string>();

        // New tax-related properties
        [JsonPropertyName("tax_attestation_numbers")]
        public List<string> TaxAttestationNumbers { get; set; } = new List<string>();

        [JsonPropertyName("tax_centers")]
        public List<string> TaxCenters { get; set; } = new List<string>();

        [JsonPropertyName("tax_systems")]
        public List<string> TaxSystems { get; set; } = new List<string>();

        // Additional document properties
        [JsonPropertyName("acfe_references")]
        public List<string> AcfeReferences { get; set; } = new List<string>();

        [JsonPropertyName("document_locations_and_dates")]
        public List<string> DocumentLocationsAndDates { get; set; } = new List<string>();

        [JsonPropertyName("quarters")]
        public List<string> Quarters { get; set; } = new List<string>();

        [JsonPropertyName("phone_numbers")]
        public List<string> PhoneNumbers { get; set; } = new List<string>();

        [JsonPropertyName("email_addresses")]
        public List<string> EmailAddresses { get; set; } = new List<string>();

        [JsonPropertyName("regimes")]
        public List<string> Regimes { get; set; } = new List<string>();

        [JsonPropertyName("raw_text")]
        public string RawText { get; set; } = string.Empty;
    }
}