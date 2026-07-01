import { ClipboardCopyIcon, LoaderCircleIcon, XIcon } from 'lucide-react'

import { MarkdownDocument } from '@/components/chat/markdown-document'
import { Button } from '@/components/ui/button'
import { ScrollArea } from '@/components/ui/scroll-area'
import {
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle
} from '@/components/ui/empty'

type PlanPreviewPanelProps = {
  title: string
  content: string
  isStreaming: boolean
  isLoading: boolean
  planId: string | null
  isFromApi: boolean
  onClose: () => void
}

export function PlanPreviewPanel({
  title,
  content,
  isStreaming,
  isLoading,
  planId,
  isFromApi,
  onClose
}: PlanPreviewPanelProps): React.JSX.Element {
  async function handleCopyPlanId(): Promise<void> {
    if (!planId) {
      return
    }

    await navigator.clipboard.writeText(planId)
  }

  return (
    <div className="flex h-full min-h-0 flex-col border-l bg-background">
      <header className="flex h-14 shrink-0 items-center gap-2 border-b px-4">
        <div className="min-w-0 flex-1">
          <h2 className="truncate text-sm font-semibold">{title}</h2>
          {isFromApi && planId ? (
            <p className="text-muted-foreground truncate font-mono text-xs">{planId}</p>
          ) : (
            <p className="text-muted-foreground text-xs">Plan preview</p>
          )}
        </div>
        {isFromApi && planId ? (
          <Button
            size="icon-sm"
            variant="ghost"
            onClick={() => void handleCopyPlanId()}
            aria-label="Copy plan ID"
          >
            <ClipboardCopyIcon />
          </Button>
        ) : null}
        <Button size="icon-sm" variant="ghost" onClick={onClose} aria-label="Close plan panel">
          <XIcon />
        </Button>
      </header>

      <ScrollArea className="min-h-0 flex-1">
        <div className="px-4 py-4">
          {isLoading ? (
            <div className="text-muted-foreground flex items-center gap-2 text-sm">
              <LoaderCircleIcon className="size-4 animate-spin" />
              <span>Loading plan…</span>
            </div>
          ) : content.length === 0 ? (
            <Empty className="border-none">
              <EmptyHeader>
                <EmptyMedia variant="icon">
                  <LoaderCircleIcon className="size-5 animate-spin" />
                </EmptyMedia>
                <EmptyTitle>Waiting for plan</EmptyTitle>
                <EmptyDescription>
                  Plan content will appear here as the assistant responds.
                </EmptyDescription>
              </EmptyHeader>
            </Empty>
          ) : isStreaming ? (
            <pre className="whitespace-pre-wrap text-sm leading-relaxed">{content}</pre>
          ) : (
            <MarkdownDocument content={content} />
          )}
        </div>
      </ScrollArea>
    </div>
  )
}
