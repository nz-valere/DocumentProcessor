using Microsoft.AspNetCore.Mvc;
using ImageOcrMicroservice.Services;
using System.Text; // Required for Encoding

namespace ImageOcrMicroservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OcrController : ControllerBase
    {
        private readonly OcrService _ocrService;
        private readonly ILogger<OcrController> _logger;

        public OcrController(OcrService ocrService, ILogger<OcrController> logger)
        {
            _ocrService = ocrService;
            _logger = logger;
        }

        [HttpPost("extractText")]
        // Updated ProducesResponseType for file download
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExtractTextFromFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is required.");
            }

            if (file.Length > 20 * 1024 * 1024) // 20MB limit
            {
                return BadRequest("File size exceeds the limit (20MB).");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedImageExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tiff" };
            var allowedPdfExtensions = new[] { ".pdf" };

            bool isImage = allowedImageExtensions.Contains(extension);
            bool isPdf = allowedPdfExtensions.Contains(extension);

            if (string.IsNullOrEmpty(extension) || (!isImage && !isPdf))
            {
                return BadRequest("Invalid file type. Allowed types: PNG, JPG, JPEG, BMP, TIFF, PDF.");
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();
                string extractedText;

                if (isPdf)
                {
                    _logger.LogInformation("Processing PDF file '{FileName}'.", file.FileName);
                    extractedText = _ocrService.ProcessPdfAndExtractText(fileBytes);
                }
                else // Is Image
                {
                    _logger.LogInformation("Processing image file '{FileName}'.", file.FileName);
                    extractedText = _ocrService.ProcessImageAndExtractText(fileBytes);
                }
                
                _logger.LogInformation("Successfully extracted text from file '{FileName}'.", file.FileName);

                // Convert the extracted text string to a byte array
                var textBytes = Encoding.UTF8.GetBytes(extractedText);
                
                // Generate a filename for the downloaded text file
                var outputFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_extracted_text.txt";

                // Return the text as a downloadable .txt file
                return File(textBytes, "text/plain", outputFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file '{FileName}'.", file.FileName);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }
    }
}