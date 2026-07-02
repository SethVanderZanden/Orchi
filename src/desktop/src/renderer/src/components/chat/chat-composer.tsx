import { useState } from 'react'

import { ChatComposer, ChatComposerInput } from '@astryxdesign/core/Chat'

type ChatComposerProps = {
  disabled?: boolean
  onSend: (content: string) => void
}

export function OrchiChatComposer({
  disabled = false,
  onSend
}: ChatComposerProps): React.JSX.Element {
  const [draft, setDraft] = useState('')

  function handleSubmit(value: string): void {
    const content = value.trim()
    if (!content || disabled) {
      return
    }

    onSend(content)
    setDraft('')
  }

  return (
    <ChatComposer
      value={draft}
      onChange={setDraft}
      onSubmit={handleSubmit}
      placeholder="Message Orchi…"
      isDisabled={disabled}
      input={<ChatComposerInput />}
    />
  )
}
