using ImageOcrMicroservice.Models;
using System.Reflection;

namespace ImageOcrMicroservice.Services
{
    public class DocumentSpecificMetadataService
    {
        private readonly ILogger<DocumentSpecificMetadataService> _logger;

        public DocumentSpecificMetadataService(ILogger<DocumentSpecificMetadataService> logger)
        {
            _logger = logger;
        }

        public DocumentMetadata FilterMetadataByDocumentType(DocumentMetadata fullMetadata, DocumentType documentType)
        {
            if (documentType == DocumentType.Unknown)
            {
                _logger.LogInformation("Document type is Unknown, returning all metadata fields.");
                return fullMetadata;
            }

            var allowedFields = DocumentTypeFieldMapping.GetFieldsForDocumentType(documentType);
            var filteredMetadata = CreateFilteredMetadata(fullMetadata, allowedFields);

            _logger.LogInformation("Filtered metadata for document type '{DocumentType}'. " +
                                 "Allowed fields: [{AllowedFields}]", 
                                 documentType, string.Join(", ", allowedFields));

            return filteredMetadata;
        }

        private DocumentMetadata CreateFilteredMetadata(DocumentMetadata source, HashSet<string> allowedFields)
        {
            var filtered = new DocumentMetadata();
            var sourceType = typeof(DocumentMetadata);
            var filteredType = typeof(DocumentMetadata);

            foreach (var property in sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!allowedFields.Contains(property.Name))
                {
                    // Set empty values for non-allowed fields
                    SetEmptyValue(filtered, property);
                    continue;
                }

                // Copy allowed field values
                var sourceValue = property.GetValue(source);
                var filteredProperty = filteredType.GetProperty(property.Name);
                
                if (filteredProperty != null && filteredProperty.CanWrite)
                {
                    filteredProperty.SetValue(filtered, sourceValue);
                }
            }

            return filtered;
        }

        private void SetEmptyValue(DocumentMetadata metadata, PropertyInfo property)
        {
            try
            {
                if (property.PropertyType == typeof(string))
                {
                    property.SetValue(metadata, string.Empty);
                }
                else if (property.PropertyType == typeof(List<string>))
                {
                    property.SetValue(metadata, new List<string>());
                }
                else
                {
                    // For other types, try to set to default value
                    var defaultValue = property.PropertyType.IsValueType 
                        ? Activator.CreateInstance(property.PropertyType) 
                        : null;
                    property.SetValue(metadata, defaultValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set empty value for property '{PropertyName}'", property.Name);
            }
        }

        public Dictionary<string, object> GetMetadataSummary(DocumentMetadata metadata, DocumentType documentType)
        {
            var summary = new Dictionary<string, object>
            {
                ["DocumentType"] = documentType.ToString(),
                ["DocumentTypeDisplayName"] = GetDocumentTypeDisplayName(documentType),
                ["ExtractedFieldsCount"] = CountNonEmptyFields(metadata),
                ["IsFiltered"] = documentType != DocumentType.Unknown
            };

            if (documentType != DocumentType.Unknown)
            {
                var allowedFields = DocumentTypeFieldMapping.GetFieldsForDocumentType(documentType);
                summary["AllowedFields"] = allowedFields.ToList();
                summary["AllowedFieldsCount"] = allowedFields.Count;
            }

            return summary;
        }

        private int CountNonEmptyFields(DocumentMetadata metadata)
        {
            var count = 0;
            var metadataType = typeof(DocumentMetadata);

            foreach (var property in metadataType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
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

        private string GetDocumentTypeDisplayName(DocumentType documentType)
        {
            return documentType switch
            {
                DocumentType.FormulaireAgregeOM => "Formulaire Agrégé OM",
                DocumentType.CniOrRecipice => "CNI ou Récépissé",
                DocumentType.RegistreCommerce => "Registre du Commerce",
                DocumentType.CarteContribuabledValide => "Carte Contribuable Valide",
                DocumentType.AttestationFiscale => "Attestation Fiscale",
                DocumentType.Unknown => "Type de Document Inconnu",
                _ => "Document Commercial"
            };
        }

        // Method to validate if a document has the expected fields for its type
        public ValidationResult ValidateDocumentMetadata(DocumentMetadata metadata, DocumentType documentType)
        {
            var result = new ValidationResult { IsValid = true, Messages = new List<string>() };

            if (documentType == DocumentType.Unknown)
            {
                result.Messages.Add("Document type is unknown - validation skipped");
                return result;
            }

            var allowedFields = DocumentTypeFieldMapping.GetFieldsForDocumentType(documentType);
            var criticalFields = GetCriticalFieldsForDocumentType(documentType);

            foreach (var criticalField in criticalFields)
            {
                if (!HasNonEmptyValue(metadata, criticalField))
                {
                    result.IsValid = false;
                    result.Messages.Add($"Critical field '{criticalField}' is missing or empty");
                }
            }

            return result;
        }

        private HashSet<string> GetCriticalFieldsForDocumentType(DocumentType documentType)
        {
            return documentType switch
            {
                DocumentType.FormulaireAgregeOM => new HashSet<string> { "BusinessNames", "RegistrationNumbers" },
                DocumentType.CniOrRecipice => new HashSet<string> { "RegistrationNumbers" },
                DocumentType.RegistreCommerce => new HashSet<string> { "RccmNumbers", "BusinessNames" },
                DocumentType.CarteContribuabledValide => new HashSet<string> { "NiuNumbers", "BusinessNames" },
                DocumentType.AttestationFiscale => new HashSet<string> { "TaxAttestationNumbers", "BusinessNames" },
                _ => new HashSet<string>()
            };
        }

        private bool HasNonEmptyValue(DocumentMetadata metadata, string fieldName)
        {
            var property = typeof(DocumentMetadata).GetProperty(fieldName);
            if (property == null) return false;

            var value = property.GetValue(metadata);
            
            return value switch
            {
                string stringValue => !string.IsNullOrWhiteSpace(stringValue),
                List<string> listValue => listValue.Any(),
                _ => value != null
            };
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
    }
}