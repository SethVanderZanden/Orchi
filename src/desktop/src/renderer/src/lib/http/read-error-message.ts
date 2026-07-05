export type ApiErrorBody = {
  message?: string
  Message?: string
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export type ReadErrorMessageOptions = {
  /** Transform first validation error or detail (e.g. Mode.Busy friendly text) */
  formatMessage?: (message: string, code?: string) => string
}

export function formatChatModeUpdateError(message: string, code?: string): string {
  if (
    code === 'Mode.Busy' ||
    message.includes('agent is running') ||
    message.startsWith('Mode.Busy')
  ) {
    return 'Wait for the agent to finish before changing mode.'
  }

  return message
}

export function formatChatModelUpdateError(message: string, code?: string): string {
  if (
    code === 'Model.Busy' ||
    message.includes('agent is running') ||
    message.startsWith('Model.Busy')
  ) {
    return 'Wait for the agent to finish before changing model.'
  }

  return message
}

function applyFormat(
  message: string,
  code: string | undefined,
  formatMessage: ReadErrorMessageOptions['formatMessage']
): string {
  return formatMessage ? formatMessage(message, code) : message
}

export async function readErrorMessage(
  response: Response,
  options?: ReadErrorMessageOptions
): Promise<string> {
  const formatMessage = options?.formatMessage

  try {
    const body = (await response.json()) as ApiErrorBody

    if (body.errors) {
      const messages = Object.values(body.errors).flat()

      if (messages.length > 0) {
        return applyFormat(messages[0]!, body.title, formatMessage)
      }
    }

    if (body.detail) {
      return applyFormat(body.detail, body.title, formatMessage)
    }

    return body.message ?? body.Message ?? `API error: ${response.status}`
  } catch {
    return `API error: ${response.status}`
  }
}
