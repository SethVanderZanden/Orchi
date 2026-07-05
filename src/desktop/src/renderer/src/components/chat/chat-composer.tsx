import { useState } from 'react'
import { ArrowUp } from 'lucide-react'

import { ChatModeBadge } from '@/components/chat/chat-mode-badge'
import { ChatModeDropdown } from '@/components/chat/chat-mode-dropdown'
import { ChatModelSelector } from '@/components/chat/chat-model-selector'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import type { AgentMode } from '@/lib/chat/types'
import { cn } from '@/lib/utils'

type ChatComposerProps = {
  disabled?: boolean
  onSend: (content: string) => void
  expanded?: boolean
  mode: AgentMode
  showModeControls?: boolean
  canChangeMode?: boolean
  modeUpdateError?: string | null
  onModeChange: (mode: AgentMode) => void
  agentId: string
  modelId: string | null
  canChangeModel?: boolean
  modelUpdateError?: string | null
  onModelChange: (modelId: string | null) => void
}

export function OrchiChatComposer({
  disabled = false,
  onSend,
  expanded = false,
  mode,
  showModeControls = false,
  canChangeMode = false,
  modeUpdateError = null,
  onModeChange,
  agentId,
  modelId,
  canChangeModel = true,
  modelUpdateError = null,
  onModelChange
}: ChatComposerProps): React.JSX.Element {
  const [draft, setDraft] = useState('')

  function handleSubmit(event: React.FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    const content = draft.trim()
    if (!content || disabled) {
      return
    }

    onSend(content)
    setDraft('')
  }

  function handleKeyDown(event: React.KeyboardEvent<HTMLTextAreaElement>): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      event.currentTarget.form?.requestSubmit()
    }
  }

  return (
    <form onSubmit={handleSubmit} className="w-full">
      <div className="rounded-xl border bg-muted/40 shadow-sm">
        <Textarea
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Message Orchi…"
          disabled={disabled}
          rows={expanded ? 4 : 3}
          className={cn(
            'min-h-[88px] max-h-52 resize-none border-0 bg-transparent px-4 py-3 leading-relaxed shadow-none',
            'focus-visible:ring-0 focus-visible:ring-offset-0',
            expanded && 'min-h-[120px]'
          )}
        />
        <div className="flex items-center justify-between gap-2 px-3 pb-3 pt-1">
          <div className="flex min-w-0 flex-wrap items-center gap-1.5">
            {showModeControls ? (
              <>
                <ChatModeBadge
                  mode={mode}
                  disabled={!canChangeMode}
                  onClear={() => onModeChange('default')}
                />
                <ChatModeDropdown
                  mode={mode}
                  disabled={!canChangeMode}
                  onModeChange={onModeChange}
                />
              </>
            ) : null}
            <ChatModelSelector
              agentId={agentId}
              modelId={modelId}
              mode={mode}
              disabled={!canChangeModel}
              error={modelUpdateError}
              onModelChange={onModelChange}
              compact
            />
          </div>
          <Button
            type="submit"
            size="icon"
            disabled={disabled || !draft.trim()}
            aria-label="Send message"
            className="size-8 shrink-0 rounded-full"
          >
            <ArrowUp className="size-4" />
          </Button>
        </div>
        {modeUpdateError ? <p className="sr-only">{modeUpdateError}</p> : null}
      </div>
    </form>
  )
}
