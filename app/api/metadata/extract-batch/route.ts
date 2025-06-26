import { type NextRequest, NextResponse } from "next/server"

// Mock OCR function - same as single extract
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

    `RAPPORT MENSUEL
Orange Cameroon
Période: ${new Date().toLocaleDateString("fr-FR")}
Référence: RPT-${Math.floor(Math.random() * 10000)
      .toString()
      .padStart(4, "0")}
Nombre d'abonnés: ${Math.floor(Math.random() * 10000 + 1000)}
Revenus: ${(Math.random() * 100000 + 10000).toFixed(2)} FCFA
Région: Centre`,

    `BON DE COMMANDE
Orange Business Services
Date: ${new Date().toISOString().split("T")[0]}
N° Commande: CMD-${Math.floor(Math.random() * 10000)
      .toString()
      .padStart(4, "0")}
Client Entreprise: ENT-${Math.floor(Math.random() * 1000)
      .toString()
      .padStart(3, "0")}
Équipements: Routeurs, Modems
Montant: ${(Math.random() * 5000 + 500).toFixed(2)} FCFA`,
  ]

  return mockTexts[Math.floor(Math.random() * mockTexts.length)]
}

// Enhanced metadata extraction - same as single extract
function extractMetadata(text: string, filename: string): Record<string, any> {
  const metadata: Record<string, any> = {
    filename,
    processedDate: new Date().toISOString(),
    documentType: "unknown",
    extractedText: text,
    confidence: Math.random() * 0.3 + 0.7,
    language: "fr",
    company: "Orange Cameroon",
  }

  // Extract dates
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

  // Extract client/customer IDs
  const clientIdMatch = text.match(/(?:Client|ID Client|Client ID|Client Entreprise)[:\s]*([A-Z0-9-]+)/i)
  if (clientIdMatch) {
    metadata.clientId = clientIdMatch[1]
  }

  // Extract document numbers and types
  const patterns = [
    { regex: /(?:N°\s*)?(?:Facture|Invoice)[:\s#]*([A-Z0-9-]+)/i, type: "facture" },
    { regex: /(?:Référence|Contract|Contrat)[:\s]*([A-Z0-9-]+)/i, type: "contrat" },
    { regex: /(?:N°\s*)?(?:Transaction|Reçu)[:\s]*([A-Z0-9-]+)/i, type: "recu" },
    { regex: /(?:N°\s*)?(?:Commande|Order)[:\s]*([A-Z0-9-]+)/i, type: "commande" },
    { regex: /(?:Référence|Report|Rapport)[:\s]*([A-Z0-9-]+)/i, type: "rapport" },
  ]

  for (const pattern of patterns) {
    const match = text.match(pattern.regex)
    if (match) {
      metadata.documentType = pattern.type
      metadata.documentNumber = match[1]
      break
    }
  }

  // Extract amounts
  const amountPatterns = [/(?:Montant|Total|Amount|Revenus)[:\s]*([0-9,]+\.?\d*)\s*FCFA/i, /([0-9,]+\.?\d*)\s*FCFA/i]

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

  // Extract phone numbers
  const phoneMatch = text.match(/(?:\+237|237)?[\s-]?([67]\d{8})/i)
  if (phoneMatch) {
    metadata.phoneNumber = phoneMatch[1]
  }

  // Extract region (for reports)
  const regionMatch = text.match(/Région[:\s]*([^\n\r]+)/i)
  if (regionMatch) {
    metadata.region = regionMatch[1].trim()
  }

  return metadata
}

export async function POST(request: NextRequest) {
  try {
    const formData = await request.formData()
    const files = formData.getAll("files") as File[]

    if (!files || files.length === 0) {
      return NextResponse.json({ error: "No files provided" }, { status: 400 })
    }

    // Validate batch size (max 20 files)
    if (files.length > 20) {
      return NextResponse.json(
        {
          error: "Too many files. Maximum batch size is 20 files.",
        },
        { status: 400 },
      )
    }

    const processedDocuments = []
    const errors = []

    for (let i = 0; i < files.length; i++) {
      const file = files[i]

      try {
        // Validate file type
        const allowedTypes = ["application/pdf", "image/jpeg", "image/jpg", "image/png"]
        if (!allowedTypes.includes(file.type)) {
          errors.push({
            filename: file.name,
            error: "Invalid file type. Only PDF, JPG, and PNG files are supported.",
          })
          continue
        }

        // Validate file size (max 10MB per file)
        if (file.size > 10 * 1024 * 1024) {
          errors.push({
            filename: file.name,
            error: "File too large. Maximum size is 10MB per file.",
          })
          continue
        }

        // Convert file to buffer
        const bytes = await file.arrayBuffer()
        const buffer = Buffer.from(bytes)

        // Simulate processing delay (shorter for batch)
        await new Promise((resolve) => setTimeout(resolve, 1000))

        // Generate image URL
        const imageUrl = file.type.includes("pdf")
          ? `/placeholder.svg?height=800&width=600&text=PDF+Preview+${encodeURIComponent(file.name)}`
          : `data:${file.type};base64,${buffer.toString("base64")}`

        // Perform OCR
        const extractedText = performOCR(file.name, buffer)

        // Extract metadata
        const metadata = extractMetadata(extractedText, file.name)

        const document = {
          id: `doc_${Date.now()}_${i}_${Math.random().toString(36).substr(2, 9)}`,
          filename: file.name,
          imageUrl,
          metadata,
          status: "processed",
          processedAt: new Date().toISOString(),
          batchIndex: i,
        }

        processedDocuments.push(document)
      } catch (error) {
        errors.push({
          filename: file.name,
          error: error instanceof Error ? error.message : "Processing failed",
        })
      }
    }

    const response = {
      success: true,
      documents: processedDocuments,
      totalProcessed: processedDocuments.length,
      totalFiles: files.length,
      message: `Successfully processed ${processedDocuments.length} out of ${files.length} documents`,
    }

    // Include errors if any
    if (errors.length > 0) {
      response.errors = errors
      response.message += `. ${errors.length} files failed to process.`
    }

    return NextResponse.json(response)
  } catch (error) {
    console.error("Error processing batch documents:", error)
    return NextResponse.json(
      {
        error: "Failed to process batch documents",
        details: error instanceof Error ? error.message : "Unknown error",
      },
      { status: 500 },
    )
  }
}
