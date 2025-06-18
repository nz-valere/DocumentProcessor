using Microsoft.AspNetCore.Mvc;
using ImageOcrMicroservice.Services;
using System.Text;

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
                string extractedText;

                // Offload the CPU-intensive OCR work to a background thread
                // This prevents blocking the web server's request thread.
                extractedText = await Task.Run(() =>
                {
                    if (isPdf)
                    {
                        _logger.LogInformation("Processing PDF file '{FileName}'.", file.FileName);
                        return _ocrService.ProcessPdfAndExtractText(fileBytes);
                    }
                    else // Is Image
                    {
                        _logger.LogInformation("Processing image file '{FileName}'.", file.FileName);
                        return _ocrService.ProcessImageAndExtractText(fileBytes);
                    }
                });
                
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
    }
}