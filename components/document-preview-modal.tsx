"use client"

import { useState } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import { ChevronLeft, ChevronRight, Download, Send, X } from "lucide-react"
import { useToast } from "@/hooks/use-toast"

interface ProcessedDocument {
  id: string
  filename: string
  imageUrl: string
  metadata: Record<string, any>
}

interface DocumentPreviewModalProps {
  documents: ProcessedDocument[]
  isOpen: boolean
  onClose: () => void
}

export function DocumentPreviewModal({ documents, isOpen, onClose }: DocumentPreviewModalProps) {
  const [currentIndex, setCurrentIndex] = useState(0)
  const [editedMetadata, setEditedMetadata] = useState<Record<string, any>>(documents[0]?.metadata || {})
  const [isSubmitting, setIsSubmitting] = useState(false)
  const { toast } = useToast()

  const currentDocument = documents[currentIndex]

  const handlePrevious = () => {
    if (currentIndex > 0) {
      const newIndex = currentIndex - 1
      setCurrentIndex(newIndex)
      setEditedMetadata(documents[newIndex].metadata)
    }
  }

  const handleNext = () => {
    if (currentIndex < documents.length - 1) {
      const newIndex = currentIndex + 1
      setCurrentIndex(newIndex)
      setEditedMetadata(documents[newIndex].metadata)
    }
  }

  const handleMetadataChange = (value: string) => {
    try {
      const parsed = JSON.parse(value)
      setEditedMetadata(parsed)
    } catch (error) {
      // Invalid JSON, keep the raw string for editing
    }
  }

  const handleDownloadMetadata = () => {
    const dataStr = JSON.stringify(editedMetadata, null, 2)
    const dataBlob = new Blob([dataStr], { type: "application/json" })
    const url = URL.createObjectURL(dataBlob)
    const link = document.createElement("a")
    link.href = url
    link.download = `${currentDocument.filename}_metadata.json`
    link.click()
    URL.revokeObjectURL(url)

    toast({
      title: "Metadata downloaded",
      description: "The metadata JSON file has been downloaded successfully.",
    })
  }

  const handleSubmitToGED = async () => {
    setIsSubmitting(true)

    try {
      const response = await fetch("/api/submit-to-ged", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          documentId: currentDocument.id,
          metadata: editedMetadata,
          filename: currentDocument.filename,
        }),
      })

      if (!response.ok) {
        throw new Error("Failed to submit to GED")
      }

      const result = await response.json()

      toast({
        title: "Successfully submitted to GED",
        description: `Document "${currentDocument.filename}" has been processed and sent to the GED system.`,
      })

      // Mark document as submitted (you might want to update the document status)
      // and potentially move to next document or close modal
    } catch (error) {
      toast({
        title: "Error submitting to GED",
        description: "Please try again or contact support.",
        variant: "destructive",
      })
    } finally {
      setIsSubmitting(false)
    }
  }

  if (!currentDocument) return null

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-7xl max-h-[90vh] overflow-hidden">
        <DialogHeader className="flex flex-row items-center justify-between">
          <DialogTitle className="text-xl font-bold text-black">
            Document Preview - {currentDocument.filename}
          </DialogTitle>
          <div className="flex items-center space-x-2">
            {documents.length > 1 && (
              <div className="flex items-center space-x-2">
                <Button variant="outline" size="sm" onClick={handlePrevious} disabled={currentIndex === 0}>
                  <ChevronLeft className="w-4 h-4" />
                </Button>
                <span className="text-sm text-gray-600">
                  {currentIndex + 1} of {documents.length}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleNext}
                  disabled={currentIndex === documents.length - 1}
                >
                  <ChevronRight className="w-4 h-4" />
                </Button>
              </div>
            )}
            <Button variant="ghost" size="sm" onClick={onClose}>
              <X className="w-4 h-4" />
            </Button>
          </div>
        </DialogHeader>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 h-[70vh]">
          {/* Document Preview */}
          <div className="border rounded-lg overflow-hidden">
            <div className="bg-gray-100 px-4 py-2 border-b">
              <h3 className="font-semibold text-black">Document Preview</h3>
            </div>
            <div className="p-4 h-full overflow-auto">
              <img
                src={currentDocument.imageUrl || "/placeholder.svg"}
                alt={currentDocument.filename}
                className="w-full h-auto border rounded shadow-sm"
                style={{ maxHeight: "600px", objectFit: "contain" }}
              />
            </div>
          </div>

          {/* Metadata Panel */}
          <div className="border rounded-lg overflow-hidden">
            <div className="bg-gray-100 px-4 py-2 border-b">
              <h3 className="font-semibold text-black">Extracted Metadata</h3>
              <p className="text-sm text-gray-600">Edit the JSON below to modify metadata</p>
            </div>
            <div className="p-4 h-full flex flex-col">
              <Textarea
                value={JSON.stringify(editedMetadata, null, 2)}
                onChange={(e) => handleMetadataChange(e.target.value)}
                className="flex-1 font-mono text-sm"
                placeholder="Metadata JSON..."
              />

              <div className="flex space-x-2 mt-4">
                <Button onClick={handleDownloadMetadata} variant="outline" className="flex-1">
                  <Download className="w-4 h-4 mr-2" />
                  Download JSON
                </Button>
                <Button
                  onClick={handleSubmitToGED}
                  disabled={isSubmitting}
                  className="flex-1 bg-orange-500 hover:bg-orange-600 text-white"
                >
                  {isSubmitting ? (
                    <>Processing...</>
                  ) : (
                    <>
                      <Send className="w-4 h-4 mr-2" />
                      Send to GED
                    </>
                  )}
                </Button>
              </div>
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
