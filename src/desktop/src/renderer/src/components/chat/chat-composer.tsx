import { useState } from 'react'
import { Send } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'

type ChatComposerProps = {
  disabled?: boolean
  onSend: (content: string) => void
}

export function OrchiChatComposer({
  disabled = false,
  onSend
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
    <form onSubmit={handleSubmit} className="flex items-end gap-2">
      <Textarea
        value={draft}
        onChange={(event) => setDraft(event.target.value)}
        onKeyDown={handleKeyDown}
        placeholder="Message Orchi…"
        disabled={disabled}
        rows={1}
        className="min-h-10 max-h-40 resize-none"
      />
      <Button type="submit" size="icon" disabled={disabled || !draft.trim()} aria-label="Send message">
        <Send className="size-4" />
      </Button>
    </form>
  )
}
