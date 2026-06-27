import { useState } from 'react'
import { ArrowUpIcon } from 'lucide-react'

import {
  InputGroup,
  InputGroupAddon,
  InputGroupButton,
  InputGroupTextarea
} from '@/components/ui/input-group'

type ChatComposerProps = {
  disabled?: boolean
  onSend: (content: string) => void
}

export function ChatComposer({ disabled = false, onSend }: ChatComposerProps): React.JSX.Element {
  const [draft, setDraft] = useState('')

  const canSend = draft.trim().length > 0 && !disabled

  function handleSubmit(event: React.FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    if (!canSend) {
      return
    }

    onSend(draft.trim())
    setDraft('')
  }

  function handleKeyDown(event: React.KeyboardEvent<HTMLTextAreaElement>): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      if (!canSend) {
        return
      }

      onSend(draft.trim())
      setDraft('')
    }
  }

  return (
    <form onSubmit={handleSubmit} className="mx-auto w-full max-w-3xl">
      <InputGroup className="min-h-12 rounded-2xl shadow-sm">
        <InputGroupTextarea
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Message Orchi…"
          disabled={disabled}
          rows={1}
          className="max-h-40 min-h-10 py-3 text-sm"
        />
        <InputGroupAddon align="block-end" className="justify-end border-t px-2 py-2">
          <InputGroupButton
            type="submit"
            size="icon-sm"
            variant="default"
            disabled={!canSend}
            aria-label="Send message"
          >
            <ArrowUpIcon />
          </InputGroupButton>
        </InputGroupAddon>
      </InputGroup>
      <p className="text-muted-foreground mt-2 text-center text-xs">
        Enter to send · Shift+Enter for a new line
      </p>
    </form>
  )
}
