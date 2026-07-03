import { useState } from 'react'

import { Layout, LayoutContent, HStack, VStack } from '@astryxdesign/core/Layout'
import { Button } from '@astryxdesign/core/Button'
import { Dialog, DialogHeader } from '@astryxdesign/core/Dialog'
import { DropdownMenu } from '@astryxdesign/core/DropdownMenu'
import { TextInput } from '@astryxdesign/core/TextInput'
import { Text } from '@astryxdesign/core/Text'

import type { AgentMode } from '@/lib/chat/types'

const MODE_OPTIONS: Array<{ id: AgentMode; label: string }> = [
  { id: 'default', label: 'Default' },
  { id: 'orchestration', label: 'Orchestration' }
]

export type NewChatOptions = {
  workspacePath: string
  mode: AgentMode
}

type NewChatDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  workspacePath: string
  workspaceName: string
  onCreateChat: (options: NewChatOptions) => Promise<void>
  isSubmitting?: boolean
}

export function NewChatDialog({
  open,
  onOpenChange,
  workspacePath,
  workspaceName,
  onCreateChat,
  isSubmitting = false
}: NewChatDialogProps): React.JSX.Element {
  const [mode, setMode] = useState<AgentMode>('default')

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    await onCreateChat({ workspacePath, mode })
    onOpenChange(false)
  }

  return (
    <Dialog isOpen={open} onOpenChange={onOpenChange} purpose="form" width={440}>
      <Layout
        header={
          <DialogHeader
            title={`New chat in ${workspaceName}`}
            subtitle="Start a conversation with the Cursor agent in this project."
            onOpenChange={onOpenChange}
          />
        }
        content={
          <LayoutContent>
            <form onSubmit={handleSubmit}>
              <VStack gap={4}>
                <VStack gap={1}>
                  <Text type="label" weight="semibold">
                    {workspaceName}
                  </Text>
                  <Text type="supporting" color="secondary">
                    {workspacePath}
                  </Text>
                </VStack>

                <TextInput label="Agent" value="Cursor" onChange={() => {}} isDisabled />

                <VStack gap={1}>
                  <Text type="label" weight="semibold">
                    Mode
                  </Text>
                  <DropdownMenu
                    button={{
                      label: MODE_OPTIONS.find((option) => option.id === mode)?.label ?? 'Default',
                      variant: 'secondary',
                      size: 'sm'
                    }}
                    items={MODE_OPTIONS.map((option) => ({
                      label: option.label,
                      onClick: () => setMode(option.id)
                    }))}
                  />
                  {mode === 'orchestration' ? (
                    <Text type="supporting" color="secondary">
                      Splits work into plans that can be kicked off to implementation agents.
                    </Text>
                  ) : null}
                </VStack>

                <HStack gap={2} hAlign="end">
                  <Button label="Cancel" variant="secondary" onClick={() => onOpenChange(false)} />
                  <Button label="Create chat" type="submit" isDisabled={isSubmitting} />
                </HStack>
              </VStack>
            </form>
          </LayoutContent>
        }
      />
    </Dialog>
  )
}
