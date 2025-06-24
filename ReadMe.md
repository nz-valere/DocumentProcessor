# Image OCR Microservice

This .NET 8 microservice takes an image or a PDF file, preprocesses it, extracts text using Tesseract OCR, and then applies spell checking as a post-processing step. For PDF files, each page is converted to an image at 300 DPI before OCR. The processed text is returned as a downloadable `.txt` file.

## Features

-   **Input**: Accepts image files (PNG, JPG, JPEG, BMP, TIFF) and PDF files.
-   **PDF Processing**: Converts PDF pages to 300 DPI PNG images using Magick.NET.
-   **Image Preprocessing**: Grayscale conversion, contrast/sharpness adjustment, binarization, placeholder for deskewing.
-   **OCR**: Utilizes Tesseract OCR engine.
-   **Post-Processing**:
    -   **Spell Checking**: Uses NHunspell to check and correct misspelled words (replaces with the first suggestion).
-   **Output**: Returns extracted and post-processed text as a `.txt` file.

## Prerequisites

-   .NET 8 SDK
-   Docker (optional)
-   **Tesseract Language Data**:
    -   Download `.traineddata` files (e.g., `eng.traineddata`) from [Tesseract GitHub (tessdata_fast)](https://github.com/tesseract-ocr/tessdata_fast).
    -   Place them in the `ImageOcrMicroservice/tessdata` directory.
-   **Hunspell Dictionaries (for Spell Checking)**:
    -   Download Hunspell dictionary files for your language (typically an `.aff` file and a `.dic` file). For English (US), you'd need `en_US.aff` and `en_US.dic`.
    -   A good source is the [LibreOffice dictionaries GitHub repository](https://github.com/LibreOffice/dictionaries). Navigate to your language (e.g., `en/`) and find the files.
    -   Create a directory named `hunspelldata` in the `ImageOcrMicroservice` project root.
    -   Place the `.aff` and `.dic` files into this `ImageOcrMicroservice/hunspelldata/` folder. The service is currently hardcoded to look for `en_US.aff` and `en_US.dic`. You can modify `OcrService.cs` if you use different filenames or languages.

## Setup and Running Locally

1.  **Clone/Create Files.**
2.  **Restore .NET dependencies:** `dotnet restore`
3.  **Download Tesseract Language Data** (see Prerequisites).
4.  **Download Hunspell Dictionaries** (see Prerequisites).
4.  **Download gs10051w64 for pdf upload support ** (see Prerequisites).
5.  **Native Dependencies (if not using Docker):**
    -   Ghostscript for PDF processing (Magick.NET).
    -   NHunspell usually bundles its native components (`Hunspellx64.dll`/`Hunspellx86.dll`), but ensure your environment can run them.
6.  **Run the application:** `dotnet run`
    -   Swagger UI at `https://localhost:XXXX/swagger`.

## API Endpoint

-   **POST** `/api/ocr/extractText`
    -   **Request**: `multipart/form-data` with a `file`. Max 20MB.
    -   **Response**: A `text/plain` file download (e.g., `originalfilename_extracted_text.txt`).

## Building and Running with Docker

1.  **Ensure Tesseract data in `tessdata` and Hunspell dictionaries in `hunspelldata`.**
2.  **Build:** `docker build -t image-ocr-microservice .`
3.  **Run:** `docker run -d -p 8080:8080 --name ocr-service image-ocr-microservice`
    -   Accessible at `http://localhost:8080`.

## Dependencies

-   ASP.NET Core 8
-   OpenCvSharp4
-   Tesseract
-   Magick.NET
-   NHunspell
-   gs10051w64

## Further Enhancements (Language Modeling)

While this version includes spell checking, true Language Modeling (LM) is a more complex step for improving text coherence beyond individual word correction. LM could involve:
-   **N-gram models**: To assess the probability of word sequences.
-   **Rule-based corrections**: For common OCR errors (e.g., "l" vs "1", "O" vs "0") based on context.
-   **Machine Learning Models**: More advanced LMs (like BERT, GPT variants) can provide sophisticated text correction and generation but are resource-intensive and typically require specialized infrastructure or cloud services.

Integrating advanced LM is a significant undertaking and could be a future enhancement.

## Important Notes
-   The current spell checker replaces misspelled words with the first suggestion from Hunspell. This might not always be the desired correction.
-   The service looks for `en_US.aff` and `en_US.dic` by default. This can be configured in `OcrService.cs`.
-   Deskewing is still a placeholder.