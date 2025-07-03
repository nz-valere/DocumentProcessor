import { type NextRequest, NextResponse } from "next/server";

const BACKEND_URL = process.env.BACKEND_API_URL;

export async function POST(request: NextRequest) {
  if (!BACKEND_URL) {
    console.error("BACKEND_API_URL environment variable is not set.");
    return NextResponse.json(
      { error: "Server configuration error." },
      { status: 500 }
    );
  }

  try {
    const requestFormData = await request.formData();
    const file = requestFormData.get("file") as File;

    // You can also get the optional documentType if your frontend sends it
    const documentType = requestFormData.get("documentType") as string | null;

    if (!file) {
      return NextResponse.json({ error: "No file provided." }, { status: 400 });
    }

    // --- Backend Integration ---
    const backendFormData = new FormData();
    backendFormData.append("file", file);

    // Construct backend URL. If a documentType is provided, add it as a query parameter.
    const apiUrl = new URL(`${BACKEND_URL}/api/Metadata/extract`);
    if (documentType) {
      apiUrl.searchParams.append("documentType", documentType);
    }
    
    console.log(`Forwarding request to backend: ${apiUrl.toString()}`);

    // Call the C# backend endpoint.
    const backendResponse = await fetch(apiUrl.toString(), {
      method: "POST",
      body: backendFormData,
      // Headers are not explicitly set to 'multipart/form-data'; 
      // fetch does this automatically when the body is a FormData instance.
    });

    // Handle non-successful responses from the backend
    if (!backendResponse.ok) {
      const errorBody = await backendResponse.json();
      console.error("Backend returned an error:", errorBody);
      return NextResponse.json(
        {
          error: "Failed to process document via backend.",
          details: errorBody.title || errorBody.error || "Unknown backend error.",
        },
        { status: backendResponse.status }
      );
    }

    // Get the successful JSON response from the backend
    const backendData = await backendResponse.json();

    // --- Re-format the response to match the original mock's structure ---
    const finalResponse = {
      success: true,
      document: {
        id: `doc_${Date.now()}`,
        filename: file.name,
        // The backend response is the metadata itself
        metadata: backendData.metadata, 
        // You can add other fields from the backend response as needed
        validation: backendData.validationResult,
        statistics: backendData.extractionStatistics,
        ocrService: backendData.ocrServiceUsed,
        status: "processed",
        processedAt: new Date().toISOString(),
      },
      message: `Successfully processed document: ${file.name}`,
    };

    return NextResponse.json(finalResponse);

  } catch (error) {
    console.error("Error in Next.js API route:", error);
    return NextResponse.json(
      {
        error: "Failed to process document.",
        details: error instanceof Error ? error.message : "An unknown error occurred.",
      },
      { status: 500 }
    );
  }
}