/* eslint-disable @typescript-eslint/no-unused-vars */
/* eslint-disable @typescript-eslint/no-explicit-any */
"use client"

import { useState } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs"
import { ChevronLeft, ChevronRight, Download, Send, X, FileText, Eye, BarChart3, Type } from "lucide-react"
import { useToast } from "@/hooks/use-toast"
import { JsonViewer } from "@/components/json-viewer"

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
  const [activeTab, setActiveTab] = useState("metadata")
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

  const getDocumentStats = () => {
    const text = editedMetadata.extractedText || ""
    const words = text.split(/\s+/).filter((word) => word.length > 0)
    const characters = text.length
    const lines = text.split("\n").length

    return {
      characters,
      words: words.length,
      lines,
      confidence: editedMetadata.confidence || 0,
      documentType: editedMetadata.documentType || "unknown",
      language: editedMetadata.language || "unknown",
      processedDate: editedMetadata.processedDate || new Date().toISOString(),
    }
  }

  if (!currentDocument) return null

  const stats = getDocumentStats()

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
            <Button onClick={handleDownloadMetadata} variant="outline" size="sm">
              <Download className="w-4 h-4 mr-2" />
              Download JSON
            </Button>
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

          {/* Tabbed Content Panel */}
          <div className="border rounded-lg overflow-hidden">
            <Tabs value={activeTab} onValueChange={setActiveTab} className="h-full flex flex-col">
              <div className="bg-gray-100 px-4 py-2 border-b">
                <TabsList className="grid w-full grid-cols-4 bg-white">
                  <TabsTrigger value="metadata" className="flex items-center space-x-1">
                    <FileText className="w-3 h-3" />
                    <span className="text-xs">Metadata</span>
                  </TabsTrigger>
                  <TabsTrigger value="fileview" className="flex items-center space-x-1">
                    <Eye className="w-3 h-3" />
                    <span className="text-xs">File View</span>
                  </TabsTrigger>
                  <TabsTrigger value="rawtext" className="flex items-center space-x-1">
                    <Type className="w-3 h-3" />
                    <span className="text-xs">Raw Text</span>
                  </TabsTrigger>
                  <TabsTrigger value="statistics" className="flex items-center space-x-1">
                    <BarChart3 className="w-3 h-3" />
                    <span className="text-xs">Statistics</span>
                  </TabsTrigger>
                </TabsList>
              </div>

              <div className="flex-1 overflow-hidden">
                <TabsContent value="metadata" className="h-full p-4 overflow-auto">
                  <div className="h-full">
                    <h4 className="font-semibold text-black mb-3">Extracted Metadata</h4>
                    <JsonViewer data={editedMetadata} />
                  </div>
                </TabsContent>

                <TabsContent value="fileview" className="h-full p-4 overflow-auto">
                  <div className="h-full">
                    <h4 className="font-semibold text-black mb-3">File View (Page by Page)</h4>
                    <div className="bg-gray-50 rounded-lg p-4 h-full flex items-center justify-center">
                      <div className="text-center">
                        <img
                          src={currentDocument.imageUrl || "/placeholder.svg"}
                          alt={`${currentDocument.filename} - Page 1`}
                          className="max-w-full max-h-96 border rounded shadow-sm mx-auto"
                        />
                        <p className="text-sm text-gray-600 mt-2">Page 1 of 1</p>
                      </div>
                    </div>
                  </div>
                </TabsContent>

                <TabsContent value="rawtext" className="h-full p-4 overflow-auto">
                  <div className="h-full">
                    <h4 className="font-semibold text-black mb-3">Raw Extracted Text</h4>
                    <div className="bg-gray-50 rounded-lg p-4 h-full overflow-auto">
                      <pre className="text-sm text-gray-800 whitespace-pre-wrap font-mono">
                        {editedMetadata.extractedText || "No text extracted"}
                      </pre>
                    </div>
                  </div>
                </TabsContent>

                <TabsContent value="statistics" className="h-full p-4 overflow-auto">
                  <div className="h-full">
                    <h4 className="font-semibold text-black mb-3">Document Statistics</h4>
                    <div className="space-y-4">
                      <div className="grid grid-cols-2 gap-4">
                        <div className="bg-gray-50 rounded-lg p-3">
                          <div className="text-2xl font-bold text-orange-500">{stats.characters}</div>
                          <div className="text-sm text-gray-600">Characters</div>
                        </div>
                        <div className="bg-gray-50 rounded-lg p-3">
                          <div className="text-2xl font-bold text-orange-500">{stats.words}</div>
                          <div className="text-sm text-gray-600">Words</div>
                        </div>
                        <div className="bg-gray-50 rounded-lg p-3">
                          <div className="text-2xl font-bold text-orange-500">{stats.lines}</div>
                          <div className="text-sm text-gray-600">Lines</div>
                        </div>
                        <div className="bg-gray-50 rounded-lg p-3">
                          <div className="text-2xl font-bold text-orange-500">
                            {Math.round(stats.confidence * 100)}%
                          </div>
                          <div className="text-sm text-gray-600">Confidence</div>
                        </div>
                      </div>

                      <div className="space-y-3">
                        <div className="flex justify-between items-center py-2 border-b">
                          <span className="text-sm font-medium text-gray-700">Document Type</span>
                          <span className="text-sm text-gray-900 capitalize">{stats.documentType}</span>
                        </div>
                        <div className="flex justify-between items-center py-2 border-b">
                          <span className="text-sm font-medium text-gray-700">Language</span>
                          <span className="text-sm text-gray-900 uppercase">{stats.language}</span>
                        </div>
                        <div className="flex justify-between items-center py-2 border-b">
                          <span className="text-sm font-medium text-gray-700">Processed Date</span>
                          <span className="text-sm text-gray-900">
                            {new Date(stats.processedDate).toLocaleString()}
                          </span>
                        </div>
                        <div className="flex justify-between items-center py-2 border-b">
                          <span className="text-sm font-medium text-gray-700">File Size</span>
                          <span className="text-sm text-gray-900">
                            {currentDocument.filename.split(".").pop()?.toUpperCase()} Document
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>
                </TabsContent>
              </div>

              <div className="border-t p-4">
                <Button
                  onClick={handleSubmitToGED}
                  disabled={isSubmitting}
                  className="w-full bg-orange-500 hover:bg-orange-600 text-white"
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
            </Tabs>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
