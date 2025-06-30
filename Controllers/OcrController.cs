using Microsoft.AspNetCore.Mvc;
using ImageOcrMicroservice.Services;
using System.Text;

namespace ImageOcrMicroservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OcrController : ControllerBase
    {
        private readonly OcrOrchestrationService _ocrOrchestrationService;
        private readonly ILogger<OcrController> _logger;

        public OcrController(OcrOrchestrationService ocrOrchestrationService, ILogger<OcrController> logger)
        {
            _ocrOrchestrationService = ocrOrchestrationService;
            _logger = logger;
        }

        [HttpPost("extractText")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExtractTextFromFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("File is required.");
            if (file.Length > 20 * 1024 * 1024) return BadRequest("File size exceeds the limit (20MB).");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedImageExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tiff" };
            var isImage = allowedImageExtensions.Contains(extension);
            var isPdf = extension == ".pdf";

            if (!isImage && !isPdf)
            {
                return BadRequest("Invalid file type. Allowed types: PNG, JPG, JPEG, BMP, TIFF or PDF.");
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();
                
                _logger.LogInformation("Processing file '{FileName}' using intelligent OCR selection.", file.FileName);

                // Use the orchestration service to intelligently choose OCR method
                string extractedText = await _ocrOrchestrationService.ProcessDocumentAndExtractTextAsync(
                    fileBytes, file.FileName, isPdf);
                
                _logger.LogInformation("Successfully extracted text from file '{FileName}'.", file.FileName);
                var textBytes = Encoding.UTF8.GetBytes(extractedText);
                var outputFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_extracted_text.txt";
                return File(textBytes, "text/plain", outputFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file '{FileName}'.", file.FileName);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("extractTextWithType")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExtractTextFromFileWithDocumentType(IFormFile file, [FromQuery] string documentType)
        {
            if (file == null || file.Length == 0) return BadRequest("File is required.");
            if (file.Length > 20 * 1024 * 1024) return BadRequest("File size exceeds the limit (20MB).");
            if (string.IsNullOrWhiteSpace(documentType)) return BadRequest("Document type is required.");

            if (!Enum.TryParse<ImageOcrMicroservice.Models.DocumentType>(documentType, true, out var docType))
            {
                return BadRequest("Invalid document type specified.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedImageExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tiff" };
            var isImage = allowedImageExtensions.Contains(extension);
            var isPdf = extension == ".pdf";

            if (!isImage && !isPdf)
            {
                return BadRequest("Invalid file type. Allowed types: PNG, JPG, JPEG, BMP, TIFF or PDF.");
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();
                
                _logger.LogInformation("Processing file '{FileName}' with specified document type '{DocumentType}'.", 
                    file.FileName, docType);

                // Use the orchestration service with specific document type
                string extractedText = await _ocrOrchestrationService.ProcessDocumentWithSpecificTypeAsync(
                    fileBytes, file.FileName, isPdf, docType);
                
                _logger.LogInformation("Successfully extracted text from file '{FileName}' using {OCRService}.", 
                    file.FileName, _ocrOrchestrationService.GetRecommendedOcrService(docType));
                
                var textBytes = Encoding.UTF8.GetBytes(extractedText);
                var outputFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_extracted_text.txt";
                return File(textBytes, "text/plain", outputFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file '{FileName}' with document type '{DocumentType}'.", 
                    file.FileName, docType);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }
    }
}