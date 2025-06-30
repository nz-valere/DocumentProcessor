using Microsoft.AspNetCore.Mvc;
using ImageOcrMicroservice.Services;
using ImageOcrMicroservice.Models;

namespace ImageOcrMicroservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MetadataController : ControllerBase
    {
        private readonly OcrOrchestrationService _ocrOrchestrationService;
        private readonly MetadataService _metadataService;
        private readonly DocumentSpecificMetadataService _documentSpecificMetadataService;
        private readonly DocumentTypeDetectionService _documentTypeDetectionService;
        private readonly ILogger<MetadataController> _logger;

        public MetadataController(
            OcrOrchestrationService ocrOrchestrationService,
            MetadataService metadataService,
            DocumentSpecificMetadataService documentSpecificMetadataService,
            DocumentTypeDetectionService documentTypeDetectionService,
            ILogger<MetadataController> logger)
        {
            _ocrOrchestrationService = ocrOrchestrationService;
            _metadataService = metadataService;
            _documentSpecificMetadataService = documentSpecificMetadataService;
            _documentTypeDetectionService = documentTypeDetectionService;
            _logger = logger;
        }

        [HttpPost("extract")]
        [ProducesResponseType(typeof(DocumentMetadata), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExtractMetadataFromFile(IFormFile file, [FromQuery] string? documentType = null)
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
                
                _logger.LogInformation("Step 1: Starting intelligent OCR process for file '{FileName}'.", file.FileName);
                
                string extractedText;
                
                if (!string.IsNullOrWhiteSpace(documentType) && Enum.TryParse<DocumentType>(documentType, true, out var specifiedDocType))
                {
                    // Use specified document type for OCR selection
                    extractedText = await _ocrOrchestrationService.ProcessDocumentWithSpecificTypeAsync(
                        fileBytes, file.FileName, isPdf, specifiedDocType);
                    _logger.LogInformation("Used specified document type '{DocumentType}' for OCR selection.", specifiedDocType);
                }
                else
                {
                    // Use automatic document type detection for OCR selection
                    extractedText = await _ocrOrchestrationService.ProcessDocumentAndExtractTextAsync(
                        fileBytes, file.FileName, isPdf);
                }

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

                _logger.LogInformation("Step 2: Starting metadata extraction from OCR text for file '{FileName}'.", file.FileName);
                DocumentMetadata metadata;

                if (!string.IsNullOrWhiteSpace(documentType) && Enum.TryParse<DocumentType>(documentType, true, out var specifiedDocType2))
                {
                    // Use specified document type for targeted extraction
                    metadata = _metadataService.ExtractMetadataForDocumentType(extractedText, specifiedDocType2, file.FileName);
                    _logger.LogInformation("Used specified document type '{DocumentType}' for extraction.", specifiedDocType2);
                }
                else
                {
                    // Use automatic detection and extraction
                    metadata = _metadataService.ExtractMetadata(extractedText, file.FileName);
                }

                // Get extraction statistics and validation results
                var extractionStats = _metadataService.GetExtractionStatistics(metadata);

                // Fix the document type parsing
                var detectedTypeEnum = ParseDocumentTypeFromDisplayName(metadata.DocumentType);

                var validationResult = _documentSpecificMetadataService.ValidateDocumentMetadata(metadata, detectedTypeEnum);

                LogExtractionResults(metadata, extractionStats, validationResult, file.FileName, detectedTypeEnum);

                // Create filtered metadata object - this is the key fix
                var filteredMetadata = CreateFilteredMetadataObject(metadata, detectedTypeEnum);

                // Return the structured metadata as a JSON object
                _logger.LogInformation("Step 4: Returning structured metadata for file '{FileName}'.", file.FileName);

                return Ok(new
                {
                    Metadata = filteredMetadata, 
                    ExtractionStatistics = extractionStats,
                    ValidationResult = new
                    {
                        validationResult.IsValid,
                        validationResult.Messages
                    },
                    OcrServiceUsed = _ocrOrchestrationService.GetRecommendedOcrService(detectedTypeEnum)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the full extraction workflow for file '{FileName}'.", file.FileName);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("extract-batch")]
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExtractMetadataFromMultipleFiles(List<IFormFile> files, [FromQuery] string? documentType = null)
        {
            if (files == null || !files.Any())
                return BadRequest("At least one file is required.");

            if (files.Count > 10)
                return BadRequest("Maximum 10 files allowed per batch.");

            var results = new List<object>();
            var tasks = new List<Task<object>>();

            DocumentType? specifiedDocType = null;
            if (!string.IsNullOrWhiteSpace(documentType) && Enum.TryParse<DocumentType>(documentType, true, out var parsedDocType))
            {
                specifiedDocType = parsedDocType;
                _logger.LogInformation("Using specified document type '{DocumentType}' for batch processing.", specifiedDocType);
            }

            foreach (var file in files)
            {
                if (file.Length > 20 * 1024 * 1024)
                {
                    _logger.LogWarning("File '{FileName}' exceeds size limit and will be skipped.", file.FileName);
                    continue;
                }

                tasks.Add(ProcessSingleFileAsync(file, specifiedDocType));
            }

            try
            {
                var processedResults = await Task.WhenAll(tasks);
                results.AddRange(processedResults.Where(r => r != null));

                // Log comprehensive batch summary
                LogBatchSummary(results, files.Count);

                return Ok(new
                {
                    Results = results,
                    Summary = new
                    {
                        TotalFilesSubmitted = files.Count,
                        FilesProcessed = results.Count,
                        FilesSkipped = files.Count - results.Count,
                        ProcessingDate = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during batch processing.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Batch processing error: {ex.Message}");
            }
        }

        [HttpGet("document-types")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult GetSupportedDocumentTypes()
        {
            try
            {
                var supportedTypes = DocumentTypeDetectionService.GetSupportedDocumentTypes()
                    .Select(type => new
                    {
                        Type = type.ToString(),
                        DisplayName = _documentTypeDetectionService.GetDocumentTypeDisplayName(type),
                        Patterns = DocumentTypeDetectionService.GetPatternsForDocumentType(type),
                        RecommendedOcrService = _ocrOrchestrationService.GetRecommendedOcrService(type),
                        IsHandwritten = OcrOrchestrationService.IsHandwrittenDocumentType(type)
                    })
                    .ToList();

                return Ok(new
                {
                    SupportedDocumentTypes = supportedTypes,
                    TotalTypes = supportedTypes.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supported document types.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving document types.");
            }
        }

        private async Task<object> ProcessSingleFileAsync(IFormFile file, DocumentType? specifiedDocumentType = null)
        {
            try
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var isImage = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tiff" }.Contains(extension);
                var isPdf = extension == ".pdf";

                if (!isImage && !isPdf)
                {
                    _logger.LogWarning("Invalid file type for '{FileName}'. Skipping.", file.FileName);
                    return CreateErrorResult(file.FileName, "Invalid file type", "Invalid");
                }

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                string extractedText;
                
                if (specifiedDocumentType.HasValue)
                {
                    extractedText = await _ocrOrchestrationService.ProcessDocumentWithSpecificTypeAsync(
                        fileBytes, file.FileName, isPdf, specifiedDocumentType.Value);
                }
                else
                {
                    extractedText = await _ocrOrchestrationService.ProcessDocumentAndExtractTextAsync(
                        fileBytes, file.FileName, isPdf);
                }

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return CreateErrorResult(file.FileName, "OCR process yielded no text.", "Empty");
                }

                DocumentMetadata metadata;
                if (specifiedDocumentType.HasValue)
                {
                    metadata = _metadataService.ExtractMetadataForDocumentType(extractedText, specifiedDocumentType.Value, file.FileName);
                }
                else
                {
                    metadata = _metadataService.ExtractMetadata(extractedText, file.FileName);
                }

                var extractionStats = _metadataService.GetExtractionStatistics(metadata);
                var detectedTypeEnum = ParseDocumentTypeFromDisplayName(metadata.DocumentType);
                var filteredMetadata = CreateFilteredMetadataObject(metadata, detectedTypeEnum);

                var validationResult = _documentSpecificMetadataService.ValidateDocumentMetadata(metadata, detectedTypeEnum);

                return new
                {
                    FileName = file.FileName,
                    Metadata = filteredMetadata,
                    ExtractionStatistics = extractionStats,
                    ValidationResult = new
                    {
                        validationResult.IsValid,
                        validationResult.Messages
                    },
                    OcrServiceUsed = _ocrOrchestrationService.GetRecommendedOcrService(detectedTypeEnum),
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file '{FileName}'.", file.FileName);
                return CreateErrorResult(file.FileName, $"Processing error: {ex.Message}", "Error");
            }
        }

        private object CreateErrorResult(string fileName, string errorMessage, string documentType)
        {
            return new
            {
                FileName = fileName,
                Metadata = new DocumentMetadata
                {
                    DocumentName = Path.GetFileNameWithoutExtension(fileName),
                    DocumentType = documentType,
                    RawText = errorMessage
                },
                ExtractionStatistics = new Dictionary<string, object>
                {
                    ["TotalFieldsExtracted"] = 0,
                    ["HasRawText"] = true
                },
                ValidationResult = new
                {
                    IsValid = false,
                    Messages = new List<string> { errorMessage }
                },
                OcrServiceUsed = "None",
                ProcessedAt = DateTime.UtcNow
            };
        }

        private void LogExtractionResults(DocumentMetadata metadata, Dictionary<string, object> stats, ValidationResult validation, string fileName, DocumentType docType)
        {
            _logger.LogInformation(
                "Metadata extraction completed for '{FileName}'. " +
                "Document Type: {DocumentType}, " +
                "OCR Service Used: {OcrService}, " +
                "Total Fields Extracted: {TotalFields}, " +
                "NIU Numbers: {NiuCount}, " +
                "RCCM Numbers: {RccmCount}, " +
                "Business Names: {BusinessCount}, " +
                "Tax Attestations: {TaxCount}, " +
                "Phone Numbers: {PhoneCount}, " +
                "Email Addresses: {EmailCount}, " +
                "Validation Valid: {IsValid}",
                fileName,
                metadata.DocumentType,
                _ocrOrchestrationService.GetRecommendedOcrService(docType),
                stats["TotalFieldsExtracted"],
                stats["NiuNumbersCount"],
                stats["RccmNumbersCount"],
                stats["BusinessNamesCount"],
                stats["TaxAttestationNumbersCount"],
                stats["PhoneNumbersCount"],
                stats["EmailAddressesCount"],
                validation.IsValid);

            if (!validation.IsValid)
            {
                _logger.LogWarning("Validation issues for '{FileName}': {ValidationMessages}",
                    fileName, string.Join(", ", validation.Messages));
            }
        }

        private void LogBatchSummary(List<object> results, int totalFiles)
        {
            var successfulResults = results.Where(r =>
            {
                var resultDict = r.GetType().GetProperty("ValidationResult")?.GetValue(r) as dynamic;
                return resultDict?.IsValid == true;
            }).Count();

            _logger.LogInformation(
                "Batch processing completed. " +
                "Total Files: {TotalFiles}, " +
                "Processed: {ProcessedCount}, " +
                "Successful: {SuccessfulCount}, " +
                "Failed/Skipped: {FailedCount}",
                totalFiles,
                results.Count,
                successfulResults,
                totalFiles - successfulResults);
        }

        private Dictionary<string, object> CreateFilteredMetadataObject(DocumentMetadata metadata, DocumentType docType)
        {
            // Get the set of allowed field names for the given document type
            var allowedFields = DocumentTypeFieldMapping.GetFieldsForDocumentType(docType);
            var filteredMetadata = new Dictionary<string, object>();

            var properties = typeof(DocumentMetadata).GetProperties();

            foreach (var property in properties)
            {
                if (allowedFields.Contains(property.Name))
                {
                    filteredMetadata[property.Name] = property.GetValue(metadata);
                }
            }

            return filteredMetadata;
        }

        private DocumentType ParseDocumentTypeFromDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return DocumentType.Unknown;

            var displayNameToEnum = new Dictionary<string, DocumentType>(StringComparer.OrdinalIgnoreCase)
            {
                { "Formulaire Agrégé OM", DocumentType.FormulaireAgregeOM },
                { "CNI ou Récépissé", DocumentType.CniOrRecipice },
                { "Registre du Commerce", DocumentType.RegistreCommerce },
                { "Carte Contribuable Valide", DocumentType.CarteContribuabledValide },
                { "Attestation Fiscale", DocumentType.AttestationFiscale }
            };

            return displayNameToEnum.TryGetValue(displayName, out var docType) 
                ? docType 
                : DocumentType.Unknown;
        }
    }
}