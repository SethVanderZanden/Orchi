import { memo } from 'react'

import { MarkdownContent } from '@/components/markdown-content'
import { cn } from '@/lib/utils'
import type { ChatMessage } from '@/lib/chat/types'

type AssistantMessageContentProps = {
  content: string
  status: ChatMessage['status']
  className?: string
  workspacePath?: string | null
}

/**
 * Renders assistant message text. Uses plain text while streaming to avoid
 * re-parsing partial markdown on every token (major source of UI jank).
 */
export const AssistantMessageContent = memo(function AssistantMessageContent({
  content,
  status,
  className,
  workspacePath
}: AssistantMessageContentProps): React.JSX.Element {
  const isStreaming = status === 'processing' || status === 'streaming'

  if (isStreaming) {
    return (
      <div className={cn('whitespace-pre-wrap break-words text-inherit', className)}>{content}</div>
    )
  }

  return (
    <MarkdownContent className={className} workspacePath={workspacePath}>
      {content}
    </MarkdownContent>
  )
})
