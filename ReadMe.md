# Image OCR Microservice

This .NET 8 microservice takes an image or a PDF file, preprocesses it to improve text clarity, and then uses Tesseract OCR to extract text. For PDF files, each page is converted to an image at 300 DPI before OCR processing.

## Features

-   **Input**: Accepts image files (PNG, JPG, JPEG, BMP, TIFF) and PDF files.
-   **PDF Processing**:
    -   Converts each page of an uploaded PDF to a PNG image at 300 DPI using Magick.NET.
-   **Image Preprocessing**:
    -   Convert to grayscale
    -   Adjust contrast and sharpness
    -   Binarize (black/white) using Otsu's thresholding
    -   Placeholder for image deskewing
-   **OCR**:
    -   Utilizes Tesseract OCR engine (version 5.x via the Tesseract NuGet package).
    -   Supports LSTM models for text recognition (default in Tesseract 4+).

## Prerequisites

-   .NET 8 SDK
-   Docker (optional, for containerized deployment)
-   Tesseract Language Data:
    -   Download the required `.traineddata` files for your language(s) (e.g., `eng.traineddata` for English) from the [Tesseract GitHub repository (tessdata_fast or tessdata_best)](https://github.com/tesseract-ocr/tessdata_fast).
    -   Place these files in the `ImageOcrMicroservice/tessdata` directory. The project is configured to copy this folder to the output directory.

## Setup and Running Locally

1.  **Clone the repository (or create the files as listed).**
2.  **Restore .NET dependencies:**
    ```bash
    dotnet restore
    ```
3.  **Download Tesseract Language Data:**
    -   Create a directory named `tessdata` in the `ImageOcrMicroservice` project root.
    -   Download `eng.traineddata` (or other languages you need) from [tessdata_fast](https://github.com/tesseract-ocr/tessdata_fast) and place it into the `tessdata` folder.
4.  **Ensure Native Dependencies for Magick.NET (if not using Docker):**
    -   For PDF processing, Magick.NET might rely on Ghostscript. Ensure it's installed and accessible in your system's PATH if you encounter issues with PDF processing locally.
5.  **Run the application:**
    ```bash
    dotnet run
    ```
    The service will typically start on `https://localhost:XXXX` and `http://localhost:YYYY`. Check the console output for the exact URLs.
    The Swagger UI will be available at `https://localhost:XXXX/swagger`.

## API Endpoint

-   **POST** `/api/ocr/extractText`
    -   **Request**: `multipart/form-data` with a file.
        -   `file`: The image or PDF file to process. Max 20MB.
    -   **Response**: JSON object with the extracted text. For PDFs, text from all pages is concatenated with page separators.
        ```json
        {
          "text": "--- Page 1 ---\nExtracted text from page 1...\n\n--- Page 2 ---\nExtracted text from page 2..."
        }
        ```

## Building and Running with Docker

1.  **Ensure Tesseract language files are in the `tessdata` folder.** The Dockerfile copies these.
2.  **Build the Docker image:**
    ```bash
    docker build -t image-ocr-microservice .
    ```
3.  **Run the Docker container:**
    ```bash
    docker run -d -p 8080:8080 --name ocr-service image-ocr-microservice
    ```
    The service will be accessible at `http://localhost:8080`.
    The API endpoint will be `http://localhost:8080/api/ocr/extractText`.

    *Docker Dependencies*: The Dockerfile includes `ghostscript` which is needed by Magick.NET for PDF processing. It also includes necessary libraries for OpenCV and Tesseract.

## Dependencies

-   ASP.NET Core 8
-   OpenCvSharp4 (wrapper for OpenCV)
-   Tesseract (wrapper for Tesseract OCR engine)
-   Magick.NET (for PDF processing and image manipulation)

## Important Notes

-   **Deskewing**: The current deskewing step in `OcrService.cs` is a placeholder (clones the image). Robust deskewing is complex and may require a dedicated algorithm or library.
-   **Performance**: Processing large PDFs or very high-resolution images can be resource-intensive.
-   **Error Handling**: The service includes basic error handling. For production, more detailed error reporting and resilience strategies might be needed.
-   **Tesseract Configuration**: Tesseract performance can be highly dependent on image quality and its internal parameters. Tuning these may be necessary for optimal results on your specific data.