import { memo } from 'react'

import { MarkdownContent } from '@/components/markdown-content'
import { spreadAssistantMarkdown } from '@/lib/chat/spread-assistant-markdown'
import type { ChatMessage } from '@/lib/chat/types'
import { cn } from '@/lib/utils'

type AssistantMessageContentProps = {
  content: string
  status: ChatMessage['status']
  className?: string
}

/**
 * Renders assistant message text. Uses plain text while streaming to avoid
 * re-parsing partial markdown on every token (major source of UI jank).
 */
export const AssistantMessageContent = memo(function AssistantMessageContent({
  content,
  status,
  className
}: AssistantMessageContentProps): React.JSX.Element {
  const isStreaming = status === 'processing' || status === 'streaming'

  if (isStreaming) {
    return (
      <div className={cn('whitespace-pre-wrap break-words leading-7 text-inherit', className)}>
        {content}
      </div>
    )
  }

  return (
    <MarkdownContent className={className}>{spreadAssistantMarkdown(content)}</MarkdownContent>
  )
})
