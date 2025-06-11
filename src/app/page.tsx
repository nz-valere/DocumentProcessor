'use client';
import { useState, useCallback, ChangeEvent } from 'react';
import Head from 'next/head';
import { createWorker } from 'tesseract.js';
import ProgressBar from '../app/components/ProgressBar';

export default function Home() {
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [extractedText, setExtractedText] = useState<string>('');
  const [progress, setProgress] = useState<number>(0);
  const [isProcessing, setIsProcessing] = useState<boolean>(false);
  const [statusMessage, setStatusMessage] = useState<string>('');

  const handleFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      setImageFile(file);
      setPreviewUrl(URL.createObjectURL(file));
      setExtractedText('');
      setProgress(0);
      setStatusMessage('Image selected. Click "Process Image" to extract text.');
    } else {
      setImageFile(null);
      setPreviewUrl(null);
      setStatusMessage('');
    }
  };

  const processImage = useCallback(async () => {
    if (!imageFile) {
      setStatusMessage('Please select an image first.');
      return;
    }

    setIsProcessing(true);
    setProgress(0);
    setExtractedText('');
    setStatusMessage('Initializing OCR engine...');

    // Create worker without logger to avoid serialization issues
    const worker = await createWorker('eng', 1, {
      logger: m => {
        console.log(m);
        if (m.status === 'recognizing text') {
          setProgress(Math.floor((m.progress || 0) * 100));
          setStatusMessage(`Recognizing text: ${Math.floor((m.progress || 0) * 100)}%`);
        } else if (m.status === 'loading language traineddata') {
          setProgress(10);
          setStatusMessage('Loading language model...');
        } else if (m.status === 'initializing api') {
          setProgress(30);
          setStatusMessage('Initializing API...');
        } else if (m.status === 'initialized api') {
          setProgress(50);
          setStatusMessage('Ready to process...');
        }
      }
    });

    try {
      setStatusMessage('Processing image...');
      setProgress(60);
      
      const result = await worker.recognize(imageFile);
      setExtractedText(result.data.text);
      setStatusMessage('Text extracted successfully!');
      setProgress(100);
    } catch (error) {
      console.error('OCR Error:', error);
      setExtractedText('Error extracting text. See console for details.');
      setStatusMessage('An error occurred during OCR processing.');
      setProgress(0);
    } finally {
      await worker.terminate();
      setIsProcessing(false);
    }
  }, [imageFile]);

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
    <div className="p-5 font-sans max-w-3xl mx-auto">
      <Head>
        <title>Next.js OCR App</title>
        <meta name="description" content="Image to Text OCR with Next.js and Tesseract.js" />
        <link rel="icon" href="/favicon.ico" />
      </Head>

      <main>
        <h1 className="text-center text-2xl font-bold text-gray-800 mb-6">Image to Text (OCR)</h1>

        <div className="mb-5 flex flex-col items-center">
          <input
            type="file"
            accept="image/*"
            onChange={handleFileChange}
            className="p-2.5 border border-gray-300 rounded cursor-pointer"
          />
        </div>

        {previewUrl && (
          <div className="mb-5 text-center">
            <h2 className="text-xl text-gray-700 mb-2">Image Preview:</h2>
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
            disabled={isProcessing}
            className={`block mx-auto py-3 px-6 text-base text-white ${
              isProcessing ? 'bg-gray-400 cursor-not-allowed' : 'bg-blue-600 hover:bg-blue-700 cursor-pointer'
            } rounded shadow`}
          >
            {isProcessing ? 'Processing...' : 'Process Image'}
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
            <pre className="whitespace-pre-wrap break-words bg-white p-3 border border-gray-300 rounded">
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
      </main>

      <footer className="text-center mt-12 pt-5 border-t border-gray-200 text-gray-500">
        <p>Powered by Next.js & Tesseract.js</p>
      </footer>
    </div>
  );
}