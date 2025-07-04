using System.Text.RegularExpressions;
using ImageOcrMicroservice.Models;

namespace ImageOcrMicroservice.Services
{
    public class RegexService
    {
        private readonly ILogger<RegexService> _logger;

        public RegexService(ILogger<RegexService> logger)
        {
            _logger = logger;
        }



        #region Common Regex Patterns
        private static readonly Regex CommonDateRegex = new Regex(@"\b(\d{1,2}[/.-]\d{1,2}[/.-]\d{4})\b", RegexOptions.Compiled);
        private static readonly Regex CommonPhoneNumberRegex = new Regex(@"(?:Tél\s*fixe|Téléphone|Phone|Tel)\s*:?\s*(\d{9,15})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CommonEmailRegex = new Regex(@"(?:Adresse électronique|e[\-\s]*mail|Email)\s*\(?[:\s]*\)?\s*([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SimpleEmailRegex = new Regex(@"\b[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}\b", RegexOptions.Compiled);
        private static readonly Regex CommonRegistrationNumberRegex = new Regex(@"\b(\d{4}[A-Z]\d{8})\b", RegexOptions.Compiled);
        private static readonly Regex CommonAddressRegex = new Regex(@"ADRESSE DU SIEGE\s*:\s*(.*?)(?=\n|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CommonLegalFormRegex = new Regex(@"\b(SARL|SAS|SA|EURL|SNC|SCS|GIE|Société Anonyme|Société à Responsabilité Limitée)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CommonQuarterRegex = new Regex(@"(?:Lieu Dit|Quarter|Quartier)\s*([A-Z][A-Z\s-]+)(?:\s+B\.P\.?:?|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CommonActivityCodeRegex = new Regex(@"(?:Code APE|Activité principale|Code NAF)[\s:]*(\d{4}[A-Z]?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CommonDocumentLocationDateRegex = new Regex(@"\b([A-Z][A-Z\s-]+)\s+(\d{1,2}/\d{1,2}/\d{4})\b", RegexOptions.Compiled);
        #endregion



        #region FormulaireAgregeOM Specific Patterns
        private static readonly Regex AgregeBusinessNameRegex = new Regex(@"Nom commercial / Raison sociale du Point Agrégé\s*\n(.*?)(?=\n)", RegexOptions.Compiled);
        private static readonly Regex AgregeNiuRegex = new Regex(@"Numéro\s+Identifiant\s+Unique\s+\(NIU\)[^\n]*\n([A-Z0-9\sW]+\.)", RegexOptions.Compiled);
        private static readonly Regex AgregeAddressRegex = new Regex(@"Localisation de l'activité\*\s*\n(.*?)(?=\n)", RegexOptions.Compiled);
        private static readonly Regex AgregeActivityRegex = new Regex(@"Activité principale du Point accepteur agrégé'\s*\n(.*?)(?=\n)", RegexOptions.Compiled);
        private static readonly Regex AgregePhoneNumberRegex = new Regex(@"Numéro de téléphone personnel[^\n]*\n([0-9\s]+)", RegexOptions.Compiled);
        private static readonly Regex AgregePromoterNamesRegex = new Regex(@"(?:Nom et prénom|Promoteur|Gérant)\s*:\s*(.*?)(?=\n|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex AgregeMinDailyRevenueRegex = new Regex(@"(?:Chiffre d'affaires minimum|CA minimum|Min CA)\s*:\s*(\d+(?:\.\d{3})*)\s*(FCFA|F CFA)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex AgregeMaxDailyRevenueRegex = new Regex(@"(?:Chiffre d'affaires maximum|CA maximum|Max CA)\s*:\s*(\d+(?:\.\d{3})*)\s*(FCFA|F CFA)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        #endregion



        #region CniOrRecipice Specific Patterns
        private static readonly Regex CniNameRegex = new Regex(@"(?:Nom|Surname|NOM)\s*:\s*(.*?)(?=\n|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CniSurnameRegex = new Regex(@"(?:Prénom|Prénoms|Given Name|PRENOMS?)\s*:\s*(.*?)(?=\n|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CniBirthDateRegex = new Regex(@"(?:Né le|Date de naissance|Born on|Birth Date)\s*:\s*(\d{1,2}[/.-]\d{1,2}[/.-]\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CniProfessionRegex = new Regex(@"(?:Profession|Métier|Occupation)\s*:\s*(.*?)(?=\n|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CniRegistrationNumberRegex = new Regex(@"(?:N°|Numéro|Number)\s*:\s*(\d+[A-Z]?\d*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        #endregion



        #region RegistreCommerce Specific Patterns
        private static readonly Regex CommerceRccmRegex = new Regex(@"\b(RC[/\s]*[A-Z]{3,}[/\s]*\d{4}[/\s]*[A-Z][/\s]*\d{4})\b", RegexOptions.Compiled);
        private static readonly Regex CommerceBusinessNameRegex = new Regex(@"\b(SUKA SARL|ALLIANCE INFINIMENT|[A-Z][A-Z\s&-]+(?:SARL|SAS|SA|EURL|SNC|SCS|GIE))\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CommerceCapitalAmountRegex = new Regex(@"CAPITAL SOCIAL\s*:\s*(\d+(?:\.\d{3})*)\s*(FCFA|F CFA|€|EUR)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CommerceRegistrationDateRegex = new Regex(@"(?:Date d'immatriculation|Immatriculé le|Date d'inscription)[\s:]*(\d{1,2}[/.-]\d{1,2}[/.-]\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CommerceDeliveredDateRegex = new Regex(@"Déposée le\s*(\d{1,2}/\d{1,2}/\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CommerceDurationRegex = new Regex(@"Durée\s*:\s*(\d+\s*ANS?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CommerceTribunalRegex = new Regex(@"(?:Tribunal|Greffe)[\s:]*([A-Z][A-Z\s-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        #endregion



        #region CarteContribuabledValide Specific Patterns
        private static readonly Regex ContribuableNiuRegex = new Regex(@"\b([MP]\d{12}[A-Z])\b", RegexOptions.Compiled);
        private static readonly Regex ContribuableBusinessNameRegex = new Regex(@"(?:Dénomination|Raison sociale|Nom commercial)\s*:\s*(.*?)(?=\n|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ContribuableTaxAttestationRegex = new Regex(@"(?:N° Carte|Numéro Carte|Card Number)\s*:\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ContribuableTaxCenterRegex = new Regex(@"Centre des impôts de rattachement\s*:\s*(.*?)(?=\n|Tax center)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ContribuableTaxSystemRegex = new Regex(@"Régime fiscal\s*:\s*(.*?)(?=\n|Tax system)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ContribuableRegimeRegex = new Regex(@"REGIME\s*:\s*(.*?)(?=\n|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        #endregion



        #region AttestationFiscale Specific Patterns
        private static readonly Regex AttestationNiuRegex = new Regex(@"\b([MP]\d{12}[A-Z])\b", RegexOptions.Compiled);
        private static readonly Regex AttestationBusinessNameRegex = new Regex(@"(?:Dénomination|Raison sociale|Nom)\s*:\s*(.*?)(?=\n|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex AttestationAcfeRegex = new Regex(@"(?:Réference\s+ACFE|Reference\s+ACFE|ACFE\s*:?)\s*:?\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex AttestationRegimeRegex = new Regex(@"REGIME\s*:\s*(.*?)(?=\n|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex AttestationGenericNumberRegex = new Regex(@"N°\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        #endregion

        public Dictionary<string, List<string>> ExtractFieldsForDocumentType(string rawText, DocumentType documentType)
        {
            var extractedFields = new Dictionary<string, List<string>>();
            
            try
            {
                switch (documentType)
                {
                    case DocumentType.FormulaireAgregeOM:
                        extractedFields = ExtractFormulaireAgregeOMFields(rawText);
                        break;
                    case DocumentType.CniOrRecipice:
                        extractedFields = ExtractCniOrRecipiceFields(rawText);
                        break;
                    case DocumentType.RegistreCommerce:
                        extractedFields = ExtractRegistreCommerceFields(rawText);
                        break;
                    case DocumentType.CarteContribuabledValide:
                        extractedFields = ExtractCarteContribuabledValideFields(rawText);
                        break;
                    case DocumentType.AttestationFiscale:
                        extractedFields = ExtractAttestationFiscaleFields(rawText);
                        break;
                    default:
                        extractedFields = ExtractCommonFields(rawText);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting fields for document type {DocumentType}", documentType);
                extractedFields = new Dictionary<string, List<string>>();
            }

            return extractedFields;
        }

        private Dictionary<string, List<string>> ExtractFormulaireAgregeOMFields(string rawText)
        {
            var fields = new Dictionary<string, List<string>>();

            // Extract specific fields for FormulaireAgregeOM
            fields["BusinessNames"] = ExtractMatches(rawText, AgregeBusinessNameRegex);
            fields["PromoterNames"] = ExtractMatches(rawText, AgregePromoterNamesRegex);
            fields["RegistrationNumbers"] = ExtractMatches(rawText, CommonRegistrationNumberRegex);
            fields["CompanyAddresses"] = ExtractMatches(rawText, AgregeAddressRegex);
            fields["LegalForms"] = ExtractMatches(rawText, CommonLegalFormRegex);
            fields["ActivityCodes"] = ExtractMatches(rawText, AgregeActivityRegex);
            fields["PhoneNumbers"] = ExtractMatches(rawText, AgregePhoneNumberRegex);
            fields["MinDailyRevenue"] = ExtractCapitalAmounts(rawText, AgregeMinDailyRevenueRegex);
            fields["MaxDailyRevenue"] = ExtractCapitalAmounts(rawText, AgregeMaxDailyRevenueRegex);

            return fields;
        }

        private Dictionary<string, List<string>> ExtractCniOrRecipiceFields(string rawText)
        {
            var fields = new Dictionary<string, List<string>>();

            // Extract specific fields for CNI/Recipice
            fields["Name"] = ExtractMatches(rawText, CniNameRegex);
            fields["Surname"] = ExtractMatches(rawText, CniSurnameRegex);
            fields["RegistrationNumbers"] = ExtractMatches(rawText, CniRegistrationNumberRegex);
            fields["BirthDate"] = ExtractMatches(rawText, CniBirthDateRegex);
            fields["Profession"] = ExtractMatches(rawText, CniProfessionRegex);
            fields["DocumentLocationsAndDates"] = ExtractDocumentLocationsAndDates(rawText);

            return fields;
        }

        private Dictionary<string, List<string>> ExtractRegistreCommerceFields(string rawText)
        {
            var fields = new Dictionary<string, List<string>>();

            // Extract specific fields for Registre Commerce
            fields["RccmNumbers"] = ExtractMatches(rawText, CommerceRccmRegex);
            fields["BusinessNames"] = ExtractMatches(rawText, CommerceBusinessNameRegex);
            fields["CompanyAddresses"] = ExtractMatches(rawText, CommonAddressRegex);
            fields["LegalForms"] = ExtractMatches(rawText, CommonLegalFormRegex);
            fields["CapitalAmounts"] = ExtractCapitalAmounts(rawText, CommerceCapitalAmountRegex);
            fields["RegistrationDates"] = ExtractMatches(rawText, CommerceRegistrationDateRegex);
            fields["DeliveredDates"] = ExtractMatches(rawText, CommerceDeliveredDateRegex);
            fields["CompanyDuration"] = ExtractMatches(rawText, CommerceDurationRegex);
            fields["TribunalNames"] = ExtractMatches(rawText, CommerceTribunalRegex);
            fields["ActivityCodes"] = ExtractMatches(rawText, CommonActivityCodeRegex);
            fields["Quarters"] = ExtractMatches(rawText, CommonQuarterRegex);

            return fields;
        }

        private Dictionary<string, List<string>> ExtractCarteContribuabledValideFields(string rawText)
        {
            var fields = new Dictionary<string, List<string>>();

            // Extract specific fields for Carte Contribuable Valide
            fields["NiuNumbers"] = ExtractMatches(rawText, ContribuableNiuRegex);
            fields["BusinessNames"] = ExtractMatches(rawText, ContribuableBusinessNameRegex);
            fields["TaxAttestationNumbers"] = ExtractMatches(rawText, ContribuableTaxAttestationRegex);
            fields["TaxCenters"] = ExtractMatches(rawText, ContribuableTaxCenterRegex);
            fields["TaxSystems"] = ExtractMatches(rawText, ContribuableTaxSystemRegex);
            fields["Regimes"] = ExtractMatches(rawText, ContribuableRegimeRegex);

            return fields;
        }

        private Dictionary<string, List<string>> ExtractAttestationFiscaleFields(string rawText)
        {
            var fields = new Dictionary<string, List<string>>();

            // Extract specific fields for Attestation Fiscale
            fields["NiuNumbers"] = ExtractMatches(rawText, AttestationNiuRegex);
            fields["BusinessNames"] = ExtractMatches(rawText, AttestationBusinessNameRegex);
            fields["Dates"] = ExtractMatches(rawText, CommonDateRegex);
            fields["AcfeReferences"] = ExtractMatches(rawText, AttestationAcfeRegex);
            fields["CompanyAddresses"] = ExtractMatches(rawText, CommonAddressRegex);
            fields["Quarters"] = ExtractMatches(rawText, CommonQuarterRegex);
            fields["PhoneNumbers"] = ExtractMatches(rawText, CommonPhoneNumberRegex);
            fields["EmailAddresses"] = ExtractEmailAddresses(rawText);
            fields["Regimes"] = ExtractMatches(rawText, AttestationRegimeRegex);
            fields["DocumentLocationsAndDates"] = ExtractDocumentLocationsAndDates(rawText);

            // Also extract generic attestation numbers
            var genericNumbers = ExtractMatches(rawText, AttestationGenericNumberRegex);
            if (genericNumbers.Any())
            {
                if (!fields.ContainsKey("TaxAttestationNumbers"))
                    fields["TaxAttestationNumbers"] = new List<string>();
                fields["TaxAttestationNumbers"].AddRange(genericNumbers);
            }

            return fields;
        }

        private Dictionary<string, List<string>> ExtractCommonFields(string rawText)
        {
            var fields = new Dictionary<string, List<string>>();

            // Extract common fields that might appear in any document
            fields["Dates"] = ExtractMatches(rawText, CommonDateRegex);
            fields["RegistrationNumbers"] = ExtractMatches(rawText, CommonRegistrationNumberRegex);
            fields["CompanyAddresses"] = ExtractMatches(rawText, CommonAddressRegex);
            fields["LegalForms"] = ExtractMatches(rawText, CommonLegalFormRegex);
            fields["ActivityCodes"] = ExtractMatches(rawText, CommonActivityCodeRegex);
            fields["PhoneNumbers"] = ExtractMatches(rawText, CommonPhoneNumberRegex);
            fields["EmailAddresses"] = ExtractEmailAddresses(rawText);
            fields["Quarters"] = ExtractMatches(rawText, CommonQuarterRegex);
            fields["DocumentLocationsAndDates"] = ExtractDocumentLocationsAndDates(rawText);

            return fields;
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

        private List<string> ExtractCapitalAmounts(string text, Regex regex)
        {
            var amounts = new List<string>();
            
            try
            {
                var matches = regex.Matches(text);
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

        private List<string> ExtractDocumentLocationsAndDates(string text)
        {
            var locationsAndDates = new List<string>();
            
            try
            {
                var matches = CommonDocumentLocationDateRegex.Matches(text);
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
                var matches = CommonEmailRegex.Matches(text);
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
                var simpleMatches = SimpleEmailRegex.Matches(text);
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

        public bool IsFieldValidForDocumentType(string fieldName, DocumentType documentType)
        {
            return DocumentTypeFieldMapping.GetFieldsForDocumentType(documentType).Contains(fieldName);
        }
    }
}