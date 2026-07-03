import { useState } from 'react'
import { ChevronDown } from 'lucide-react'

import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from '@/components/ui/dialog'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
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

  const modeLabel = MODE_OPTIONS.find((option) => option.id === mode)?.label ?? 'Default'

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>New chat in {workspaceName}</DialogTitle>
          <DialogDescription>
            Start a conversation with the Cursor agent in this project.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-1">
            <p className="text-sm font-medium">{workspaceName}</p>
            <p className="text-xs text-muted-foreground">{workspacePath}</p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="agent">Agent</Label>
            <Input id="agent" value="Cursor" disabled readOnly />
          </div>

          <div className="space-y-2">
            <Label>Mode</Label>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="outline" size="sm" type="button" className="w-full justify-between">
                  {modeLabel}
                  <ChevronDown className="size-4 opacity-50" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start" className="w-[var(--radix-dropdown-menu-trigger-width)]">
                {MODE_OPTIONS.map((option) => (
                  <DropdownMenuItem key={option.id} onClick={() => setMode(option.id)}>
                    {option.label}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
            {mode === 'orchestration' ? (
              <p className="text-xs text-muted-foreground">
                Splits work into plans that can be kicked off to implementation agents.
              </p>
            ) : null}
          </div>

          <DialogFooter>
            <Button type="button" variant="secondary" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              Create chat
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
