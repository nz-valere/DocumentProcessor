using Azure;
using Azure.AI.Vision.ImageAnalysis;
using System.Text;
using ImageMagick;
namespace ImageOcrMicroservice.Services
{
    /// <summary>
    /// Service to interact with Azure's new AI Vision Image Analysis service for advanced OCR.
    /// This version is updated to correctly handle PDF files by converting them to images first.
    /// </summary>
    public class AzureOcrService
    {
        private readonly ImageAnalysisClient _client;
        private readonly ILogger<AzureOcrService> _logger;

        public AzureOcrService(IConfiguration configuration, ILogger<AzureOcrService> logger)
        {
            _logger = logger;
            string endpoint = configuration["AzureOcr:Endpoint"] ?? throw new ArgumentNullException("AzureOcr:Endpoint is not configured in appsettings.json.");
            string key = configuration["AzureOcr:Key"] ?? throw new ArgumentNullException("AzureOcr:Key is not configured in appsettings.json.");
            var credential = new AzureKeyCredential(key);
            _client = new ImageAnalysisClient(new Uri(endpoint), credential);
            _logger.LogInformation("AzureOcrService (Image Analysis SDK) initialized successfully.");
        }

        public async Task<string> ExtractTextAsync(Stream fileStream)
        {
            var allPagesText = new StringBuilder();
            try
            {
                // --- REVISED LOGIC: Use ImageMagick to handle PDF and image files uniformly ---
                _logger.LogInformation("Processing file with ImageMagick to handle PDF conversion if necessary.");
                
                // Reset stream position just in case
                fileStream.Position = 0;

                using (var magickImages = new MagickImageCollection())
                {
                    var settings = new MagickReadSettings
                    {
                        Density = new Density(300, 300) // 300 DPI is good for OCR
                    };

                    await magickImages.ReadAsync(fileStream, settings);
                    
                    if (!magickImages.Any())
                    {
                        _logger.LogWarning("ImageMagick could not read any pages or images from the provided stream.");
                        return "Error: Could not process the file. It may be empty or corrupted.";
                    }

                    _logger.LogInformation("File processed by ImageMagick. Found {PageCount} page(s) to analyze with Azure.", magickImages.Count);

                    int pageNum = 1;
                    foreach (var imagePage in magickImages)
                    {
                        _logger.LogDebug("Analyzing page {PageNum} with Azure AI Vision.", pageNum);
                        allPagesText.AppendLine($"--Page {pageNum}--");
                        //PNG is a lossless format, ideal for OCR.
                        imagePage.Format = MagickFormat.Png;
                        byte[] pageAsPngBytes = imagePage.ToByteArray();
                        
                        var binaryData = BinaryData.FromBytes(pageAsPngBytes);

                        ImageAnalysisResult result = await _client.AnalyzeAsync(binaryData, VisualFeatures.Read);

                        foreach (DetectedTextBlock block in result.Read.Blocks)
                        {
                            foreach (DetectedTextLine line in block.Lines)
                            {
                                allPagesText.AppendLine(line.Text);
                            }
                        }
                        allPagesText.AppendLine(); 
                        pageNum++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during Azure Image Analysis or PDF processing.");
                return $"Error during Azure OCR: {ex.Message}";
            }

            return allPagesText.ToString();
        }
    }
}
