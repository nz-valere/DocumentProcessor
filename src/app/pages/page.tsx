/* eslint-disable @typescript-eslint/no-explicit-any */
'use client'

import type { NextPage } from 'next';
import Head from 'next/head';
import React, { useState, ChangeEvent, useCallback } from 'react';
import Tesseract, { Worker } from 'tesseract.js';
import ProgressBar from '../components/ProgressBar';

// Add a proper type declaration for the worker
interface TesseractWorker extends Worker {
  loadLanguage: (language: string) => Promise<any>;
  initialize: (language: string) => Promise<any>;
  recognize: (image: File | string) => Promise<any>;
  setLogger: (logger: (log: any) => void) => void;
}

const Home: NextPage = () => {
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

    const worker = await Tesseract.createWorker() as TesseractWorker;
    
    worker.setLogger((m) => {
      // console.log(m); // For detailed logging
      if (m.status === 'recognizing text') {
        setProgress(Math.floor(m.progress * 100));
        setStatusMessage(`Recognizing text: ${Math.floor(m.progress * 100)}%`);
      } else if (m.status === 'loading language model') {
        setStatusMessage('Loading language model...');
      } else if (m.status === 'initializing api') {
        setStatusMessage('Initializing API...');
      } else {
        setStatusMessage(`Status: ${m.status}`);
      }
    });

    try {
      await worker.loadLanguage('eng'); // Load English language model
      await worker.initialize('eng');
      setStatusMessage('Processing image...');
      const { data: { text } } = await worker.recognize(imageFile);
      setExtractedText(text);
      setStatusMessage('Text extracted successfully!');
      setProgress(100);
    } catch (error) {
      console.error('OCR Error:', error);
      setExtractedText('Error extracting text. See console for details.');
      setStatusMessage('An error occurred during OCR processing.');
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
    <div style={{ padding: '20px', fontFamily: 'Arial, sans-serif', maxWidth: '800px', margin: '0 auto' }}>
      <Head>
        <title>Next.js OCR App</title>
        <meta name="description" content="Image to Text OCR with Next.js and Tesseract.js" />
        <link rel="icon" href="/favicon.ico" />
      </Head>

      <main>
        <h1 style={{ textAlign: 'center', color: '#333' }}>Image to Text (OCR)</h1>

        <div style={{ marginBottom: '20px', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
          <input
            type="file"
            accept="image/*"
            onChange={handleFileChange}
            style={{
              padding: '10px',
              border: '1px solid #ccc',
              borderRadius: '4px',
              cursor: 'pointer'
            }}
          />
        </div>

        {previewUrl && (
          <div style={{ marginBottom: '20px', textAlign: 'center' }}>
            <h2 style={{ color: '#555' }}>Image Preview:</h2>
            <img
              src={previewUrl}
              alt="Selected preview"
              style={{ maxWidth: '100%', maxHeight: '300px', border: '1px solid #ddd', borderRadius: '4px' }}
            />
          </div>
        )}

        {imageFile && (
          <button
            onClick={processImage}
            disabled={isProcessing}
            style={{
              display: 'block',
              margin: '20px auto',
              padding: '12px 25px',
              fontSize: '16px',
              color: 'white',
              backgroundColor: isProcessing ? '#aaa' : '#0070f3',
              border: 'none',
              borderRadius: '5px',
              cursor: isProcessing ? 'not-allowed' : 'pointer',
              boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
            }}
          >
            {isProcessing ? 'Processing...' : 'Process Image'}
          </button>
        )}

        {isProcessing && (
          <div style={{ marginBottom: '20px' }}>
            <ProgressBar value={progress} />
          </div>
        )}
        
        {statusMessage && (
            <p style={{ textAlign: 'center', color: '#333', fontStyle: 'italic' }}>{statusMessage}</p>
        )}

        {extractedText && (
          <div style={{ marginTop: '30px', padding: '15px', border: '1px solid #eee', borderRadius: '4px', backgroundColor: '#f9f9f9' }}>
            <h2 style={{ color: '#555' }}>Extracted Text:</h2>
            <pre style={{ whiteSpace: 'pre-wrap', wordWrap: 'break-word', backgroundColor: 'white', padding: '10px', border: '1px solid #ddd', borderRadius: '3px' }}>
              {extractedText}
            </pre>
            <button
              onClick={downloadTextFile}
              style={{
                marginTop: '15px',
                padding: '10px 20px',
                fontSize: '15px',
                color: 'white',
                backgroundColor: '#28a745',
                border: 'none',
                borderRadius: '5px',
                cursor: 'pointer',
                boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
              }}
            >
              Download Text File
            </button>
          </div>
        )}
      </main>

      <footer style={{ textAlign: 'center', marginTop: '50px', paddingTop: '20px', borderTop: '1px solid #eee', color: '#777' }}>
        <p>Powered by Next.js & Tesseract.js</p>
      </footer>
    </div>
  );
};

export default Home;