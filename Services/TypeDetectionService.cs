using ImageOcrMicroservice.Models;

namespace ImageOcrMicroservice.Services
{
    public class DocumentTypeDetectionService
    {
        private readonly ILogger<DocumentTypeDetectionService> _logger;

        // Document type detection patterns
        private static readonly Dictionary<DocumentType, List<string>> FileNamePatterns = new()
        {
            {
                DocumentType.FormulaireAgregeOM,
                new List<string> { "formulaireagregeom", "formulaire_agrege_om", "agrege_om" }
            },
            {
                DocumentType.CniOrRecipice,
                new List<string> { "cni", "recipice", "carte_identite", "identite" }
            },
            {
                DocumentType.RegistreCommerce,
                new List<string> { "registrecommerce", "registre_commerce", "extrait", "kbis", "commerce" }
            },
            {
                DocumentType.CarteContribuabledValide,
                new List<string> { "cartecontribuablevalide", "carte_contribuable", "contribuable_valide", "contribuable" }
            },
            {
                DocumentType.AttestationFiscale,
                new List<string> 
                { 
                    "attestationnonredevance", 
                    "attestation_non_redevance", 
                    "attestation_conformite_fiscal", 
                    "conformite_fiscal",
                    "attestation_fiscal",
                    "non_redevance"
                }
            }
        };

        public DocumentTypeDetectionService(ILogger<DocumentTypeDetectionService> logger)
        {
            _logger = logger;
        }

        public DocumentType DetectDocumentType(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogWarning("File name is null or empty, returning Unknown document type.");
                return DocumentType.Unknown;
            }

            var normalizedFileName = NormalizeFileName(fileName);
            _logger.LogDebug("Normalized file name: '{NormalizedFileName}' from original: '{FileName}'", normalizedFileName, fileName);

            foreach (var (documentType, patterns) in FileNamePatterns)
            {
                if (patterns.Any(pattern => normalizedFileName.Contains(pattern)))
                {
                    _logger.LogInformation("Detected document type '{DocumentType}' for file '{FileName}'", documentType, fileName);
                    return documentType;
                }
            }

            _logger.LogInformation("Could not detect document type for file '{FileName}', returning Unknown", fileName);
            return DocumentType.Unknown;
        }

        public string GetDocumentTypeDisplayName(DocumentType documentType)
        {
            return documentType switch
            {
                DocumentType.FormulaireAgregeOM => "Formulaire Agrégé OM",
                DocumentType.CniOrRecipice => "CNI ou Récépissé",
                DocumentType.RegistreCommerce => "Registre du Commerce",
                DocumentType.CarteContribuabledValide => "Carte Contribuable Valide",
                DocumentType.AttestationFiscale => "Attestation Fiscale",
                DocumentType.Unknown => "Document Type Unknown",
                _ => "Business Document"
            };
        }

        public bool IsSpecificDocumentType(DocumentType documentType)
        {
            return documentType != DocumentType.Unknown;
        }

        private static string NormalizeFileName(string fileName)
        {
            // Remove file extension and convert to lowercase
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            
            // Remove special characters and spaces, convert to lowercase
            return nameWithoutExtension
                .ToLowerInvariant()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "")
                .Replace(".", "");
        }

        // Method to get all supported document types for API documentation or UI
        public static IEnumerable<DocumentType> GetSupportedDocumentTypes()
        {
            return FileNamePatterns.Keys;
        }

        // Method to get patterns for a specific document type (useful for testing or documentation)
        public static List<string> GetPatternsForDocumentType(DocumentType documentType)
        {
            return FileNamePatterns.TryGetValue(documentType, out var patterns) 
                ? new List<string>(patterns) 
                : new List<string>();
        }
    }
}