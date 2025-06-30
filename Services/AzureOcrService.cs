using Azure;
using Azure.AI.Vision.ImageAnalysis;
using System.Text;

namespace ImageOcrMicroservice.Services
{
    /// <summary>
    /// Service to interact with Azure's new AI Vision Image Analysis service for advanced OCR,
    /// especially for handwritten text.
    /// </summary>
    public class AzureOcrService
    {
        // --- CHANGED: The client is now an ImageAnalysisClient ---
        private readonly ImageAnalysisClient _client;
        private readonly ILogger<AzureOcrService> _logger;

        public AzureOcrService(IConfiguration configuration, ILogger<AzureOcrService> logger)
        {
            _logger = logger;

            // This part remains the same. It securely loads your credentials from appsettings.json
            string endpoint = configuration["AzureOcr:Endpoint"] ?? throw new ArgumentNullException("AzureOcr:Endpoint is not configured in appsettings.json.");
            string key = configuration["AzureOcr:Key"] ?? throw new ArgumentNullException("AzureOcr:Key is not configured in appsettings.json.");

            var credential = new AzureKeyCredential(key);

            // --- CHANGED: Instantiating the new ImageAnalysisClient ---
            _client = new ImageAnalysisClient(new Uri(endpoint), credential);

            _logger.LogInformation("AzureOcrService (Image Analysis SDK) initialized successfully.");
        }

        /// <summary>
        /// Extracts text from a document stream using the Azure AI Vision "Read" feature.
        /// The method signature remains the same, so the orchestrator does not need to change.
        /// </summary>
        /// <param name="fileStream">The stream of the document (PDF or image).</param>
        /// <returns>The extracted text as a single string.</returns>
        public async Task<string> ExtractTextAsync(Stream fileStream)
        {
            var allPagesText = new StringBuilder();
            try
            {
                // The new SDK works with BinaryData, which can be created directly from a stream.
                // This is efficient as it avoids loading the entire file into a byte array in memory first.
                fileStream.Position = 0; // Ensure the stream is at the beginning before reading.
                var binaryData = await BinaryData.FromStreamAsync(fileStream);

                // --- CHANGED: Calling the new AnalyzeAsync method ---
                // We specifically request the "Read" feature (VisualFeatures.Read) to perform OCR.
                ImageAnalysisResult result = await _client.AnalyzeAsync(
                    binaryData,
                    VisualFeatures.Read);

                _logger.LogInformation("Azure Image Analysis completed. Found {BlockCount} text blocks.", result.Read.Blocks.Count);

                // --- CHANGED: Loop through the new result structure (Blocks and Lines) ---
                // The new SDK provides text in blocks, which is slightly different from the old 'pages' structure.
                // We will just concatenate all lines from all blocks to get the full text.
                foreach (DetectedTextBlock block in result.Read.Blocks)
                {
                    foreach (DetectedTextLine line in block.Lines)
                    {
                        allPagesText.AppendLine(line.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during Azure Image Analysis.");
                // Return an error message that the orchestrator can handle.
                return $"Error during Azure OCR: {ex.Message}";
            }

            return allPagesText.ToString();
        }
    }
}
