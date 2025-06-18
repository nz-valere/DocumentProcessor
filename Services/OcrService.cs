using OpenCvSharp;
using Tesseract;
using ImageMagick;
using System.Text;

namespace ImageOcrMicroservice.Services
{
    public class OcrService : IDisposable
    {
        private readonly TesseractEngine _tesseractEngine;
        private readonly ILogger<OcrService> _logger;

        public OcrService(ILogger<OcrService> logger)
        {
            _logger = logger;
            try
            {
                string tessDataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
                string languageModel = "fra";
                
                _logger.LogInformation("Initializing Tesseract engine with tessdata path: {TessDataPath} and model: {Model}", tessDataPath, languageModel);
                
                _tesseractEngine = new TesseractEngine(tessDataPath, languageModel, EngineMode.LstmOnly);
                
                _logger.LogInformation("Tesseract engine initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Tesseract engine. Ensure 'tessdata' folder with '*.traineddata' files is present");
                throw;
            }
        }

        private string PerformOcrOnImage(byte[] imageBytes)
        {
            try
            {
                _logger.LogDebug("Starting OCR processing on image data of {ImageSize} bytes", imageBytes.Length);
                
                using (var img = Pix.LoadFromMemory(imageBytes))
                {
                    using (var page = _tesseractEngine.Process(img))
                    {
                        string text = page.GetText();
                        var confidence = page.GetMeanConfidence();
                        var extractedLength = text.Trim().Length;
                        
                        _logger.LogInformation("OCR completed successfully. Confidence: {Confidence:F2}%, Text length: {TextLength} characters", 
                            confidence, extractedLength);
                        
                        if (confidence < 60)
                        {
                            _logger.LogWarning("Low OCR confidence detected: {Confidence:F2}%. Text quality may be poor", confidence);
                        }
                        
                        return text.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Tesseract OCR processing");
                return $"Error during OCR: {ex.Message}";
            }
        }

        public string ProcessPdfAndExtractText(byte[] pdfBytes)
        {
            _logger.LogInformation("Starting PDF OCR processing for {PdfSize} bytes", pdfBytes.Length);
            
            var allPagesText = new StringBuilder();
            var settings = new MagickReadSettings
            {
                Density = new Density(300, 300),
                Format = MagickFormat.Pdf
            };

            using (var magickImages = new MagickImageCollection())
            {
                try 
                { 
                    magickImages.Read(pdfBytes, settings); 
                }
                catch (MagickException ex)
                {
                    _logger.LogError(ex, "Magick.NET failed to read PDF");
                    if (ex.Message.ToLower().Contains("ghostscript"))
                    {
                         _logger.LogError("Ghostscript may not be installed or found in system PATH");
                    }
                    return $"Error reading PDF: {ex.Message}";
                }

                var pageCount = magickImages.Count;
                _logger.LogInformation("PDF processed successfully. Processing {PageCount} pages", pageCount);
                
                if (pageCount == 0) 
                {
                    _logger.LogWarning("PDF contained no pages");
                    return "PDF contained no pages or could not be processed.";
                }

                int pageNum = 1;
                int successfulPages = 0;
                
                foreach (var imagePage in magickImages)
                {
                    _logger.LogDebug("Processing page {PageNum}/{TotalPages}", pageNum, pageCount);
                    
                    imagePage.Format = MagickFormat.Png; 
                    byte[] pageImageBytes = imagePage.ToByteArray();
                    
                    string textFromPage = PerformOcrOnImage(pageImageBytes); 

                    if (!string.IsNullOrWhiteSpace(textFromPage) && !textFromPage.StartsWith("Error during OCR"))
                    {
                        successfulPages++;
                        if (allPagesText.Length > 0) allPagesText.AppendLine("\n");
                        allPagesText.AppendLine($"--- Page {pageNum} ---");
                        allPagesText.AppendLine(textFromPage);
                    }
                    else
                    {
                        _logger.LogWarning("No text extracted from page {PageNum}", pageNum);
                        if (allPagesText.Length > 0) allPagesText.AppendLine("\n");
                        allPagesText.AppendLine($"--- Page {pageNum} (No text extracted or page was blank) ---");
                    }
                    pageNum++;
                }
                
                _logger.LogInformation("PDF processing completed. Successfully processed {SuccessfulPages}/{TotalPages} pages", 
                    successfulPages, pageCount);
            }
            
            return allPagesText.ToString();
        }
        
        public string ProcessImageAndExtractText(byte[] imageBytes)
        {
            _logger.LogInformation("Processing single image for OCR extraction");
            return PerformOcrOnImage(imageBytes);
        }

        public void Dispose()
        {
            _logger.LogDebug("Disposing OcrService and Tesseract engine");
            _tesseractEngine?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}