using ImageOcrMicroservice.Models;

namespace ImageOcrMicroservice.Services
{
    /// <summary>
    /// Orchestrates OCR processing using Tesseract OCR service
    /// </summary>
    public class OcrOrchestrationService
    {
        private readonly OcrService _tesseractOcrService;
        private readonly DocumentTypeDetectionService _documentTypeDetectionService;
        private readonly ILogger<OcrOrchestrationService> _logger;

        public OcrOrchestrationService(
            OcrService tesseractOcrService,
            DocumentTypeDetectionService documentTypeDetectionService,
            ILogger<OcrOrchestrationService> logger)
        {
            _tesseractOcrService = tesseractOcrService;
            _documentTypeDetectionService = documentTypeDetectionService;
            _logger = logger;
        }

        public string ProcessDocumentAndExtractText(byte[] fileBytes, string fileName, bool isPdf)
        {
            // Detect document type from filename
            var documentType = _documentTypeDetectionService.DetectDocumentType(fileName);
            _logger.LogInformation("Detected document type: {DocumentType} for file: {FileName}", documentType, fileName);

            // Return error if document type is Unknown
            if (documentType == DocumentType.Unknown)
            {
                var errorMessage = $"Document type could not be determined for file: {fileName}. Supported document types are: CNI/Récépissé, Registre du Commerce, Carte Contribuable Valide, and Attestation Fiscale.";
                _logger.LogError(errorMessage);
                return $"Error: {errorMessage}";
            }

            // Use Tesseract OCR for all document types
            _logger.LogInformation("Using Tesseract OCR for document type: {DocumentType}", documentType);
            return ProcessWithTesseractOcr(fileBytes, isPdf);
        }

        public string ProcessDocumentWithSpecificType(byte[] fileBytes, string fileName, bool isPdf, DocumentType documentType)
        {
            _logger.LogInformation("Processing document with specified type: {DocumentType} for file: {FileName}", documentType, fileName);

            // Return error if document type is Unknown
            if (documentType == DocumentType.Unknown)
            {
                var errorMessage = $"Cannot process document with Unknown type for file: {fileName}. Supported document types are: CNI/Récépissé, Registre du Commerce, Carte Contribuable Valide, and Attestation Fiscale.";
                _logger.LogError(errorMessage);
                return $"Error: {errorMessage}";
            }

            // Use Tesseract OCR for all document types
            _logger.LogInformation("Using Tesseract OCR for specified document type: {DocumentType}", documentType);
            return ProcessWithTesseractOcr(fileBytes, isPdf);
        }

        private string ProcessWithTesseractOcr(byte[] fileBytes, bool isPdf)
        {
            try
            {
                _logger.LogInformation("Starting Tesseract OCR processing ({FileSize} bytes)", fileBytes.Length);
                
                return isPdf
                    ? _tesseractOcrService.ProcessPdfAndExtractText(fileBytes)
                    : _tesseractOcrService.ProcessImageAndExtractText(fileBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tesseract OCR processing failed.");
                return $"Error during Tesseract OCR: {ex.Message}";
            }
        }

        // Method to get OCR service recommendation for a document type
        public string GetRecommendedOcrService(DocumentType documentType)
        {
            return "Tesseract OCR";
        }
    }
}