import { Layout, LayoutContent, VStack } from '@astryxdesign/core/Layout'
import { Button } from '@astryxdesign/core/Button'
import { Dialog, DialogHeader } from '@astryxdesign/core/Dialog'
import { TextInput } from '@astryxdesign/core/TextInput'
import { Text } from '@astryxdesign/core/Text'
import { HStack } from '@astryxdesign/core/Layout'

export type NewChatOptions = {
  workspacePath: string
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
  async function handleSubmit(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    await onCreateChat({ workspacePath })
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

                <HStack gap={2} hAlign="end">
                  <Button
                    label="Cancel"
                    variant="secondary"
                    onClick={() => onOpenChange(false)}
                  />
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
