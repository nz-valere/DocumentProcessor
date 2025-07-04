using System.Text.RegularExpressions;
using ImageOcrMicroservice.Models;

namespace ImageOcrMicroservice.Services
{
    public class MetadataService
    {
        private readonly ILogger<MetadataService> _logger;
        private readonly DocumentTypeDetectionService _documentTypeDetectionService;
        private readonly DocumentSpecificMetadataService _documentSpecificMetadataService;
        private readonly RegexService _regexService;

        public MetadataService(
            ILogger<MetadataService> logger,
            DocumentTypeDetectionService documentTypeDetectionService,
            DocumentSpecificMetadataService documentSpecificMetadataService,
            RegexService regexService)
        {
            _logger = logger;
            _documentTypeDetectionService = documentTypeDetectionService;
            _documentSpecificMetadataService = documentSpecificMetadataService;
            _regexService = regexService;
        }

        public DocumentMetadata ExtractMetadata(string rawText, string? fileName = null)
        {
            _logger.LogInformation("Starting metadata extraction from raw text for file '{FileName}'.", fileName ?? "Unknown");

            // Step 1: Detect document type from filename
            var documentType = _documentTypeDetectionService.DetectDocumentType(fileName);
            _logger.LogInformation("Detected document type: {DocumentType} for file '{FileName}'", documentType, fileName ?? "Unknown");

            // Step 2: Extract metadata using document-type-specific patterns
            var extractedMetadata = ExtractMetadataForDocumentType(rawText, fileName, documentType);

            // Step 3: Validate the extracted metadata
            var validation = _documentSpecificMetadataService.ValidateDocumentMetadata(extractedMetadata, documentType);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Validation failed for document type {DocumentType}: {ValidationMessages}", 
                    documentType, string.Join(", ", validation.Messages));
            }

            // Step 4: Log summary
            var summary = _documentSpecificMetadataService.GetMetadataSummary(extractedMetadata, documentType);
            _logger.LogInformation("Metadata extraction completed for '{FileName}'. Document type: {DocumentType}, " +
                                 "Extracted fields: {ExtractedFieldsCount}, Is filtered: {IsFiltered}",
                                 fileName ?? "Unknown", documentType, summary["ExtractedFieldsCount"], summary["IsFiltered"]);

            return extractedMetadata;
        }

        private DocumentMetadata ExtractMetadataForDocumentType(string rawText, string? fileName, DocumentType documentType)
        {
            _logger.LogInformation("Starting targeted metadata extraction for document type '{DocumentType}' from file '{FileName}'.", 
                documentType, fileName ?? "Unknown");

            // Initialize metadata with basic information
            var metadata = new DocumentMetadata
            {
                DocumentName = ExtractDocumentName(fileName),
                DocumentType = _documentTypeDetectionService.GetDocumentTypeDisplayName(documentType),
                RawText = rawText
            };

            // Extract fields using document-type-specific regex patterns
            var extractedFields = _regexService.ExtractFieldsForDocumentType(rawText, documentType);

            // Map extracted fields to metadata properties
            MapExtractedFieldsToMetadata(metadata, extractedFields);

            // Filter metadata based on document type to only include relevant fields
            var filteredMetadata = _documentSpecificMetadataService.FilterMetadataByDocumentType(metadata, documentType);

            LogExtractionResults(filteredMetadata, documentType);

            return filteredMetadata;
        }

        private void MapExtractedFieldsToMetadata(DocumentMetadata metadata, Dictionary<string, List<string>> extractedFields)
        {
            try
            {
                var metadataType = typeof(DocumentMetadata);
                foreach (var field in extractedFields)
                {
                    var property = metadataType.GetProperty(field.Key);
                    if (property != null && property.PropertyType == typeof(List<string>))
                    {
                        property.SetValue(metadata, field.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping extracted fields to metadata properties");
            }
        }

        private void LogExtractionResults(DocumentMetadata metadata, DocumentType documentType)
        {
            var fieldCounts = new Dictionary<string, int>();
            var metadataType = typeof(DocumentMetadata);

            foreach (var property in metadataType.GetProperties())
            {
                var value = property.GetValue(metadata);

                if (value is List<string> listValue && listValue.Any())
                {
                    fieldCounts[property.Name] = listValue.Count;
                }
            }

            var totalFieldsExtracted = fieldCounts.Sum(kvp => kvp.Value);
            var fieldsExtracted = string.Join(", ", fieldCounts.Select(kvp => $"{kvp.Key}: {kvp.Value}"));

            _logger.LogDebug("Extraction results for document type {DocumentType}: Total fields extracted: {TotalFields}. " +
                             "Field breakdown: {FieldBreakdown}",
                             documentType, totalFieldsExtracted, fieldsExtracted);
        }

        private string ExtractDocumentName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "Unknown Document";

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            var patterns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "REGISTRECOMMERCE", "Registre du Commerce" },
                { "EXTRAIT", "Extrait du Registre du Commerce" },
                { "KBIS", "Extrait K-bis" },
                { "ATTESTATION", "Attestation d'Immatriculation" },
                { "FORMULAIREAGREGEOM", "Formulaire Agrégé OM" },
                { "CNI", "CNI ou Récépissé" },
                { "RECIPICE", "CNI ou Récépissé" }
            };

            foreach (var pattern in patterns)
            {
                if (nameWithoutExtension.StartsWith(pattern.Key, StringComparison.OrdinalIgnoreCase))
                    return pattern.Value;
            }

            return nameWithoutExtension;
        }

        // Method to get extraction statistics
        public Dictionary<string, object> GetExtractionStatistics(DocumentMetadata metadata)
        {
            var stats = new Dictionary<string, object>
            {
                ["TotalFieldsExtracted"] = CountNonEmptyFields(metadata),
                ["NiuNumbersCount"] = metadata.NiuNumbers?.Count ?? 0,
                ["RccmNumbersCount"] = metadata.RccmNumbers?.Count ?? 0,
                ["BusinessNamesCount"] = metadata.BusinessNames?.Count ?? 0,
                ["DatesCount"] = metadata.Dates?.Count ?? 0,
                ["RegistrationNumbersCount"] = metadata.RegistrationNumbers?.Count ?? 0,
                ["TaxAttestationNumbersCount"] = metadata.TaxAttestationNumbers?.Count ?? 0,
                ["PhoneNumbersCount"] = metadata.PhoneNumbers?.Count ?? 0,
                ["EmailAddressesCount"] = metadata.EmailAddresses?.Count ?? 0,
                ["HasRawText"] = !string.IsNullOrWhiteSpace(metadata.RawText)
            };

            return stats;
        }

        private int CountNonEmptyFields(DocumentMetadata metadata)
        {
            var count = 0;
            var metadataType = typeof(DocumentMetadata);

            foreach (var property in metadataType.GetProperties())
            {
                var value = property.GetValue(metadata);
                
                if (value != null)
                {
                    if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                    {
                        count++;
                    }
                    else if (value is List<string> listValue && listValue.Any())
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        // Method to extract metadata for a specific document type only (compatibility method)
        public DocumentMetadata ExtractMetadataForDocumentType(string rawText, DocumentType documentType, string? fileName = null)
        {
            return ExtractMetadataForDocumentType(rawText, fileName, documentType);
        }
    }
}