import { useSyncExternalStore } from 'react'
import { GitBranch } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  canUseWorktreeToggle,
  getWorktreeIntent,
  isWorktreeIntentEnabled,
  setWorktreeIntentBranchName,
  setWorktreeIntentEnabled,
  subscribeWorktreeIntents
} from '@/lib/chat/worktree-intent'
import type { Project } from '@/lib/projects/types'
import { cn } from '@/lib/utils'

type ChatWorktreeToggleProps = {
  chatId: string
  projectId: string | null
  projects: Project[]
  /** Message count — toggle only shown when 0 (new chat). */
  messageCount: number
  disabled?: boolean
  className?: string
}

export function ChatWorktreeToggle({
  chatId,
  projectId,
  projects,
  messageCount,
  disabled = false,
  className
}: ChatWorktreeToggleProps): React.JSX.Element | null {
  const intent = useSyncExternalStore(
    subscribeWorktreeIntents,
    () => getWorktreeIntent(chatId),
    () => getWorktreeIntent(chatId)
  )
  const enabled = intent?.enabled === true
  const project = projects.find((entry) => entry.id === projectId) ?? null

  if (!canUseWorktreeToggle(messageCount) || !projectId || !project) {
    return null
  }

  const pattern = project.defaultWorktreeBranchPattern || 'orchi/{date}-{shortId}'

  return (
    <div className={cn('flex min-w-0 items-center gap-1.5', className)}>
      <Button
        type="button"
        variant={enabled ? 'default' : 'ghost'}
        size="sm"
        disabled={disabled}
        className={cn('h-7 gap-1.5 px-2 text-xs font-normal', !enabled && 'text-muted-foreground')}
        aria-label={enabled ? 'Disable worktree for this chat' : 'Enable worktree for this chat'}
        aria-pressed={enabled}
        title="Worktree (Ctrl+T)"
        onClick={() => setWorktreeIntentEnabled(chatId, !isWorktreeIntentEnabled(chatId))}
      >
        <GitBranch className="size-3.5" />
        Worktree
      </Button>
      {enabled ? (
        <Input
          value={intent?.branchName ?? ''}
          onChange={(change) => setWorktreeIntentBranchName(chatId, change.target.value)}
          disabled={disabled}
          placeholder={pattern}
          aria-label="Worktree branch name"
          className="h-7 w-40 min-w-0 px-2 text-xs"
        />
      ) : null}
    </div>
  )
}
