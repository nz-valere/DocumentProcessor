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
    const documentType = requestFormData.get("documentType") as string | null;

    if (!file) {
      return NextResponse.json({ error: "No file provided." }, { status: 400 });
    }

    const backendFormData = new FormData();
    backendFormData.append("file", file);

    const apiUrl = new URL(`${BACKEND_URL}/api/Metadata/extract`);
    // CORRECTED: The .NET model binder looks for the parameter from the form, not query string.
    if (documentType) {
       backendFormData.append("documentType", documentType);
    }
    
    console.log(`Forwarding request to backend: ${apiUrl.toString()}`);

    const backendResponse = await fetch(apiUrl.toString(), {
      method: "POST",
      body: backendFormData,
    });

    // --- START OF FIX ---
    // Handle non-successful responses from the backend
    if (!backendResponse.ok) {
      let errorDetails = "Unknown backend error.";
      const contentType = backendResponse.headers.get("content-type");

      // Check if the response is JSON or plain text and parse accordingly
      if (contentType && contentType.includes("application/json")) {
        const errorBody = await backendResponse.json();
        errorDetails = errorBody.title || errorBody.error || errorBody.message || JSON.stringify(errorBody);
      } else {
        errorDetails = await backendResponse.text();
      }
      
      console.error("Backend returned an error:", errorDetails);

      // Forward the actual error and status from the backend
      return NextResponse.json(
        {
          error: "Failed to process document via backend.",
          details: errorDetails,
        },
        { status: backendResponse.status }
      );
    }
    // --- END OF FIX ---

    const backendData = await backendResponse.json();

    const finalResponse = {
      success: true,
      document: {
        id: `doc_${Date.now()}`,
        filename: file.name,
        metadata: backendData.metadata, 
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

// I've also applied the same robust error handling to your GET function.
export async function GET() {
  if (!BACKEND_URL) {
    console.error("BACKEND_API_URL environment variable is not set.");
    return NextResponse.json(
      { error: "Server configuration error." },
      { status: 500 }
    );
  }
  
  try{
    const apiUrl = new URL(`${BACKEND_URL}/api/Metadata/document-types`);
    console.log(`Fetching document types from backend: ${apiUrl.toString()}`);
    
    const backendResponse = await fetch(apiUrl.toString(), {
      method: "GET",
    });

    if (!backendResponse.ok) {
        let errorDetails = "Unknown backend error.";
        const contentType = backendResponse.headers.get("content-type");

        if (contentType && contentType.includes("application/json")) {
            const errorBody = await backendResponse.json();
            errorDetails = errorBody.title || errorBody.error || "Unknown backend error.";
        } else {
            errorDetails = await backendResponse.text();
        }

        console.error("Backend returned an error:", errorDetails);
        return NextResponse.json(
            {
                error: "Failed to fetch document types from backend.",
                details: errorDetails,
            },
            { status: backendResponse.status }
        );
    }
    
    const documentTypes = await backendResponse.json();
    return NextResponse.json({ documentTypes });

  } catch (error) {
    console.error("Error fetching document types:", error);
    return NextResponse.json(
      { error: "An internal error occurred while fetching document types." },
      { status: 500 }
    );
  }
}