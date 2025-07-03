/* eslint-disable @typescript-eslint/no-explicit-any */
"use client"

import { useState } from "react"
import { ChevronDown, ChevronRight } from "lucide-react"
import { JSX } from "react"

interface JsonViewerProps {
  data: any
  level?: number
}

interface JsonNodeProps {
  keyName?: string
  value: any
  level: number
  isLast?: boolean
}

function JsonNode({ keyName, value, level, isLast = false }: JsonNodeProps) {
  const [isExpanded, setIsExpanded] = useState(level < 2) // Auto-expand first 2 levels
  const indent = level * 20

  const getValueType = (val: any): string => {
    if (val === null) return "null"
    if (typeof val === "string") return "string"
    if (typeof val === "number") return "number"
    if (typeof val === "boolean") return "boolean"
    if (Array.isArray(val)) return "array"
    if (typeof val === "object") return "object"
    return "unknown"
  }

  const getItemCount = (val: any): string => {
    if (Array.isArray(val)) {
      return val.length === 1 ? "1 item" : `${val.length} items`
    }
    if (typeof val === "object" && val !== null) {
      const count = Object.keys(val).length
      return count === 1 ? "1 item" : `${count} items`
    }
    return ""
  }

  const renderValue = (val: any): JSX.Element => {
    const type = getValueType(val)

    switch (type) {
      case "string":
        return <span className="text-red-600 font-medium">"{val}"</span>
      case "number":
        return <span className="text-blue-600 font-medium">{val}</span>
      case "boolean":
        return <span className="text-purple-600 font-medium">{val.toString()}</span>
      case "null":
        return <span className="text-gray-500 font-medium">null</span>
      default:
        return <span className="text-red-600 font-medium">{String(val)}</span>
    }
  }

  const isExpandable = (val: any): boolean => {
    return (typeof val === "object" && val !== null) || Array.isArray(val)
  }

  if (!isExpandable(value)) {
    return (
      <div className="flex items-center" style={{ paddingLeft: `${indent}px` }}>
        {keyName && (
          <>
            <span className="text-gray-800 font-medium">"{keyName}"</span>
            <span className="text-gray-600 mx-2">:</span>
          </>
        )}
        {renderValue(value)}
        {!isLast && <span className="text-gray-600">,</span>}
      </div>
    )
  }

  const itemCount = getItemCount(value)
  const isArray = Array.isArray(value)
  const entries = isArray ? value.map((item, index) => [index, item]) : Object.entries(value)

  return (
    <div>
      <div
        className="flex items-center cursor-pointer hover:bg-gray-50 py-1"
        style={{ paddingLeft: `${indent}px` }}
        onClick={() => setIsExpanded(!isExpanded)}
      >
        {isExpanded ? (
          <ChevronDown className="w-3 h-3 text-gray-500 mr-1" />
        ) : (
          <ChevronRight className="w-3 h-3 text-gray-500 mr-1" />
        )}
        {keyName && (
          <>
            <span className="text-gray-800 font-medium">"{keyName}"</span>
            <span className="text-gray-600 mx-2">:</span>
          </>
        )}
        <span className="text-gray-600">{isArray ? "[" : "{"}</span>
        {!isExpanded && (
          <>
            <span className="text-gray-400 italic ml-2 text-sm">{itemCount}</span>
            <span className="text-gray-600 ml-1">{isArray ? "]" : "}"}</span>
          </>
        )}
      </div>

      {isExpanded && (
        <>
          {entries.map(([key, val], index) => (
            <JsonNode
              key={key}
              keyName={isArray ? undefined : String(key)}
              value={val}
              level={level + 1}
              isLast={index === entries.length - 1}
            />
          ))}
          <div className="flex items-center" style={{ paddingLeft: `${indent}px` }}>
            <span className="text-gray-600">{isArray ? "]" : "}"}</span>
            {!isLast && <span className="text-gray-600">,</span>}
          </div>
        </>
      )}
    </div>
  )
}

export function JsonViewer({ data, level = 0 }: JsonViewerProps) {
  return (
    <div className="font-mono text-sm bg-white border rounded-lg p-4 overflow-auto max-h-96">
      <div className="flex items-center mb-2">
        <ChevronDown className="w-3 h-3 text-gray-500 mr-1" />
        <span className="text-gray-600">{"{"}</span>
        <span className="text-gray-400 italic ml-2 text-sm">{Object.keys(data).length} items</span>
      </div>
      {Object.entries(data).map(([key, value], index) => (
        <JsonNode key={key} keyName={key} value={value} level={1} isLast={index === Object.keys(data).length - 1} />
      ))}
      <div className="text-gray-600">{"}"}</div>
    </div>
  )
}
