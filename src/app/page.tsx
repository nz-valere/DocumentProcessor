'use client';

import Head from 'next/head';
import dynamic from 'next/dynamic';

// Dynamically import the OCR component with no SSR
const OCRProcessor = dynamic(() => import('./components/OCRProcessor'), {
  ssr: false,
  loading: () => (
    <div className="text-center p-8">
      <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      <p className="mt-2 text-gray-600">Loading OCR processor...</p>
    </div>
  )
});

export default function Home() {
  return (
    <div className="p-5 font-sans max-w-3xl mx-auto">
      <Head>
        <title>OCR App</title>
        <meta name="description" content="Image to Text OCR with Next.js and Tesseract.js" />
        <link rel="icon" href="/favicon.ico" />
      </Head>

      <main>
        <OCRProcessor />
      </main>

      <footer className="text-center mt-12 pt-5 border-t border-gray-200 text-gray-500">
        <p>Powered by Next.js & Tesseract.js</p>
      </footer>
    </div>
  );
}