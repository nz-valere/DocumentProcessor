"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Upload, FileText, BoxIcon as Batch, Loader2 } from "lucide-react"
import { DocumentPreviewModal } from "@/components/document-preview-modal"
import { useToast } from "@/hooks/use-toast"

interface ProcessedDocument {
  id: string
  filename: string
  imageUrl: string
  metadata: Record<string, any>
}

export default function HomePage() {
  const [isUploading, setIsUploading] = useState(false)
  const [processedDocuments, setProcessedDocuments] = useState<ProcessedDocument[]>([])
  const [showPreview, setShowPreview] = useState(false)
  const { toast } = useToast()

  const handleFileUpload = async (files: FileList | null, isBatch: boolean) => {
    if (!files || files.length === 0) return

    setIsUploading(true)
    const formData = new FormData()

    if (isBatch) {
      // For batch processing, add all files
      Array.from(files).forEach((file, index) => {
        formData.append(`files`, file)
      })
    } else {
      // For single processing, add just one file
      formData.append("file", files[0])
    }

    try {
      const endpoint = isBatch ? "/api/metadata/extract-batch" : "/api/metadata/extract"
      const response = await fetch(endpoint, {
        method: "POST",
        body: formData,
      })

      if (!response.ok) {
        throw new Error("Failed to process documents")
      }

      const result = await response.json()

      // Transform the response to match our expected format
      const documents = isBatch ? result.documents : [result.document]
      setProcessedDocuments(documents)
      setShowPreview(true)

      toast({
        title: "Documents processed successfully",
        description: `${documents.length} document(s) processed and ready for review.`,
      })
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    } catch (error) {
      toast({
        title: "Error processing documents",
        description: "Please try again or contact support.",
        variant: "destructive",
      })
    } finally {
      setIsUploading(false)
    }
  }

  const handleSingleUpload = () => {
    const input = document.createElement("input")
    input.type = "file"
    input.accept = ".pdf,.jpg,.jpeg,.png"
    input.onchange = (e) => {
      const target = e.target as HTMLInputElement
      handleFileUpload(target.files, false)
    }
    input.click()
  }

  const handleBatchUpload = () => {
    const input = document.createElement("input")
    input.type = "file"
    input.accept = ".pdf,.jpg,.jpeg,.png"
    input.multiple = true
    input.onchange = (e) => {
      const target = e.target as HTMLInputElement
      handleFileUpload(target.files, true)
    }
    input.click()
  }

  return (
    <div className="min-h-screen bg-white">
      {/* Header */}
      <header className="bg-black text-white py-6">
        <div className="container mx-auto px-4">
          <div className="flex items-center space-x-4">
            <div className="w-12 h-12 bg-orange-500 rounded-lg flex items-center justify-center">
              {/* <FileText className="w-6 h-6 text-white" /> */}
              <img src="/logo.svg" alt="My Icon" width={41} height={41} />
            </div>
            <div>
              <h1 className="text-2xl font-bold">OrangeOcr</h1>
              <p className="text-gray-300"> Document Processor</p>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="container mx-auto px-4 py-12">
        <div className="max-w-4xl mx-auto">
          {/* Welcome Section */}
          <div className="text-center mb-12">
            <h2 className="text-4xl font-bold text-black mb-4">Process Your Documents Intelligently</h2>
            <p className="text-lg text-gray-600 max-w-2xl mx-auto">
              Upload scanned documents or PDFs for automatic OCR processing, metadata extraction, and seamless
              integration to the GED system.
            </p>
          </div>

          {/* Upload Cards */}
          <div className="grid md:grid-cols-2 gap-8 mb-12">
            {/* Single Upload */}
            <Card className="border-2 border-gray-200 hover:border-orange-500 transition-colors">
              <CardHeader className="text-center">
                <div className="w-16 h-16 bg-orange-100 rounded-full flex items-center justify-center mx-auto mb-4">
                  <Upload className="w-8 h-8 text-orange-500" />
                </div>
                <CardTitle className="text-xl text-black">Single Document</CardTitle>
                <CardDescription>Upload one document at a time for quick processing</CardDescription>
              </CardHeader>
              <CardContent>
                <Button
                  onClick={handleSingleUpload}
                  disabled={isUploading}
                  className="w-full bg-orange-500 hover:bg-orange-600 text-white"
                  size="lg"
                >
                  {isUploading ? (
                    <>
                      <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                      Processing...
                    </>
                  ) : (
                    <>
                      <Upload className="w-4 h-4 mr-2" />
                      Upload Single Document
                    </>
                  )}
                </Button>
                <p className="text-sm text-gray-500 mt-2 text-center">Supports PDF, JPG, PNG formats</p>
              </CardContent>
            </Card>

            {/* Batch Upload */}
            <Card className="border-2 border-gray-200 hover:border-orange-500 transition-colors">
              <CardHeader className="text-center">
                <div className="w-16 h-16 bg-orange-100 rounded-full flex items-center justify-center mx-auto mb-4">
                  <Batch className="w-8 h-8 text-orange-500" />
                </div>
                <CardTitle className="text-xl text-black">Batch Processing</CardTitle>
                <CardDescription>Upload multiple documents for efficient batch processing</CardDescription>
              </CardHeader>
              <CardContent>
                <Button
                  onClick={handleBatchUpload}
                  disabled={isUploading}
                  className="w-full bg-black hover:bg-gray-800 text-white"
                  size="lg"
                >
                  {isUploading ? (
                    <>
                      <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                      Processing...
                    </>
                  ) : (
                    <>
                      <Batch className="w-4 h-4 mr-2" />
                      Upload Batch Documents
                    </>
                  )}
                </Button>
                <p className="text-sm text-gray-500 mt-2 text-center">Select multiple files for batch processing</p>
              </CardContent>
            </Card>
          </div>

          {/* Features Section */}
          <div className="bg-gray-50 rounded-lg p-8">
            <h3 className="text-2xl font-bold text-black mb-6 text-center">Powerful Features</h3>
            <div className="grid md:grid-cols-3 gap-6">
              <div className="text-center">
                <div className="w-12 h-12 bg-orange-500 rounded-lg flex items-center justify-center mx-auto mb-3">
                  <FileText className="w-6 h-6 text-white" />
                </div>
                <h4 className="font-semibold text-black mb-2">OCR Processing</h4>
                <p className="text-gray-600 text-sm">
                  Advanced OCR technology extracts text from scanned documents and images
                </p>
              </div>
              <div className="text-center">
                <div className="w-12 h-12 bg-orange-500 rounded-lg flex items-center justify-center mx-auto mb-3">
                  <Upload className="w-6 h-6 text-white" />
                </div>
                <h4 className="font-semibold text-black mb-2">Metadata Extraction</h4>
                <p className="text-gray-600 text-sm">Automatically extract dates, customer IDs, and document types</p>
              </div>
              <div className="text-center">
                <div className="w-12 h-12 bg-orange-500 rounded-lg flex items-center justify-center mx-auto mb-3">
                  <Batch className="w-6 h-6 text-white" />
                </div>
                <h4 className="font-semibold text-black mb-2">GED Integration</h4>
                <p className="text-gray-600 text-sm">Seamless integration with your document management system</p>
              </div>
            </div>
          </div>
        </div>
      </main>

      {/* Document Preview Modal */}
      {showPreview && (
        <DocumentPreviewModal
          documents={processedDocuments}
          isOpen={showPreview}
          onClose={() => setShowPreview(false)}
        />
      )}
    </div>
  )
}
