using ImageOcrMicroservice.Models;

namespace ImageOcrMicroservice.Models
{
    public class DocumentTypeFieldMapping
    {
        public static readonly Dictionary<DocumentType, HashSet<string>> FieldMappings = new()
        {
            {
                DocumentType.FormulaireAgregeOM,
                new HashSet<string>
                {
                    "DocumentName",
                    "DocumentType",
                    "BusinessNames",
                    "RegistrationNumbers",
                    "CompanyAddresses",
                    "LegalForms",
                    "CapitalAmounts",
                    "ActivityCodes",
                    "PhoneNumbers",
                    "EmailAddresses",
                    // "RawText"
                }
            },
            {
                DocumentType.CniOrRecipice,
                new HashSet<string>
                {
                    "DocumentName",
                    "DocumentType",
                    "RegistrationNumbers",
                    "Dates",
                    "DocumentLocationsAndDates",
                    // "RawText"
                }
            },
            {
                DocumentType.RegistreCommerce,
                new HashSet<string>
                {
                    "DocumentName",
                    "DocumentType",
                    "RccmNumbers",
                    "BusinessNames",
                    "RegistrationNumbers",
                    "CompanyAddresses",
                    "LegalForms",
                    "CapitalAmounts",
                    "RegistrationDates",
                    "DeliveredDates",
                    "CompanyDuration",
                    "TribunalNames",
                    "ActivityCodes",
                    "Quarters",
                    "PhoneNumbers",
                    "EmailAddresses",
                    // "RawText"
                }
            },
            {
                DocumentType.CarteContribuabledValide,
                new HashSet<string>
                {
                    "DocumentName",
                    "DocumentType",
                    "NiuNumbers",
                    "BusinessNames",
                    "TaxAttestationNumbers",
                    "TaxCenters",
                    "TaxSystems",
                    "CompanyAddresses",
                    "Quarters",
                    "PhoneNumbers",
                    "EmailAddresses",
                    "Regimes",
                    // "RawText"
                }
            },
            {
                DocumentType.AttestationFiscale,
                new HashSet<string>
                {
                    "DocumentName",
                    "DocumentType",
                    "NiuNumbers",
                    "BusinessNames",
                    "TaxAttestationNumbers",
                    "TaxCenters",
                    "TaxSystems",
                    "AcfeReferences",
                    "CompanyAddresses",
                    "Quarters",
                    "PhoneNumbers",
                    "EmailAddresses",
                    "Regimes",
                    "DocumentLocationsAndDates",
                    // "RawText"
                }
            }
        };

        public static HashSet<string> GetFieldsForDocumentType(DocumentType documentType)
        {
            return FieldMappings.TryGetValue(documentType, out var fields) 
                ? fields 
                : GetAllFields();
        }

        public static HashSet<string> GetAllFields()
        {
            return new HashSet<string>
            {
                "DocumentName",
                "DocumentType",
                "NiuNumbers",
                "RccmNumbers",
                "BusinessNames",
                "Dates",
                "RegistrationNumbers",
                "CompanyAddresses",
                "LegalForms",
                "CapitalAmounts",
                "RegistrationDates",
                "DeliveredDates",
                "CompanyDuration",
                "TribunalNames",
                "ActivityCodes",
                "TaxAttestationNumbers",
                "TaxCenters",
                "TaxSystems",
                "AcfeReferences",
                "DocumentLocationsAndDates",
                "Quarters",
                "PhoneNumbers",
                "EmailAddresses",
                "Regimes",
                "RawText"
            };
        }
    }
}