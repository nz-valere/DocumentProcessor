using ImageOcrMicroservice.Models;

namespace ImageOcrMicroservice.Services
{
    /// <summary>
    /// Orchestrates between different OCR services based on document type
    /// </summary>
    public class OcrOrchestrationService
    {
        private readonly OcrService _tesseractOcrService;
        private readonly AzureOcrService _azureOcrService;
        private readonly DocumentTypeDetectionService _documentTypeDetectionService;
        private readonly ILogger<OcrOrchestrationService> _logger;

        // Document types that require handwritten OCR (Azure)
        private static readonly HashSet<DocumentType> HandwrittenDocumentTypes = new()
        {
            DocumentType.FormulaireAgregeOM,
            DocumentType.Unknown // Fallback to Azure for unknown types
        };

        public OcrOrchestrationService(
            OcrService tesseractOcrService,
            AzureOcrService azureOcrService,
            DocumentTypeDetectionService documentTypeDetectionService,
            ILogger<OcrOrchestrationService> logger)
        {
            _tesseractOcrService = tesseractOcrService;
            _azureOcrService = azureOcrService;
            _documentTypeDetectionService = documentTypeDetectionService;
            _logger = logger;
        }

        public async Task<string> ProcessDocumentAndExtractTextAsync(byte[] fileBytes, string fileName, bool isPdf)
        {
            // Detect document type from filename
            var documentType = _documentTypeDetectionService.DetectDocumentType(fileName);
            _logger.LogInformation("Detected document type: {DocumentType} for file: {FileName}", documentType, fileName);

            // Determine which OCR service to use
            var useAzureOcr = ShouldUseAzureOcr(documentType);
            _logger.LogInformation("Using {OcrService} for document type: {DocumentType}", 
                useAzureOcr ? "Azure OCR" : "Tesseract OCR", documentType);

            if (useAzureOcr)
            {
                return await ProcessWithAzureOcrAsync(fileBytes, fileName);
            }
            else
            {
                return ProcessWithTesseractOcr(fileBytes, isPdf);
            }
        }

        public async Task<string> ProcessDocumentWithSpecificTypeAsync(byte[] fileBytes, string fileName, bool isPdf, DocumentType documentType)
        {
            _logger.LogInformation("Processing document with specified type: {DocumentType} for file: {FileName}", documentType, fileName);

            var useAzureOcr = ShouldUseAzureOcr(documentType);
            _logger.LogInformation("Using {OcrService} for specified document type: {DocumentType}", 
                useAzureOcr ? "Azure OCR" : "Tesseract OCR", documentType);

            if (useAzureOcr)
            {
                return await ProcessWithAzureOcrAsync(fileBytes, fileName);
            }
            else
            {
                return ProcessWithTesseractOcr(fileBytes, isPdf);
            }
        }

        private bool ShouldUseAzureOcr(DocumentType documentType)
        {
            var shouldUse = HandwrittenDocumentTypes.Contains(documentType);
            _logger.LogDebug("Document type {DocumentType} requires Azure OCR: {RequiresAzure}", documentType, shouldUse);
            return shouldUse;
        }

        private async Task<string> ProcessWithAzureOcrAsync(byte[] fileBytes, string fileName)
        {
            try
            {
                _logger.LogInformation("Starting Azure OCR processing for file: {FileName} ({FileSize} bytes)", fileName, fileBytes.Length);
                
                using var memoryStream = new MemoryStream(fileBytes);
                var extractedText = await _azureOcrService.ExtractTextAsync(memoryStream);
                
                if (string.IsNullOrWhiteSpace(extractedText) || extractedText.StartsWith("Error during Azure OCR"))
                {
                    _logger.LogWarning("Azure OCR failed or returned no text for {FileName}. Falling back to Tesseract.", fileName);
                    
                    // Fallback to Tesseract if Azure fails
                    var isPdf = Path.GetExtension(fileName).ToLowerInvariant() == ".pdf";
                    return ProcessWithTesseractOcr(fileBytes, isPdf);
                }

                _logger.LogInformation("Azure OCR completed successfully for {FileName}. Text length: {TextLength} characters", 
                    fileName, extractedText.Trim().Length);
                
                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure OCR processing failed for {FileName}. Falling back to Tesseract.", fileName);
                
                // Fallback to Tesseract if Azure throws an exception
                var isPdf = Path.GetExtension(fileName).ToLowerInvariant() == ".pdf";
                return ProcessWithTesseractOcr(fileBytes, isPdf);
            }
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
            return ShouldUseAzureOcr(documentType) ? "Azure Document Analysis" : "Tesseract OCR";
        }

        // Method to add new handwritten document types
        public static void AddHandwrittenDocumentType(DocumentType documentType)
        {
            HandwrittenDocumentTypes.Add(documentType);
        }

        // Method to check if a document type uses handwritten OCR
        public static bool IsHandwrittenDocumentType(DocumentType documentType)
        {
            return HandwrittenDocumentTypes.Contains(documentType);
        }
    }
}