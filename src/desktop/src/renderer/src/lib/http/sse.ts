export type SseEvent = {
  event: string
  data: string
}

/** Parse one SSE block (events separated by blank line) */
export function parseSseBlock(block: string): SseEvent | null {
  const normalized = block.replace(/\r\n/g, '\n').trim()
  if (!normalized) {
    return null
  }

  let eventName = 'message'
  const dataLines: string[] = []

  for (const line of normalized.split('\n')) {
    if (!line || line.startsWith(':')) {
      continue
    }

    if (line.startsWith('event:')) {
      eventName = line.slice('event:'.length).trim()
      continue
    }

    if (line.startsWith('data:')) {
      dataLines.push(line.slice('data:'.length).trim())
    }
  }

  if (dataLines.length === 0) {
    return null
  }

  return { event: eventName, data: dataLines.join('\n') }
}

/** Read a fetch Response body and invoke callback per parsed event */
export async function readSseStream(
  response: Response,
  onEvent: (event: SseEvent) => void,
  signal?: AbortSignal
): Promise<void> {
  if (!response.body) {
    throw new Error('Streaming response body was empty.')
  }

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''

  try {
    while (true) {
      if (signal?.aborted) {
        break
      }

      const { done, value } = await reader.read()
      if (done) {
        break
      }

      buffer += decoder.decode(value, { stream: true })

      while (true) {
        const boundary = buffer.indexOf('\n\n')
        if (boundary === -1) {
          break
        }

        const rawBlock = buffer.slice(0, boundary)
        buffer = buffer.slice(boundary + 2)
        const parsed = parseSseBlock(rawBlock)
        if (parsed) {
          onEvent(parsed)
        }
      }
    }
  } finally {
    reader.releaseLock()
  }
}
