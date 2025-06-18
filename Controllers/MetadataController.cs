using Microsoft.AspNetCore.Mvc;
using ImageOcrMicroservice.Services;
using ImageOcrMicroservice.Models;

namespace ImageOcrMicroservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MetadataController : ControllerBase
    {
        private readonly OcrService _ocrService;
        private readonly MetadataService _metadataService;
        private readonly ILogger<MetadataController> _logger;

        public MetadataController(
            OcrService ocrService, 
            MetadataService metadataService, 
            ILogger<MetadataController> logger)
        {
            _ocrService = ocrService;
            _metadataService = metadataService;
            _logger = logger;
        }

        [HttpPost("extract")]
        [ProducesResponseType(typeof(DocumentMetadata), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExtractMetadataFromFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("File is required.");
            if (file.Length > 20 * 1024 * 1024) return BadRequest("File size exceeds the limit (20MB).");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var isImage = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tiff" }.Contains(extension);
            var isPdf = extension == ".pdf";

            if (!isImage && !isPdf)
            {
                return BadRequest("Invalid file type. Allowed: PNG, JPG, JPEG, BMP, TIFF, PDF.");
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();
                string extractedText;

                // Step 1: Perform OCR to get the raw text (offloaded to a background thread)
                extractedText = await Task.Run(() =>
                {
                    _logger.LogInformation("Step 1: Starting OCR process for file '{FileName}'.", file.FileName);
                    return isPdf 
                        ? _ocrService.ProcessPdfAndExtractText(fileBytes) 
                        : _ocrService.ProcessImageAndExtractText(fileBytes);
                });

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    _logger.LogWarning("OCR process yielded no text for file '{FileName}'.", file.FileName);
                    return Ok(new DocumentMetadata { RawText = "OCR process yielded no text." });
                }

                // Step 2: Extract structured metadata from the raw text
                _logger.LogInformation("Step 2: Starting metadata extraction from OCR text.");
                var metadata = _metadataService.ExtractMetadata(extractedText);

                // Step 3: Return the structured metadata as a JSON object
                return Ok(metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the full extraction workflow for file '{FileName}'.", file.FileName);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }
    }
}
