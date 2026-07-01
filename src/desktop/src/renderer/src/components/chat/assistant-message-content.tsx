import type { ChatMessageStatus } from '@/lib/chat/types'
import { MarkdownDocument } from '@/components/chat/markdown-document'

type AssistantMessageContentProps = {
  content: string
  status: ChatMessageStatus
  showPlaceholder?: boolean
}

export function AssistantMessageContent({
  content,
  status,
  showPlaceholder = false
}: AssistantMessageContentProps): React.JSX.Element {
  if (showPlaceholder) {
    return <span>…</span>
  }

  if (status === 'processing' || status === 'streaming') {
    return <span className="whitespace-pre-wrap">{content}</span>
  }

  return <MarkdownDocument content={content} />
}
