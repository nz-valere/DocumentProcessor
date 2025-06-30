using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using System.Text;

namespace ImageOcrMicroservice.Services
{
    /// <summary>
    /// Service to interact with Azure's Document Analysis for advanced OCR,
    /// especially for handwritten text.
    /// </summary>
    public class AzureOcrService
    {
        private readonly DocumentAnalysisClient _client;
        private readonly ILogger<AzureOcrService> _logger;

        public AzureOcrService(IConfiguration configuration, ILogger<AzureOcrService> logger)
        {
            _logger = logger;
            string endpoint = configuration["AzureOcr:Endpoint"] ?? throw new ArgumentNullException("Azure OCR Endpoint is not configured.");
            string key = configuration["AzureOcr:Key"] ?? throw new ArgumentNullException("Azure OCR Key is not configured.");

            var credential = new AzureKeyCredential(key);
            _client = new DocumentAnalysisClient(new Uri(endpoint), credential);
            _logger.LogInformation("AzureOcrService initialized successfully.");
        }

        public async Task<string> ExtractTextAsync(Stream fileStream)
        {
            var allPagesText = new StringBuilder();
            try
            {
                // Use the "prebuilt-read" model which is excellent for text extraction, including handwriting.
                AnalyzeDocumentOperation operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", fileStream);
                AnalyzeResult result = operation.Value;

                _logger.LogInformation("Azure Document Analysis completed. Found {PageCount} pages.", result.Pages.Count);

                int pageNum = 1;
                foreach (DocumentPage page in result.Pages)
                {
                    allPagesText.AppendLine($"--- Page {pageNum} ---");
                    foreach (DocumentLine line in page.Lines)
                    {
                        allPagesText.AppendLine(line.Content);
                    }
                    pageNum++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during Azure Document Analysis.");
                // Return an error message that can be handled downstream
                return $"Error during Azure OCR: {ex.Message}";
            }

            return allPagesText.ToString();
        }
    }
}
