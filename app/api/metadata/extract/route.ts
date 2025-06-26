import { type NextRequest, NextResponse } from "next/server"

// Mock OCR function - replace with actual OCR service
function performOCR(filename: string, fileBuffer: Buffer): string {
  const mockTexts = [
    `FACTURE / INVOICE
Orange Cameroon
Date: ${new Date().toISOString().split("T")[0]}
N° Facture: FAC-${Math.floor(Math.random() * 10000)
      .toString()
      .padStart(4, "0")}
Client ID: CLI-${Math.floor(Math.random() * 1000)
      .toString()
      .padStart(3, "0")}
Montant Total: ${(Math.random() * 1000 + 100).toFixed(2)} FCFA
Service: Abonnement Mobile
Période: ${new Date().toLocaleDateString("fr-FR")}`,

    `CONTRAT DE SERVICE
Orange Cameroon S.A.
Date de signature: ${new Date().toISOString().split("T")[0]}
Référence: CTR-${Math.floor(Math.random() * 10000)
      .toString()
      .padStart(4, "0")}
ID Client: ${Math.floor(Math.random() * 100000)
      .toString()
      .padStart(5, "0")}
Type de contrat: Abonnement Entreprise
Durée: 24 mois
Montant mensuel: ${(Math.random() * 500 + 50).toFixed(2)} FCFA`,

    `REÇU DE PAIEMENT
Orange Money
Date: ${new Date().toISOString().split("T")[0]}
N° Transaction: TXN-${Math.floor(Math.random() * 1000000)
      .toString()
      .padStart(6, "0")}
Client: ${Math.floor(Math.random() * 100000)
      .toString()
      .padStart(5, "0")}
Montant: ${(Math.random() * 200 + 10).toFixed(2)} FCFA
Service: Transfert d'argent
Statut: Confirmé`,
  ]

  return mockTexts[Math.floor(Math.random() * mockTexts.length)]
}

// Enhanced metadata extraction with Orange Cameroon specific patterns
function extractMetadata(text: string, filename: string): Record<string, any> {
  const metadata: Record<string, any> = {
    filename,
    processedDate: new Date().toISOString(),
    documentType: "unknown",
    extractedText: text,
    confidence: Math.random() * 0.3 + 0.7, // Mock confidence score
    language: "fr", // French for Cameroon
    company: "Orange Cameroon",
  }

  // Extract dates (multiple formats for Cameroon)
  const datePatterns = [
    /Date[:\s]*(\d{4}-\d{2}-\d{2})/i,
    /Date[:\s]*(\d{2}\/\d{2}\/\d{4})/i,
    /Date[:\s]*(\d{2}-\d{2}-\d{4})/i,
    /(\d{2}\/\d{2}\/\d{4})/,
    /(\d{4}-\d{2}-\d{2})/,
  ]

  for (const pattern of datePatterns) {
    const match = text.match(pattern)
    if (match) {
      metadata.documentDate = match[1]
      break
    }
  }

  // Extract Orange Cameroon specific patterns
  const clientIdMatch = text.match(/(?:Client|ID Client|Client ID)[:\s]*([A-Z0-9-]+)/i)
  if (clientIdMatch) {
    metadata.clientId = clientIdMatch[1]
  }

  // Extract invoice/contract/receipt numbers
  const invoiceMatch = text.match(/(?:N°\s*)?(?:Facture|Invoice)[:\s#]*([A-Z0-9-]+)/i)
  const contractMatch = text.match(/(?:Référence|Contract|Contrat)[:\s]*([A-Z0-9-]+)/i)
  const receiptMatch = text.match(/(?:N°\s*)?(?:Transaction|Reçu)[:\s]*([A-Z0-9-]+)/i)

  if (invoiceMatch) {
    metadata.documentType = "facture"
    metadata.documentNumber = invoiceMatch[1]
  } else if (contractMatch) {
    metadata.documentType = "contrat"
    metadata.documentNumber = contractMatch[1]
  } else if (receiptMatch) {
    metadata.documentType = "recu"
    metadata.documentNumber = receiptMatch[1]
  }

  // Extract amounts (FCFA currency)
  const amountPatterns = [/(?:Montant|Total|Amount)[:\s]*([0-9,]+\.?\d*)\s*FCFA/i, /([0-9,]+\.?\d*)\s*FCFA/i]

  for (const pattern of amountPatterns) {
    const match = text.match(pattern)
    if (match) {
      metadata.amount = match[1]
      metadata.currency = "FCFA"
      break
    }
  }

  // Extract service type
  const serviceMatch = text.match(/Service[:\s]*([^\n\r]+)/i)
  if (serviceMatch) {
    metadata.serviceType = serviceMatch[1].trim()
  }

  // Extract phone numbers (Cameroon format)
  const phoneMatch = text.match(/(?:\+237|237)?[\s-]?([67]\d{8})/i)
  if (phoneMatch) {
    metadata.phoneNumber = phoneMatch[1]
  }

  return metadata
}

export async function POST(request: NextRequest) {
  try {
    const formData = await request.formData()
    const file = formData.get("file") as File

    if (!file) {
      return NextResponse.json({ error: "No file provided" }, { status: 400 })
    }

    // Validate file type
    const allowedTypes = ["application/pdf", "image/jpeg", "image/jpg", "image/png"]
    if (!allowedTypes.includes(file.type)) {
      return NextResponse.json(
        {
          error: "Invalid file type. Only PDF, JPG, and PNG files are supported.",
        },
        { status: 400 },
      )
    }

    // Validate file size (max 10MB)
    if (file.size > 10 * 1024 * 1024) {
      return NextResponse.json(
        {
          error: "File too large. Maximum size is 10MB.",
        },
        { status: 400 },
      )
    }

    // Convert file to buffer
    const bytes = await file.arrayBuffer()
    const buffer = Buffer.from(bytes)

    // Simulate processing delay
    await new Promise((resolve) => setTimeout(resolve, 2000))

    // For PDFs, we'd convert to image here. For demo, we'll use a placeholder
    const imageUrl = file.type.includes("pdf")
      ? `/placeholder.svg?height=800&width=600&text=PDF+Preview+${encodeURIComponent(file.name)}`
      : `data:${file.type};base64,${buffer.toString("base64")}`

    // Perform OCR
    const extractedText = performOCR(file.name, buffer)

    // Extract metadata
    const metadata = extractMetadata(extractedText, file.name)

    const document = {
      id: `doc_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      filename: file.name,
      imageUrl,
      metadata,
      status: "processed",
      processedAt: new Date().toISOString(),
    }

    return NextResponse.json({
      success: true,
      document,
      message: `Successfully processed document: ${file.name}`,
    })
  } catch (error) {
    console.error("Error processing document:", error)
    return NextResponse.json(
      {
        error: "Failed to process document",
        details: error instanceof Error ? error.message : "Unknown error",
      },
      { status: 500 },
    )
  }
}
