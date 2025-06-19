using System.Text.RegularExpressions;
using ImageOcrMicroservice.Models;

namespace ImageOcrMicroservice.Services
{
    public class MetadataService
    {
        private readonly ILogger<MetadataService> _logger;

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

        public MetadataService(ILogger<MetadataService> logger)
        {
            _logger = logger;
        }

        public DocumentMetadata ExtractMetadata(string rawText, string? fileName = null)
        {
            _logger.LogInformation("Starting metadata extraction from raw text.");

            var metadata = new DocumentMetadata
            {
                DocumentName = ExtractDocumentName(fileName),
                DocumentType = DetermineDocumentType(rawText, fileName),
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
                // New tax-related extractions
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

            _logger.LogInformation("Finished metadata extraction. Found {NiuCount} NIU(s), {RccmCount} RCCM(s), {DateCount} Date(s), {NameCount} Name(s), {RegCount} Registration(s), {DeliveredCount} Delivered Date(s), {DurationCount} Duration(s), {TaxAttestationCount} Tax Attestation(s), {TaxCenterCount} Tax Center(s), {TaxSystemCount} Tax System(s), {AcfeCount} ACFE Reference(s), {LocationDateCount} Location/Date(s), {QuarterCount} Quarter(s), {PhoneCount} Phone(s), {EmailCount} Email(s), {RegimeCount} Regime(s).",
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
            
            return nameWithoutExtension;
        }

        private string DetermineDocumentType(string rawText, string? fileName)
        {
            var text = rawText.ToLowerInvariant();
            var name = fileName?.ToLowerInvariant() ?? "";

            if (text.Contains("attestation d'immatriculation") || text.Contains("attestation of taxpayers registration"))
                return "Tax Registration Certificate";
            if (text.Contains("registre du commerce") || name.Contains("registrecommerce"))
                return "Commercial Register";
            if (text.Contains("extrait k-bis") || text.Contains("kbis"))
                return "K-bis Extract";
            if (text.Contains("statuts") || text.Contains("articles of association"))
                return "Company Statutes";
            if (text.Contains("procès-verbal") || text.Contains("minutes"))
                return "Company Minutes";
            
            return "Business Document";
        }

        private List<string> ExtractMatches(string text, Regex regex)
        {
            return regex.Matches(text)
                        .Cast<Match>()
                        .Select(m => m.Value.Trim())
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .Distinct()
                        .ToList();
        }

        private List<string> ExtractSpecialMatches(string text, Regex regex)
        {
            return regex.Matches(text)
                        .Cast<Match>()
                        .Select(m => m.Groups.Count > 1 ? m.Groups[1].Value.Trim() : m.Value.Trim())
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .Distinct()
                        .ToList();
        }

        private List<string> ExtractCapitalAmounts(string text)
        {
            var matches = CapitalAmountRegex.Matches(text);
            var results = new List<string>();

            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 3)
                {
                    var amount = match.Groups[1].Value.Trim();
                    var currency = match.Groups[2].Value.Trim();
                    results.Add($"{amount} {currency}");
                }
            }

            return results.Distinct().ToList();
        }

        private List<string> ExtractDocumentLocationsAndDates(string text)
        {
            var results = new List<string>();
            var matches = DocumentLocationDateRegex.Matches(text);
            
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 3)
                {
                    var location = match.Groups[1].Value.Trim();
                    var date = match.Groups[2].Value.Trim();
                    results.Add($"{location} {date}");
                }
            }

            return results.Distinct().ToList();
        }

        private List<string> ExtractEmailAddresses(string text)
        {
            var results = new List<string>();
            var matches = EmailRegex.Matches(text);
            
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1 && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    results.Add(match.Groups[1].Value.Trim());
                }
                else
                {
                    // Check if email section exists but is empty (for tracking purposes)
                    if (match.Value.Contains("électronique") || match.Value.Contains("e-mail") || match.Value.Contains("Email"))
                    {
                        results.Add("(Empty)");
                    }
                }
            }

            return results.Distinct().ToList();
        }

        private List<string> ExtractTaxAttestationNumbers(string text)
        {
            var results = new List<string>();
            
            // Look for tax attestation numbers following the attestation text
            var matches = TaxAttestationNumberRegex.Matches(text);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    results.Add(match.Groups[1].Value.Trim());
                }
            }

            // Also look for standalone numeric sequences that might be attestation numbers
            // between attestation headers and other content
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Contains("ATTESTATION D'IMMATRICULATION", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("ATTESTATION OF TAXPAYERS REGISTRATION", StringComparison.OrdinalIgnoreCase))
                {
                    // Check next few lines for numeric sequences
                    for (int j = i + 1; j < Math.Min(i + 3, lines.Length); j++)
                    {
                        var nextLine = lines[j].Trim();
                        if (Regex.IsMatch(nextLine, @"^\d{8,12}$"))
                        {
                            results.Add(nextLine);
                            break;
                        }
                    }
                }
            }

            return results.Distinct().ToList();
        }
    }
}