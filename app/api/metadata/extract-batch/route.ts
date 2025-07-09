import { type NextRequest, NextResponse } from "next/server"

const BACKEND_URL = process.env.BACKEND_API_URL

export async function POST(request: NextRequest) {
  if (!BACKEND_URL) {
    console.error("BACKEND_API_URL environment variable is not set.")
    return NextResponse.json(
      { error: "Server configuration error." },
      { status: 500 }
    )
  }

  try {
    const formData = await request.formData()
    const files = formData.getAll("files") as File[]
    const documentType = formData.get("documentType") as string | null

    if (!files || files.length === 0) {
      return NextResponse.json({ error: "No files provided" }, { status: 400 })
    }

    // Validate batch size (max 10 files to match controller limit)
    if (files.length > 10) {
      return NextResponse.json(
        {
          error: "Too many files. Maximum batch size is 10 files.",
        },
        { status: 400 }
      )
    }

    // Prepare FormData for backend
    const backendFormData = new FormData()
    
    // Add all files to the FormData
    files.forEach((file) => {
      backendFormData.append("files", file)
    })

    // Construct backend URL with documentType query parameter if provided
    const apiUrl = new URL(`${BACKEND_URL}/api/Metadata/extract-batch`)
    if (documentType) {
      apiUrl.searchParams.append("documentType", documentType)
    }

    console.log(`Forwarding batch request to backend: ${apiUrl.toString()}`)

    const backendResponse = await fetch(apiUrl.toString(), {
      method: "POST",
      body: backendFormData,
    })

    // Handle non-successful responses from the backend
    if (!backendResponse.ok) {
      const errorBody = await backendResponse.json()
      console.error("Backend returned an error:", errorBody)
      return NextResponse.json(
        {
          error: "Failed to process batch documents via backend.",
          details: errorBody.title || errorBody.error || errorBody.message || "Unknown backend error.",
        },
        { status: backendResponse.status }
      )
    }

    // Get the successful JSON response from the backend
    const backendData = await backendResponse.json()

    // Transform backend response to match frontend expectations
    const processedDocuments = backendData.results.map((result: any, index: number) => {
      // Generate image URL for frontend display
      const file = files[index]
      const imageUrl = file?.type.includes("pdf")
        ? `/placeholder.svg?height=800&width=600&text=PDF+Preview+${encodeURIComponent(file.name)}`
        : file ? `data:${file.type};base64,${Buffer.from(new Uint8Array(file.stream())).toString("base64")}` : null

      return {
        id: `doc_${Date.now()}_${index}_${Math.random().toString(36).substr(2, 9)}`,
        filename: result.fileName,
        imageUrl,
        metadata: result.metadata,
        validation: result.validationResult,
        statistics: result.extractionStatistics,
        ocrService: result.ocrServiceUsed,
        status: result.validationResult?.isValid ? "processed" : "processed_with_errors",
        processedAt: result.processedAt || new Date().toISOString(),
        batchIndex: index,
      }
    })

    // Collect any errors from files that couldn't be processed
    const errors: { filename: string; error: string }[] = []
    const processedFileNames = new Set(backendData.results.map((r: any) => r.fileName))
    
    files.forEach((file) => {
      if (!processedFileNames.has(file.name)) {
        errors.push({
          filename: file.name,
          error: "File was skipped during processing (likely due to size or type restrictions)",
        })
      }
    })

    // Also check for validation errors in processed documents
    backendData.results.forEach((result: any) => {
      if (!result.validationResult?.isValid) {
        errors.push({
          filename: result.fileName,
          error: result.validationResult?.messages?.join(", ") || "Validation failed",
        })
      }
    })

    const response = {
      success: true,
      documents: processedDocuments,
      totalProcessed: processedDocuments.length,
      totalFiles: files.length,
      message: `Successfully processed ${processedDocuments.length} out of ${files.length} documents`,
      summary: backendData.summary,
    }

    // Include errors if any
    if (errors.length > 0) {
      response.errors = errors
      response.message += `. ${errors.length} files had issues during processing.`
    }

    return NextResponse.json(response)

  } catch (error) {
    console.error("Error in Next.js batch API route:", error)
    return NextResponse.json(
      {
        error: "Failed to process batch documents",
        details: error instanceof Error ? error.message : "Unknown error",
      },
      { status: 500 }
    )
  }
}