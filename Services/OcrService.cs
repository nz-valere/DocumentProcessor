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
                string languageModel = "eng";
                
                _logger.LogInformation("Initializing Tesseract engine with tessdata path: {TessDataPath} and model: {Model}", tessDataPath, languageModel);
                
                _tesseractEngine = new TesseractEngine(tessDataPath, languageModel, EngineMode.LstmOnly);
                
                _logger.LogInformation("Tesseract engine initialized successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Tesseract engine. Ensure 'tessdata' folder with '*.traineddata' files is present.");
                throw;
            }
        }
        
        private string PerformOcrOnImage(byte[] imageBytes)
        {
            using Mat src = Mat.FromImageData(imageBytes, ImreadModes.Color);
            if (src.Empty())
            {
                _logger.LogWarning("Source image is empty or could not be loaded for OCR processing.");
                return string.Empty;
            }

            using Mat gray = new Mat();
            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

            using Mat denoised = new Mat();
            Cv2.MedianBlur(gray, denoised, 3);

            using Mat deskewed = DeskewImage(denoised);

            using Mat binary = new Mat();
            Cv2.AdaptiveThreshold(deskewed, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);

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
        
        private Mat DeskewImage(Mat src)
        {
            Mat gray = src.Clone();
            Cv2.BitwiseNot(gray, gray);

            // Fixed: Use the correct HoughLinesP signature
            LineSegmentPoint[] lines = Cv2.HoughLinesP(gray, 1, Math.PI / 180, 100, 50, 10);
            
            if (lines == null || lines.Length == 0)
            {
                _logger.LogWarning("Deskewing failed: No lines detected. Returning original image.");
                gray.Dispose();
                return src.Clone(); 
            }

            double angleSum = 0;
            int count = 0;
            foreach (var line in lines)
            {
                if (line.P2.Y == line.P1.Y) continue; // Changed from Item2/Item0 to P2.Y/P1.Y

                double angle = Math.Atan2(line.P2.Y - line.P1.Y, line.P2.X - line.P1.X) * 180.0 / Math.PI;
                
                if (Math.Abs(angle) < 20)
                {
                    angleSum += angle;
                    count++;
                }
            }

            if(count == 0)
            {
                _logger.LogWarning("Deskewing failed: No suitable lines found for angle calculation.");
                gray.Dispose();
                return src.Clone();
            }

            double avgAngle = angleSum / count;
            _logger.LogDebug("Calculated skew angle: {Angle}", avgAngle);

            Mat rotated = new Mat();
            Point2f center = new Point2f(src.Cols / 2f, src.Rows / 2f);
            Mat rotationMatrix = Cv2.GetRotationMatrix2D(center, avgAngle, 1.0);
            
            Cv2.WarpAffine(src, rotated, rotationMatrix, src.Size(), InterpolationFlags.Cubic, BorderTypes.Constant, Scalar.All(255));
            
            gray.Dispose();
            rotationMatrix.Dispose();
            
            return rotated;
        }

        public string ProcessPdfAndExtractText(byte[] pdfBytes)
        {
            var allPagesText = new StringBuilder();
            var settings = new MagickReadSettings
            {
                Density = new Density(300, 300),
                Format = MagickFormat.Pdf
            };

            _logger.LogInformation("Starting PDF processing. Will convert pages to images at 300 DPI.");

            using (var magickImages = new MagickImageCollection())
            {
                try { magickImages.Read(pdfBytes, settings); }
                catch (MagickException ex)
                {
                    _logger.LogError(ex, "Magick.NET failed to read or process the PDF.");
                    if (ex.Message.ToLower().Contains("ghostscript"))
                    {
                         _logger.LogError("This error might indicate that Ghostscript is not installed or not found in the system's PATH.");
                    }
                    return $"Error reading PDF: {ex.Message}";
                }

                _logger.LogInformation("PDF read successfully. Number of pages: {PageCount}", magickImages.Count);
                if (magickImages.Count == 0) return "PDF contained no pages or could not be processed.";

                int pageNum = 1;
                foreach (var imagePage in magickImages)
                {
                    _logger.LogInformation("Processing PDF Page {PageNum}/{TotalPages}", pageNum, magickImages.Count);
                    imagePage.Format = MagickFormat.Png;
                    byte[] pageImageBytes = imagePage.ToByteArray();
                    
                    string textFromPage = PerformOcrOnImage(pageImageBytes); 

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
        
        public string ProcessImageAndExtractText(byte[] imageBytes)
        {
             return PerformOcrOnImage(imageBytes);
        }

        public void Dispose()
        {
            _tesseractEngine?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}