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
            if (file == null || file.Length == 0) 
                return BadRequest("File is required.");
            
            if (file.Length > 20 * 1024 * 1024) 
                return BadRequest("File size exceeds the limit (20MB).");

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
                    return Ok(new DocumentMetadata 
                    { 
                        DocumentName = Path.GetFileNameWithoutExtension(file.FileName),
                        DocumentType = "Unknown",
                        RawText = "OCR process yielded no text." 
                    });
                }

                // Step 2: Extract structured metadata from the raw text (including file name and new tax information)
                _logger.LogInformation("Step 2: Starting metadata extraction from OCR text for file '{FileName}'.", file.FileName);
                var metadata = _metadataService.ExtractMetadata(extractedText, file.FileName);

                // Log tax-related and additional extractions for debugging
                if (metadata.TaxAttestationNumbers.Any() || metadata.TaxCenters.Any() || metadata.TaxSystems.Any() ||
                    metadata.AcfeReferences.Any() || metadata.DocumentLocationsAndDates.Any() || metadata.Quarters.Any() ||
                    metadata.PhoneNumbers.Any() || metadata.EmailAddresses.Any() || metadata.Regimes.Any())
                {
                    _logger.LogInformation("Extended information extracted: {TaxAttestationCount} attestation number(s), {TaxCenterCount} tax center(s), {TaxSystemCount} tax system(s), {AcfeCount} ACFE reference(s), {LocationDateCount} location/date(s), {QuarterCount} quarter(s), {PhoneCount} phone number(s), {EmailCount} email(s), {RegimeCount} regime(s) for file '{FileName}'.",
                        metadata.TaxAttestationNumbers.Count, metadata.TaxCenters.Count, metadata.TaxSystems.Count,
                        metadata.AcfeReferences.Count, metadata.DocumentLocationsAndDates.Count, metadata.Quarters.Count,
                        metadata.PhoneNumbers.Count, metadata.EmailAddresses.Count, metadata.Regimes.Count, file.FileName);
                }

                // Step 3: Return the structured metadata as a JSON object
                _logger.LogInformation("Step 3: Returning structured metadata for file '{FileName}'.", file.FileName);
                return Ok(metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the full extraction workflow for file '{FileName}'.", file.FileName);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("extract-batch")]
        [ProducesResponseType(typeof(List<DocumentMetadata>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExtractMetadataFromMultipleFiles(List<IFormFile> files)
        {
            if (files == null || !files.Any())
                return BadRequest("At least one file is required.");

            if (files.Count > 10)
                return BadRequest("Maximum 10 files allowed per batch.");

            var results = new List<DocumentMetadata>();
            var tasks = new List<Task<DocumentMetadata>>();

            foreach (var file in files)
            {
                if (file.Length > 20 * 1024 * 1024)
                {
                    _logger.LogWarning("File '{FileName}' exceeds size limit and will be skipped.", file.FileName);
                    continue;
                }

                tasks.Add(ProcessSingleFileAsync(file));
            }

            try
            {
                var processedResults = await Task.WhenAll(tasks);
                results.AddRange(processedResults.Where(r => r != null));

                // Log summary of all extracted information across all files
                var totalTaxAttestations = results.Sum(r => r.TaxAttestationNumbers.Count);
                var totalTaxCenters = results.Sum(r => r.TaxCenters.Count);
                var totalTaxSystems = results.Sum(r => r.TaxSystems.Count);
                var totalAcfeReferences = results.Sum(r => r.AcfeReferences.Count);
                var totalLocationDates = results.Sum(r => r.DocumentLocationsAndDates.Count);
                var totalQuarters = results.Sum(r => r.Quarters.Count);
                var totalPhones = results.Sum(r => r.PhoneNumbers.Count);
                var totalEmails = results.Sum(r => r.EmailAddresses.Count);
                var totalRegimes = results.Sum(r => r.Regimes.Count);

                _logger.LogInformation("Batch processing completed. Processed {ProcessedCount} out of {TotalCount} files. Extended info extracted: {TaxAttestationCount} attestations, {TaxCenterCount} tax centers, {TaxSystemCount} tax systems, {AcfeCount} ACFE references, {LocationDateCount} location/dates, {QuarterCount} quarters, {PhoneCount} phones, {EmailCount} emails, {RegimeCount} regimes.", 
                    results.Count, files.Count, totalTaxAttestations, totalTaxCenters, totalTaxSystems,
                    totalAcfeReferences, totalLocationDates, totalQuarters, totalPhones, totalEmails, totalRegimes);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during batch processing.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Batch processing error: {ex.Message}");
            }
        }

        private async Task<DocumentMetadata> ProcessSingleFileAsync(IFormFile file)
        {
            try
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var isImage = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tiff" }.Contains(extension);
                var isPdf = extension == ".pdf";

                if (!isImage && !isPdf)
                {
                    _logger.LogWarning("Invalid file type for '{FileName}'. Skipping.", file.FileName);
                    return new DocumentMetadata 
                    { 
                        DocumentName = Path.GetFileNameWithoutExtension(file.FileName),
                        DocumentType = "Invalid",
                        RawText = "Invalid file type" 
                    };
                }

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var extractedText = await Task.Run(() =>
                {
                    return isPdf 
                        ? _ocrService.ProcessPdfAndExtractText(fileBytes) 
                        : _ocrService.ProcessImageAndExtractText(fileBytes);
                });

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return new DocumentMetadata 
                    { 
                        DocumentName = Path.GetFileNameWithoutExtension(file.FileName),
                        DocumentType = "Empty",
                        RawText = "OCR process yielded no text." 
                    };
                }

                return _metadataService.ExtractMetadata(extractedText, file.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file '{FileName}'.", file.FileName);
                return new DocumentMetadata 
                { 
                    DocumentName = Path.GetFileNameWithoutExtension(file.FileName),
                    DocumentType = "Error",
                    RawText = $"Processing error: {ex.Message}" 
                };
            }
        }
    }
}