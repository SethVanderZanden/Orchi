import { ChevronDown, Columns2, FileText, GitBranch, Trash2, X } from 'lucide-react'

import { ShortcutHint } from '@/components/app-header/shortcut-hint'
import { ChatGitActionsMenu } from '@/components/layout/chat-git-actions-menu'
import { OpenInEditorMenu } from '@/components/layout/open-in-editor-menu'
import { Button } from '@/components/ui/button'
import { ButtonGroup } from '@/components/ui/button-group'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu'
import { PageHeader } from '@/components/ui/page-header'
import { requestOpenBranchReview } from '@/lib/branch-review/events'
import type { ChatThread } from '@/lib/chat/types'
import type { GitHostProvider } from '@/lib/git/types'

type ChatWorkspaceHeaderProps = {
  chat: ChatThread
  projectName: string | null
  childChatCount: number
  workspacePath: string
  chatId: string
  projectId: string | null
  defaultBaseBranch: string
  gitHostProvider: GitHostProvider
  workspaceBranch: string | null
  parentChatId: string | null
  parentTitle: string | null
  showPlanReview: boolean
  reviewPanelOpen: boolean
  hasReviewReady: boolean
  onToggleReviewPanel: () => void
  onOpenParentBeside: () => void
  onClose: () => void
  onDelete: () => void
  deleteDisabled: boolean
}

export function ChatWorkspaceHeader({
  chat,
  projectName,
  childChatCount,
  workspacePath,
  chatId,
  projectId,
  defaultBaseBranch,
  gitHostProvider,
  workspaceBranch,
  parentChatId,
  parentTitle,
  showPlanReview,
  reviewPanelOpen,
  hasReviewReady,
  onToggleReviewPanel,
  onOpenParentBeside,
  onClose,
  onDelete,
  deleteDisabled
}: ChatWorkspaceHeaderProps): React.JSX.Element {
  return (
    <PageHeader
      startContent={
        <div className="min-w-0 space-y-1">
          <p className="truncate text-sm text-muted-foreground">
            {projectName ? `${projectName} · ` : ''}
            {chat.workspacePath} · {chat.messages.length} message
            {chat.messages.length === 1 ? '' : 's'}
            {childChatCount > 0
              ? ` · ${childChatCount} child agent${childChatCount === 1 ? '' : 's'}`
              : ''}
          </p>
          {parentChatId ? (
            <div className="flex min-w-0 items-center gap-2">
              <p className="min-w-0 truncate text-sm text-muted-foreground">
                Parent: <span className="text-foreground/80">{parentTitle ?? 'Parent chat'}</span>
              </p>
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="h-7 shrink-0 gap-1.5 px-2 text-xs font-normal"
                onClick={onOpenParentBeside}
                title="Open parent beside"
                aria-label={`Open parent ${parentTitle ?? 'chat'} beside`}
              >
                <Columns2 className="size-3" />
                Open beside
                <ShortcutHint>Ctrl+↑</ShortcutHint>
              </Button>
            </div>
          ) : null}
          {chat.planFilePath ? (
            <p className="truncate text-sm text-muted-foreground">Plan: {chat.planFilePath}</p>
          ) : null}
        </div>
      }
      endContent={
        <>
          <OpenInEditorMenu workspacePath={workspacePath} />
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="h-8 gap-1.5 px-3 text-sm font-normal"
            disabled={projectId == null}
            title={
              projectId == null
                ? 'Assign a project to review a branch.'
                : 'Compare two branches and start a review chat'
            }
            aria-label="Review branch"
            onClick={() => {
              if (!projectId) {
                return
              }
              requestOpenBranchReview({ projectId })
            }}
          >
            <GitBranch className="size-3.5" />
            Review branch
          </Button>
          <ChatGitActionsMenu
            chatId={chatId}
            projectId={projectId}
            workspacePath={workspacePath}
            defaultBaseBranch={defaultBaseBranch}
            gitHostProvider={gitHostProvider}
            workspaceBranch={workspaceBranch}
          />
          {showPlanReview ? (
            <Button
              variant={reviewPanelOpen ? 'default' : 'outline'}
              size="sm"
              className="h-8 gap-1.5 px-3 text-sm font-normal"
              onClick={onToggleReviewPanel}
            >
              <FileText className="size-3.5" />
              Review
              <ShortcutHint>Ctrl+R</ShortcutHint>
              {hasReviewReady && !reviewPanelOpen ? (
                <span className="size-1.5 rounded-full bg-primary" aria-label="Review ready" />
              ) : null}
            </Button>
          ) : null}
          <ButtonGroup aria-label="Chat actions">
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="h-8 gap-1.5 px-2.5 text-sm font-normal"
              aria-label={`Close ${chat.title}`}
              onClick={onClose}
            >
              <X className="size-4" />
              Close
            </Button>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  type="button"
                  variant="outline"
                  size="icon"
                  className="size-8"
                  aria-label="More chat actions"
                >
                  <ChevronDown className="size-3.5" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem
                  className="font-medium text-destructive focus:bg-destructive/10 focus:text-destructive dark:focus:bg-destructive/20 [&_svg]:text-destructive!"
                  disabled={deleteDisabled}
                  onClick={onDelete}
                >
                  <Trash2 className="size-4" />
                  Delete Chat
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </ButtonGroup>
        </>
      }
    />
  )
}
