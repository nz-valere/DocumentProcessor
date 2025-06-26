import { type NextRequest, NextResponse } from "next/server"

export async function POST(request: NextRequest) {
  try {
    const { documentId, metadata, filename } = await request.json()

    if (!documentId || !metadata) {
      return NextResponse.json(
        {
          error: "Missing required fields: documentId and metadata",
        },
        { status: 400 },
      )
    }

    // Simulate GED system integration with Orange Cameroon specific processing
    await new Promise((resolve) => setTimeout(resolve, 1500))

    // Transform metadata for GED system
    const gedMetadata = {
      ...metadata,
      submittedBy: "DocuPipe System",
      submissionDate: new Date().toISOString(),
      company: "Orange Cameroon",
      status: "archived",
      // Add GED-specific fields
      classification: metadata.documentType || "general",
      retention_period: getRetentionPeriod(metadata.documentType),
      access_level: "internal",
    }

    const gedResponse = {
      success: true,
      gedDocumentId: `GED_OC_${Date.now()}`,
      status: "processed",
      submittedAt: new Date().toISOString(),
      originalDocumentId: documentId,
      filename,
      metadata: gedMetadata,
    }

    return NextResponse.json({
      success: true,
      message: "Document successfully submitted to GED system",
      gedResponse,
    })
  } catch (error) {
    console.error("Error submitting to GED:", error)
    return NextResponse.json(
      {
        error: "Failed to submit document to GED system",
        details: error instanceof Error ? error.message : "Unknown error",
      },
      { status: 500 },
    )
  }
}

// Helper function to determine retention period based on document type
function getRetentionPeriod(documentType: string): string {
  const retentionPeriods = {
    facture: "7 years",
    contrat: "10 years",
    recu: "3 years",
    rapport: "5 years",
    commande: "5 years",
    default: "3 years",
  }

  return retentionPeriods[documentType] || retentionPeriods.default
}
