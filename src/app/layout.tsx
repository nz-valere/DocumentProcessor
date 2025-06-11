export const metadata = {
  title: 'OCR Text Extractor',
  description: 'Extract text from images using Tesseract.js OCR',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}