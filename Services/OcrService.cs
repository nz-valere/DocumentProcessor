using OpenCvSharp;
using Tesseract;
using ImageMagick; // Added for PDF processing
using System.Text; // Added for StringBuilder

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
                _logger.LogInformation("Initializing Tesseract engine with tessdata path: {TessDataPath}", tessDataPath);
                _tesseractEngine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
                _logger.LogInformation("Tesseract engine initialized successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Tesseract engine. Ensure 'tessdata' folder with '*.traineddata' files is present.");
                throw;
            }
        }

        public string ProcessImageAndExtractText(byte[] imageBytes)
        {
            _logger.LogInformation("Starting image preprocessing and OCR for a single image/page.");
            using Mat src = Mat.FromImageData(imageBytes, ImreadModes.Color);
            if (src.Empty())
            {
                _logger.LogWarning("Source image is empty or could not be loaded for OCR processing.");
                return string.Empty;
            }

            using Mat gray = new Mat();
            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            _logger.LogDebug("Image converted to grayscale.");

            using Mat contrast = new Mat();
            gray.ConvertTo(contrast, -1, alpha: 1.5, beta: 0);
            _logger.LogDebug("Contrast adjusted.");

            using Mat sharpened = new Mat();
            Mat kernel = new Mat(3, 3, MatType.CV_32F, new float[] { 0, -1, 0, -1, 5, -1, 0, -1, 0 });
            Cv2.Filter2D(contrast, sharpened, MatType.CV_8U, kernel, anchor: new Point(-1, -1), delta: 0, borderType: BorderTypes.Default);
            kernel.Dispose(); // Dispose the kernel Mat
            _logger.LogDebug("Image sharpened.");
            
            Mat deskewed = sharpened.Clone(); 
            // Actual deskewing logic would go here if implemented
            _logger.LogDebug("Deskew step (currently a clone).");

            using Mat binary = new Mat();
            Cv2.Threshold(deskewed, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            _logger.LogDebug("Image binarized using Otsu's threshold.");
            
            if (deskewed != sharpened) // only if 'deskewed' was a new Mat from a real deskew operation
            {
                deskewed.Dispose();
            }

            try
            {
                byte[] imageAsBytesForTesseract;
                Cv2.ImEncode(".png", binary, out imageAsBytesForTesseract); 

                using (var img = Pix.LoadFromMemory(imageAsBytesForTesseract))
                {
                    using (var page = _tesseractEngine.Process(img))
                    {
                        string text = page.GetText();
                        _logger.LogInformation("OCR processed for image/page. Mean confidence: {MeanConfidence}", page.GetMeanConfidence());
                        return text.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Tesseract OCR processing for an image/page.");
                return $"Error during OCR: {ex.Message}";
            }
        }

        public string ProcessPdfAndExtractText(byte[] pdfBytes)
        {
            var allPagesText = new StringBuilder();
            var settings = new MagickReadSettings
            {
                Density = new Density(300, 300), // Set DPI for rasterization
                Format = MagickFormat.Pdf // Explicitly state we are reading a PDF
            };

            _logger.LogInformation("Starting PDF processing. Will convert pages to images at 300 DPI.");

            using (var magickImages = new MagickImageCollection())
            {
                try
                {
                    magickImages.Read(pdfBytes, settings);
                }
                catch (MagickException ex)
                {
                    _logger.LogError(ex, "Magick.NET failed to read or process the PDF.");
                    // Check if Ghostscript might be missing or if the PDF is corrupted.
                    if (ex.Message.ToLower().Contains("ghostscript"))
                    {
                         _logger.LogError("This error might indicate that Ghostscript is not installed or not found in the system's PATH, especially in a Docker environment.");
                    }
                    return $"Error reading PDF: {ex.Message}";
                }

                _logger.LogInformation("PDF read successfully. Number of pages: {PageCount}", magickImages.Count);

                if (magickImages.Count == 0)
                {
                    _logger.LogWarning("PDF contained no pages or could not be parsed correctly.");
                    return "PDF contained no pages or could not be processed.";
                }

                int pageNum = 1;
                foreach (var imagePage in magickImages)
                {
                    _logger.LogInformation("Processing PDF Page {PageNum}/{TotalPages}", pageNum, magickImages.Count);
                    // It's good practice to set format before ToByteArray if a specific one is desired
                    imagePage.Format = MagickFormat.Png; 
                    
                    // Optional: Add a border or change background if Tesseract struggles with edges
                    // imagePage.BorderColor = MagickColors.White;
                    // imagePage.Border(10); // Add 10px white border

                    byte[] pageImageBytes = imagePage.ToByteArray();
                    string textFromPage = ProcessImageAndExtractText(pageImageBytes); 

                    if (!string.IsNullOrWhiteSpace(textFromPage))
                    {
                        if (allPagesText.Length > 0) allPagesText.AppendLine("\n");
                        allPagesText.AppendLine($"--- Page {pageNum} ---");
                        allPagesText.AppendLine(textFromPage);
                    }
                    else
                    {
                        if (allPagesText.Length > 0) allPagesText.AppendLine("\n");
                        allPagesText.AppendLine($"--- Page {pageNum} (No text extracted or page was blank) ---");
                    }
                    pageNum++;
                }
            }
            _logger.LogInformation("Finished processing all PDF pages.");
            return allPagesText.ToString();
        }

        public void Dispose()
        {
            _tesseractEngine?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}