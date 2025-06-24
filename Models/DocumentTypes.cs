namespace ImageOcrMicroservice.Models
{
    public enum DocumentType
    {
        // A document type that couldn't be identified.
        Unknown,
        
        // Specific, known document types.
        FormulaireAgregeOM,
        CniOrRecipice,
        RegistreCommerce,
        CarteContribuabledValide,
        AttestationFiscale
    }
}