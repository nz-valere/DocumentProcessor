'use client';

import { useState, useCallback, ChangeEvent, useEffect } from 'react';
import { createWorker } from 'tesseract.js';
import ProgressBar from '@/app/components/ProgressBar';

// Dynamically import pdfjs-dist only on client side
let pdfjsLib: any = null;

const initializePDFJS = async () => {
  if (typeof window !== 'undefined' && !pdfjsLib) {
    pdfjsLib = await import('pdfjs-dist');
    pdfjsLib.GlobalWorkerOptions.workerSrc = '/pdf.worker.min.mjs';
  }
  return pdfjsLib;
};

export default function OCRProcessor() {
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [extractedText, setExtractedText] = useState<string>('');
  const [progress, setProgress] = useState<number>(0);
  const [isProcessing, setIsProcessing] = useState<boolean>(false);
  const [statusMessage, setStatusMessage] = useState<string>('');
  const [pdfPages, setPdfPages] = useState<Blob[]>([]);
  const [pdfLibInitialized, setPdfLibInitialized] = useState<boolean>(false);

  // Initialize PDF.js on component mount
  useEffect(() => {
    const init = async () => {
      try {
        await initializePDFJS();
        setPdfLibInitialized(true);
      } catch (error) {
        console.error('Failed to initialize PDF.js:', error);
        setStatusMessage('Failed to initialize PDF processing library');
      }
    };
    init();
  }, []);

  // Clean up object URLs
  useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  // Fixed PDF conversion function
  const convertPdfToImages = useCallback(async (file: File) => {
    if (!pdfjsLib) {
      throw new Error('PDF.js not initialized');
    }

    const arrayBuffer = await file.arrayBuffer();
    const pdf = await pdfjsLib.getDocument({ data: arrayBuffer }).promise;
    const pages: Blob[] = [];

    for (let i = 1; i <= pdf.numPages; i++) {
      const page = await pdf.getPage(i);
      const viewport = page.getViewport({ scale: 2.0 }); // Increased scale for better OCR
      const canvas = document.createElement('canvas');
      canvas.width = viewport.width;
      canvas.height = viewport.height;
      const context = canvas.getContext('2d');
      if (!context) throw new Error('Failed to get canvas context');

      await page.render({ 
        canvasContext: context, 
        viewport 
      }).promise;

      // Convert canvas to blob instead of data URL
      const blob = await new Promise<Blob>((resolve, reject) => {
        canvas.toBlob((blob) => {
          if (blob) resolve(blob);
          else reject(new Error('Failed to convert canvas to blob'));
        }, 'image/jpeg', 0.9);
      });

      pages.push(blob);
    }
    return pages;
  }, []);

  const handleFileChange = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];

    if (file) {
      // Clean up previous state
      setImageFile(file);
      setExtractedText('');
      setProgress(0);
      setPdfPages([]);
      
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
        setPreviewUrl(null);
      }

      if (file.type === 'application/pdf') {
        if (!pdfLibInitialized) {
          setStatusMessage('PDF processing library not ready. Please wait and try again.');
          return;
        }

        setStatusMessage('PDF selected. Converting to images...');
        try {
          const pages = await convertPdfToImages(file);
          setPdfPages(pages);
          // Create preview from first page
          setPreviewUrl(URL.createObjectURL(pages[0]));
          setStatusMessage(`PDF converted to ${pages.length} page(s). Click "Process Document" to extract text.`);
        } catch (error) {
          setStatusMessage('Error converting PDF to images. Please try a different file.');
          console.error('PDF Conversion Error:', error);
          setImageFile(null);
        }
      } else {
        setPreviewUrl(URL.createObjectURL(file));
        setStatusMessage('Image selected. Click "Process Document" to extract text.');
      }
    } else {
      setImageFile(null);
      setPreviewUrl(null);
      setStatusMessage('');
      setPdfPages([]);
    }
  };

  const processImage = useCallback(async () => {
    if (!imageFile) {
      setStatusMessage('Please select an image or PDF first.');
      return;
    }
    
    setIsProcessing(true);
    setProgress(0);
    setExtractedText('');
    setStatusMessage('Initializing OCR engine...');

    const worker = await createWorker('eng');

    try {
      let allText = '';
      
      if (imageFile.type === 'application/pdf') {
        let pagesToProcess = pdfPages;
        
        if (pdfPages.length === 0) {
          setStatusMessage('Converting PDF...');
          pagesToProcess = await convertPdfToImages(imageFile);
          setPdfPages(pagesToProcess);
        }

        setStatusMessage('Processing PDF document...');
        
        for (let i = 0; i < pagesToProcess.length; i++) {
          setStatusMessage(`Processing page ${i + 1} of ${pagesToProcess.length}...`);
          const result = await worker.recognize(pagesToProcess[i]);
          allText += `--- Page ${i + 1} ---\n${result.data.text}\n\n`;
          setProgress(Math.floor(((i + 1) / pagesToProcess.length) * 90));
        }
      } else {
        setStatusMessage('Processing image...');
        const result = await worker.recognize(imageFile);
        allText = result.data.text;
      }

      setExtractedText(allText);
      setStatusMessage('Text extracted successfully!');
      setProgress(100);
    } catch (error) {
      console.error('OCR Error:', error);
      setStatusMessage('An error occurred during OCR processing.');
      setProgress(0);
    } finally {
      await worker.terminate();
      setIsProcessing(false); 
    }
  }, [imageFile, pdfPages, convertPdfToImages]);

  const downloadTextFile = () => {
    if (!extractedText) {
      setStatusMessage('No text to download.');
      return;
    }
    const blob = new Blob([extractedText], { type: 'text/plain;charset=utf-8' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    const originalFileName = imageFile?.name.substring(0, imageFile.name.lastIndexOf('.')) || 'extracted_text';
    link.download = `${originalFileName}_ocr.txt`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    setStatusMessage('Text file downloaded.');
  };

  return (
    <>
      <h1 className="text-center text-2xl font-bold text-gray-800 mb-6">Image to Text (OCR)</h1>

      <div className="mb-5 flex flex-col items-center">
        <input
          type="file"
          accept="image/*, application/pdf"
          onChange={handleFileChange}
          className="p-2.5 border border-gray-300 rounded cursor-pointer"
        />
        <p className='text-sm mt-2 text-gray-600'>Supported formats: Images (PNG, JPG, etc.) and PDF</p>
      </div>

      {previewUrl && (
        <div className="mb-5 text-center">
          <h2 className="text-xl text-gray-700 mb-2">Preview:</h2>
          <img
            src={previewUrl}
            alt="Selected preview"
            className="max-w-full max-h-[300px] border border-gray-300 rounded mx-auto"
          />
        </div>
      )}

      {imageFile && (
        <button
          onClick={processImage}
          disabled={isProcessing || !pdfLibInitialized}
          className={`block mx-auto py-3 px-6 text-base text-white ${
            isProcessing || !pdfLibInitialized ? 'bg-gray-400 cursor-not-allowed' : 'bg-blue-600 hover:bg-blue-700 cursor-pointer'
          } rounded shadow`}
        >
          {isProcessing ? 'Processing...' : !pdfLibInitialized ? 'Loading PDF processor...' : 'Process Document'}
        </button>
      )}

      {isProcessing && (
        <div className="mb-5">
          <ProgressBar value={progress} />
        </div>
      )}
      
      {statusMessage && (
        <p className="text-center text-gray-700 italic my-3">{statusMessage}</p>
      )}

      {extractedText && (
        <div className="mt-7 p-4 border border-gray-200 rounded bg-gray-50">
          <h2 className="text-xl text-gray-700 mb-2">Extracted Text:</h2>
          <pre className="text-gray-700 whitespace-pre-wrap break-words bg-white p-3 border border-gray-300 rounded">
            {extractedText}
          </pre>
          <button
            onClick={downloadTextFile}
            className="mt-4 py-2.5 px-5 text-base text-white bg-green-600 hover:bg-green-700 rounded shadow"
          >
            Download Text File
          </button>
        </div>
      )}
    </>
  );
}