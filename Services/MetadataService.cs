using System.Text.RegularExpressions;
using ImageOcrMicroservice.Models;

namespace ImageOcrMicroservice.Services
{
    public class MetadataService
    {
        private readonly ILogger<MetadataService> _logger;
        private readonly DocumentTypeDetectionService _documentTypeDetectionService;
        private readonly DocumentSpecificMetadataService _documentSpecificMetadataService;

        // Enhanced regex patterns for French commercial register documents
        private static readonly Regex NiuRegex = new Regex(@"\b([MP]\d{12}[A-Z])\b", RegexOptions.Compiled);
        private static readonly Regex RccmRegex = new Regex(@"\b(RC[/\s]*[A-Z]{3,}[/\s]*\d{4}[/\s]*[A-Z][/\s]*\d{4})\b", RegexOptions.Compiled);
        private static readonly Regex DateRegex = new Regex(@"\b(\d{1,2}[/.-]\d{1,2}[/.-]\d{4})\b", RegexOptions.Compiled);
        private static readonly Regex BusinessNameRegex = new Regex(@"\b(SUKA SARL|ALLIANCE INFINIMENT|[A-Z][A-Z\s&-]+(?:SARL|SAS|SA|EURL|SNC|SCS|GIE))\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        // Additional patterns for commercial register documents based on actual raw text format
        private static readonly Regex RegistrationNumberRegex = new Regex(@"\b(\d{4}[A-Z]\d{8})\b", RegexOptions.Compiled);
        private static readonly Regex AddressRegex = new Regex(@"ADRESSE DU SIEGE\s*:\s*(.*?)(?=\n|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex LegalFormRegex = new Regex(@"\b(SARL|SAS|SA|EURL|SNC|SCS|GIE|Société Anonyme|Société à Responsabilité Limitée)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CapitalAmountRegex = new Regex(@"CAPITAL SOCIAL\s*:\s*(\d+(?:\.\d{3})*)\s*(FCFA|F CFA|€|EUR)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegistrationDateRegex = new Regex(@"(?:Date d'immatriculation|Immatriculé le|Date d'inscription)[\s:]*(\d{1,2}[/.-]\d{1,2}[/.-]\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DeliveredDateRegex = new Regex(@"Déposée le\s*(\d{1,2}/\d{1,2}/\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DurationRegex = new Regex(@"Durée\s*:\s*(\d+\s*ANS?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TribunalRegex = new Regex(@"(?:Tribunal|Greffe)[\s:]*([A-Z][A-Z\s-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ActivityCodeRegex = new Regex(@"(?:Code APE|Activité principale|Code NAF)[\s:]*(\d{4}[A-Z]?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // New tax-related regex patterns
        private static readonly Regex TaxAttestationNumberRegex = new Regex(@"(?:ATTESTATION D'IMMATRICULATION|ATTESTATION OF TAXPAYERS REGISTRATION)\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TaxCenterRegex = new Regex(@"Centre des impôts de rattachement\s*:\s*(.*?)(?=\n|Tax center)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TaxSystemRegex = new Regex(@"Régimefiscal\s*:\s*(.*?)(?=\n|Tax system)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Additional document information regex patterns
        private static readonly Regex AcfeReferenceRegex = new Regex(@"(?:Réference\s+ACFE|Reference\s+ACFE|ACFE\s*:?)\s*:?\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DocumentLocationDateRegex = new Regex(@"\b([A-Z][A-Z\s-]+)\s+(\d{1,2}/\d{1,2}/\d{4})\b", RegexOptions.Compiled);
        private static readonly Regex QuarterRegex = new Regex(@"(?:Lieu Dit|Quarter|Quartier)\s*([A-Z][A-Z\s-]+)(?:\s+B\.P\.?:?|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex PhoneNumberRegex = new Regex(@"(?:Tél\s*fixe|Téléphone|Phone|Tel)\s*:?\s*(\d{9,15})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new Regex(@"(?:Adresse électronique|e[\-\s]*mail|Email)\s*\(?[:\s]*\)?\s*([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RegimeRegex = new Regex(@"REGIME\s*:\s*(.*?)(?=\n|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public MetadataService(
            ILogger<MetadataService> logger,
            DocumentTypeDetectionService documentTypeDetectionService,
            DocumentSpecificMetadataService documentSpecificMetadataService)
        {
            _logger = logger;
            _documentTypeDetectionService = documentTypeDetectionService;
            _documentSpecificMetadataService = documentSpecificMetadataService;
        }

        public DocumentMetadata ExtractMetadata(string rawText, string? fileName = null)
        {
            _logger.LogInformation("Starting metadata extraction from raw text for file '{FileName}'.", fileName ?? "Unknown");

            // Step 1: Detect document type from filename
            var documentType = _documentTypeDetectionService.DetectDocumentType(fileName);
            _logger.LogInformation("Detected document type: {DocumentType} for file '{FileName}'", documentType, fileName ?? "Unknown");

            // Step 2: Extract all possible metadata (full extraction)
            var fullMetadata = ExtractAllMetadata(rawText, fileName, documentType);

            // Step 3: Filter metadata based on document type
            var filteredMetadata = _documentSpecificMetadataService.FilterMetadataByDocumentType(fullMetadata, documentType);

            // Step 4: Validate the filtered metadata
            var validation = _documentSpecificMetadataService.ValidateDocumentMetadata(filteredMetadata, documentType);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Validation failed for document type {DocumentType}: {ValidationMessages}", 
                    documentType, string.Join(", ", validation.Messages));
            }

            // Step 5: Log summary
            var summary = _documentSpecificMetadataService.GetMetadataSummary(filteredMetadata, documentType);
            _logger.LogInformation("Metadata extraction completed for '{FileName}'. Document type: {DocumentType}, " +
                                 "Extracted fields: {ExtractedFieldsCount}, Is filtered: {IsFiltered}",
                                 fileName ?? "Unknown", documentType, summary["ExtractedFieldsCount"], summary["IsFiltered"]);

            return filteredMetadata;
        }

        private DocumentMetadata ExtractAllMetadata(string rawText, string? fileName, DocumentType detectedDocumentType)
        {
            var metadata = new DocumentMetadata
            {
                DocumentName = ExtractDocumentName(fileName),
                DocumentType = _documentTypeDetectionService.GetDocumentTypeDisplayName(detectedDocumentType),
                NiuNumbers = ExtractMatches(rawText, NiuRegex),
                RccmNumbers = ExtractMatches(rawText, RccmRegex),
                BusinessNames = ExtractMatches(rawText, BusinessNameRegex),
                Dates = ExtractMatches(rawText, DateRegex),
                RegistrationNumbers = ExtractMatches(rawText, RegistrationNumberRegex),
                CompanyAddresses = ExtractSpecialMatches(rawText, AddressRegex),
                LegalForms = ExtractMatches(rawText, LegalFormRegex),
                CapitalAmounts = ExtractCapitalAmounts(rawText),
                RegistrationDates = ExtractSpecialMatches(rawText, RegistrationDateRegex),
                DeliveredDates = ExtractSpecialMatches(rawText, DeliveredDateRegex),
                CompanyDuration = ExtractSpecialMatches(rawText, DurationRegex),
                TribunalNames = ExtractSpecialMatches(rawText, TribunalRegex),
                ActivityCodes = ExtractSpecialMatches(rawText, ActivityCodeRegex),
                // Tax-related extractions
                TaxAttestationNumbers = ExtractTaxAttestationNumbers(rawText),
                TaxCenters = ExtractSpecialMatches(rawText, TaxCenterRegex),
                TaxSystems = ExtractSpecialMatches(rawText, TaxSystemRegex),
                // Additional document information extractions
                AcfeReferences = ExtractSpecialMatches(rawText, AcfeReferenceRegex),
                DocumentLocationsAndDates = ExtractDocumentLocationsAndDates(rawText),
                Quarters = ExtractSpecialMatches(rawText, QuarterRegex),
                PhoneNumbers = ExtractSpecialMatches(rawText, PhoneNumberRegex),
                EmailAddresses = ExtractEmailAddresses(rawText),
                Regimes = ExtractSpecialMatches(rawText, RegimeRegex),
                RawText = rawText
            };

            _logger.LogDebug("Full metadata extraction completed. Found {NiuCount} NIU(s), {RccmCount} RCCM(s), " +
                           "{DateCount} Date(s), {NameCount} Name(s), {RegCount} Registration(s), {DeliveredCount} Delivered Date(s), " +
                           "{DurationCount} Duration(s), {TaxAttestationCount} Tax Attestation(s), {TaxCenterCount} Tax Center(s), " +
                           "{TaxSystemCount} Tax System(s), {AcfeCount} ACFE Reference(s), {LocationDateCount} Location/Date(s), " +
                           "{QuarterCount} Quarter(s), {PhoneCount} Phone(s), {EmailCount} Email(s), {RegimeCount} Regime(s).",
                           metadata.NiuNumbers.Count, metadata.RccmNumbers.Count, metadata.Dates.Count, 
                           metadata.BusinessNames.Count, metadata.RegistrationNumbers.Count, 
                           metadata.DeliveredDates.Count, metadata.CompanyDuration.Count,
                           metadata.TaxAttestationNumbers.Count, metadata.TaxCenters.Count, metadata.TaxSystems.Count,
                           metadata.AcfeReferences.Count, metadata.DocumentLocationsAndDates.Count, metadata.Quarters.Count,
                           metadata.PhoneNumbers.Count, metadata.EmailAddresses.Count, metadata.Regimes.Count);

            return metadata;
        }

        private string ExtractDocumentName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "Unknown Document";

            // Remove file extension and clean up the name
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            
            // Convert common patterns to readable names
            if (nameWithoutExtension.StartsWith("REGISTRECOMMERCE", StringComparison.OrdinalIgnoreCase))
                return "Registre du Commerce";
            if (nameWithoutExtension.StartsWith("EXTRAIT", StringComparison.OrdinalIgnoreCase))
                return "Extrait du Registre du Commerce";
            if (nameWithoutExtension.StartsWith("KBIS", StringComparison.OrdinalIgnoreCase))
                return "Extrait K-bis";
            if (nameWithoutExtension.StartsWith("ATTESTATION", StringComparison.OrdinalIgnoreCase))
                return "Attestation d'Immatriculation";
            if (nameWithoutExtension.StartsWith("FORMULAIREAGREGEOM", StringComparison.OrdinalIgnoreCase))
                return "Formulaire Agrégé OM";
            if (nameWithoutExtension.StartsWith("CNI", StringComparison.OrdinalIgnoreCase) || 
                nameWithoutExtension.StartsWith("RECIPICE", StringComparison.OrdinalIgnoreCase))
                return "CNI ou Récépissé";

            // Return the cleaned filename if no specific pattern matches
            return nameWithoutExtension;
        }

        private List<string> ExtractMatches(string text, Regex regex)
        {
            var matches = new List<string>();
            
            try
            {
                var regexMatches = regex.Matches(text);
                foreach (Match match in regexMatches)
                {
                    if (match.Success && match.Groups.Count > 1)
                    {
                        var value = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrWhiteSpace(value) && !matches.Contains(value))
                        {
                            matches.Add(value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting matches with regex pattern");
            }

            return matches;
        }

        private List<string> ExtractSpecialMatches(string text, Regex regex)
        {
            var matches = new List<string>();
            
            try
            {
                var regexMatches = regex.Matches(text);
                foreach (Match match in regexMatches)
                {
                    if (match.Success && match.Groups.Count > 1)
                    {
                        var value = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrWhiteSpace(value) && !matches.Contains(value))
                        {
                            matches.Add(value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting special matches with regex pattern");
            }

            return matches;
        }

        private List<string> ExtractCapitalAmounts(string text)
        {
            var amounts = new List<string>();
            
            try
            {
                var matches = CapitalAmountRegex.Matches(text);
                foreach (Match match in matches)
                {
                    if (match.Success && match.Groups.Count >= 3)
                    {
                        var amount = match.Groups[1].Value.Trim();
                        var currency = match.Groups[2].Value.Trim();
                        var fullAmount = $"{amount} {currency}";
                        
                        if (!string.IsNullOrWhiteSpace(amount) && !amounts.Contains(fullAmount))
                        {
                            amounts.Add(fullAmount);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting capital amounts");
            }

            return amounts;
        }

        private List<string> ExtractTaxAttestationNumbers(string text)
        {
            var numbers = new List<string>();
            
            try
            {
                // Try the specific tax attestation pattern first
                var attestationMatches = TaxAttestationNumberRegex.Matches(text);
                foreach (Match match in attestationMatches)
                {
                    if (match.Success && match.Groups.Count > 1)
                    {
                        var number = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrWhiteSpace(number) && !numbers.Contains(number))
                        {
                            numbers.Add(number);
                        }
                    }
                }

                // Also look for generic attestation numbers
                var genericPattern = new Regex(@"N°\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                var genericMatches = genericPattern.Matches(text);
                foreach (Match match in genericMatches)
                {
                    if (match.Success && match.Groups.Count > 1)
                    {
                        var number = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrWhiteSpace(number) && !numbers.Contains(number))
                        {
                            numbers.Add(number);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting tax attestation numbers");
            }

            return numbers;
        }

        private List<string> ExtractDocumentLocationsAndDates(string text)
        {
            var locationsAndDates = new List<string>();
            
            try
            {
                var matches = DocumentLocationDateRegex.Matches(text);
                foreach (Match match in matches)
                {
                    if (match.Success && match.Groups.Count >= 3)
                    {
                        var location = match.Groups[1].Value.Trim();
                        var date = match.Groups[2].Value.Trim();
                        var locationDate = $"{location}, {date}";
                        
                        if (!string.IsNullOrWhiteSpace(location) && !string.IsNullOrWhiteSpace(date) && 
                            !locationsAndDates.Contains(locationDate))
                        {
                            locationsAndDates.Add(locationDate);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting document locations and dates");
            }

            return locationsAndDates;
        }

        private List<string> ExtractEmailAddresses(string text)
        {
            var emails = new List<string>();
            
            try
            {
                var matches = EmailRegex.Matches(text);
                foreach (Match match in matches)
                {
                    if (match.Success && match.Groups.Count > 1)
                    {
                        var email = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrWhiteSpace(email) && IsValidEmail(email) && !emails.Contains(email))
                        {
                            emails.Add(email);
                        }
                    }
                }

                // Additional simple email pattern for cases where the complex regex might miss
                var simpleEmailPattern = new Regex(@"\b[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}\b", RegexOptions.Compiled);
                var simpleMatches = simpleEmailPattern.Matches(text);
                foreach (Match match in simpleMatches)
                {
                    if (match.Success)
                    {
                        var email = match.Value.Trim();
                        if (!string.IsNullOrWhiteSpace(email) && IsValidEmail(email) && !emails.Contains(email))
                        {
                            emails.Add(email);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting email addresses");
            }

            return emails;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Simple validation - check if it contains @ and has at least one dot after @
                var atIndex = email.IndexOf('@');
                if (atIndex <= 0 || atIndex == email.Length - 1)
                    return false;

                var domainPart = email.Substring(atIndex + 1);
                return domainPart.Contains('.') && domainPart.Length > 3;
            }
            catch
            {
                return false;
            }
        }

        // Method to extract metadata for a specific document type only
        public DocumentMetadata ExtractMetadataForDocumentType(string rawText, DocumentType documentType, string? fileName = null)
        {
            _logger.LogInformation("Starting targeted metadata extraction for document type '{DocumentType}' from file '{FileName}'.", 
                documentType, fileName ?? "Unknown");

            // Extract all metadata first
            var fullMetadata = ExtractAllMetadata(rawText, fileName, documentType);

            // Filter based on the specific document type
            var filteredMetadata = _documentSpecificMetadataService.FilterMetadataByDocumentType(fullMetadata, documentType);

            // Validate the filtered metadata
            var validation = _documentSpecificMetadataService.ValidateDocumentMetadata(filteredMetadata, documentType);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Validation failed for document type {DocumentType}: {ValidationMessages}", 
                    documentType, string.Join(", ", validation.Messages));
            }

            return filteredMetadata;
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
    }
}