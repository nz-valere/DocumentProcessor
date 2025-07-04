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
                    "PromoterNames",
                    "RegistrationNumbers",
                    "CompanyAddresses",
                    "LegalForms",
                    "ActivityCodes",
                    "PhoneNumbers",
                    "MinDailyRevenue",
                    "MaxDailyRevenue",
                    "RawText"
                }
            },
            {
                DocumentType.CniOrRecipice,
                new HashSet<string>
                {
                    "DocumentName",
                    "DocumentType",
                    "Name",
                    "Surname",
                    "RegistrationNumbers",
                    "BirthDate",
                    "Profession",
                    "DocumentLocationsAndDates",
                    "RawText"
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
                    "CompanyAddresses",
                    "LegalForms",
                    "CapitalAmounts",
                    "RegistrationDates",
                    "DeliveredDates",
                    "CompanyDuration",
                    "TribunalNames",
                    "ActivityCodes",
                    "Quarters",
                    "RawText"
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
                    "Regimes",
                    "RawText"
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
                    "Dates",
                    "AcfeReferences",
                    "CompanyAddresses",
                    "Quarters",
                    "PhoneNumbers",
                    "EmailAddresses",
                    "Regimes",
                    "DocumentLocationsAndDates",
                    "RawText"
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
                "PromoterNames",
                "Name",
                "Surname",
                "BirthDate",
                "Profession",
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
                "MinDailyRevenue",
                "MaxDailyRevenue",
                "RawText"
            };
        }
    }
}