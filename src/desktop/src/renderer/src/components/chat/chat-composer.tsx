import { useCallback, useEffect, useRef, useState } from 'react'
import { ArrowUp, Paperclip } from 'lucide-react'
import { toast } from 'sonner'

import { ChatComposerToolbar } from '@/components/chat/chat-composer-toolbar'
import {
  ComposerStagedAttachments,
  type ComposerStagedItem
} from '@/components/chat/composer-staged-attachments'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { deleteStagedChatAttachment, uploadChatAttachment } from '@/lib/chat/api'
import { isPersistedChat } from '@/lib/chat/chat-persistence'
import { getComposerDraft, setComposerDraft } from '@/lib/chat/composer-drafts'
import type { AgentMode } from '@/lib/chat/types'
import type { Project } from '@/lib/projects/types'
import { cn } from '@/lib/utils'

export type ComposerSendPayload = {
  content: string
  attachmentIds: string[]
  pendingFiles: File[]
}

type ChatComposerProps = {
  chatId: string
  autoFocus?: boolean
  disabled?: boolean
  onSend: (payload: ComposerSendPayload) => void
  expanded?: boolean
  /** Prefills the composer once on mount (e.g. text copied into a new split chat). */
  initialDraft?: string
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
  contextSizeId: string | null
  canChangeContextSize?: boolean
  contextSizeUpdateError?: string | null
  onContextSizeChange: (contextSizeId: string | null) => void
  reasoningEffortId: string | null
  canChangeReasoningEffort?: boolean
  reasoningEffortUpdateError?: string | null
  onReasoningEffortChange: (reasoningEffortId: string | null) => void
  approvalPolicyId: string | null
  canChangeApprovalPolicy?: boolean
  approvalPolicyUpdateError?: string | null
  onApprovalPolicyChange: (approvalPolicyId: string | null) => void
  projectId?: string | null
  projects?: Project[]
  /** When 0, worktree toggle is available (new chat). */
  messageCount?: number
}

export function OrchiChatComposer({
  chatId,
  autoFocus = false,
  disabled = false,
  onSend,
  expanded = false,
  initialDraft,
  mode,
  showModeControls = false,
  canChangeMode = false,
  modeUpdateError = null,
  onModeChange,
  agentId,
  modelId,
  canChangeModel = true,
  modelUpdateError = null,
  onModelChange,
  contextSizeId,
  canChangeContextSize = true,
  contextSizeUpdateError = null,
  onContextSizeChange,
  reasoningEffortId,
  canChangeReasoningEffort = true,
  reasoningEffortUpdateError = null,
  onReasoningEffortChange,
  approvalPolicyId,
  canChangeApprovalPolicy = true,
  approvalPolicyUpdateError = null,
  onApprovalPolicyChange,
  projectId = null,
  projects = [],
  messageCount = 0
}: ChatComposerProps): React.JSX.Element {
  const textareaRef = useRef<HTMLTextAreaElement>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [draft, setDraft] = useState(() => initialDraft ?? getComposerDraft(chatId) ?? '')
  const [stagedItems, setStagedItems] = useState<ComposerStagedItem[]>([])
  const [isUploading, setIsUploading] = useState(false)

  useEffect(() => {
    setStagedItems([])
  }, [chatId])

  useEffect(() => {
    if (!autoFocus || disabled) {
      return
    }

    const frameId = requestAnimationFrame(() => {
      textareaRef.current?.focus()
    })

    return () => cancelAnimationFrame(frameId)
  }, [autoFocus, chatId, disabled])

  const canSend =
    (draft.trim().length > 0 || stagedItems.length > 0) && !disabled && !isUploading

  function handleDraftChange(next: string): void {
    setDraft(next)
    setComposerDraft(chatId, next)
  }

  function handleSubmit(event: React.FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    if (!canSend) {
      return
    }

    const attachmentIds = stagedItems
      .filter((item): item is Extract<ComposerStagedItem, { kind: 'uploaded' }> => item.kind === 'uploaded')
      .map((item) => item.attachment.id)
    const pendingFiles = stagedItems
      .filter((item): item is Extract<ComposerStagedItem, { kind: 'pending' }> => item.kind === 'pending')
      .map((item) => item.file)

    onSend({
      content: draft.trim(),
      attachmentIds,
      pendingFiles
    })
    setDraft('')
    setComposerDraft(chatId, '')
    setStagedItems([])
  }

  function handleKeyDown(event: React.KeyboardEvent<HTMLTextAreaElement>): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      event.currentTarget.form?.requestSubmit()
    }
  }

  const addFiles = useCallback(
    async (files: FileList | File[]) => {
      const list = Array.from(files)
      if (list.length === 0 || disabled) {
        return
      }

      if (isPersistedChat(chatId)) {
        setIsUploading(true)
        try {
          const uploaded: ComposerStagedItem[] = []
          for (const file of list) {
            const attachment = await uploadChatAttachment(chatId, file)
            uploaded.push({ kind: 'uploaded', attachment })
          }

          setStagedItems((current) => [...current, ...uploaded])
        } catch (error) {
          const message = error instanceof Error ? error.message : 'Failed to upload attachment.'
          toast.error(message)
        } finally {
          setIsUploading(false)
        }

        return
      }

      setStagedItems((current) => [
        ...current,
        ...list.map((file) => ({
          kind: 'pending' as const,
          localId: crypto.randomUUID(),
          file
        }))
      ])
    },
    [chatId, disabled]
  )

  const handleRemoveStaged = useCallback(
    async (item: ComposerStagedItem) => {
      setStagedItems((current) =>
        current.filter((candidate) => {
          if (item.kind === 'uploaded' && candidate.kind === 'uploaded') {
            return candidate.attachment.id !== item.attachment.id
          }

          if (item.kind === 'pending' && candidate.kind === 'pending') {
            return candidate.localId !== item.localId
          }

          return true
        })
      )

      if (item.kind === 'uploaded' && isPersistedChat(chatId)) {
        try {
          await deleteStagedChatAttachment(chatId, item.attachment.id)
        } catch {
          // Staged item already removed from UI; server cleanup is best-effort.
        }
      }
    },
    [chatId]
  )

  function handleFileInputChange(event: React.ChangeEvent<HTMLInputElement>): void {
    if (event.target.files) {
      void addFiles(event.target.files)
      event.target.value = ''
    }
  }

  function handlePaste(event: React.ClipboardEvent<HTMLTextAreaElement>): void {
    const files = event.clipboardData.files
    if (files.length === 0) {
      return
    }

    event.preventDefault()
    void addFiles(files)
  }

  function handleDrop(event: React.DragEvent<HTMLFormElement>): void {
    if (!event.dataTransfer.files.length) {
      return
    }

    event.preventDefault()
    void addFiles(event.dataTransfer.files)
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="w-full"
      onDragOver={(event) => event.preventDefault()}
      onDrop={handleDrop}
    >
      <div className="rounded-xl border bg-muted/40 shadow-sm">
        <ComposerStagedAttachments
          items={stagedItems}
          disabled={disabled || isUploading}
          onRemove={(item) => void handleRemoveStaged(item)}
        />
        <Textarea
          ref={textareaRef}
          value={draft}
          onChange={(event) => handleDraftChange(event.target.value)}
          onKeyDown={handleKeyDown}
          onPaste={handlePaste}
          placeholder="Message Orchi…"
          disabled={disabled || isUploading}
          rows={expanded ? 4 : 3}
          className={cn(
            'min-h-[96px] max-h-52 resize-none border-0 bg-transparent px-4 py-3.5 text-sm leading-relaxed shadow-none',
            'focus-visible:ring-0 focus-visible:ring-offset-0',
            expanded && 'min-h-[128px]'
          )}
        />
        <div className="flex items-center justify-between gap-2 px-3.5 pb-3.5 pt-1">
          <div className="flex min-w-0 items-center gap-1">
            <input
              ref={fileInputRef}
              type="file"
              multiple
              className="sr-only"
              onChange={handleFileInputChange}
              disabled={disabled || isUploading}
            />
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="size-8 shrink-0"
              disabled={disabled || isUploading}
              aria-label="Attach files"
              onClick={() => fileInputRef.current?.click()}
            >
              <Paperclip className="size-4" />
            </Button>
            <ChatComposerToolbar
              chatId={chatId}
              disabled={disabled || isUploading}
              mode={mode}
              showModeControls={showModeControls}
              canChangeMode={canChangeMode}
              modeUpdateError={modeUpdateError}
              onModeChange={onModeChange}
              agentId={agentId}
              modelId={modelId}
              canChangeModel={canChangeModel}
              modelUpdateError={modelUpdateError}
              onModelChange={onModelChange}
              contextSizeId={contextSizeId}
              canChangeContextSize={canChangeContextSize}
              contextSizeUpdateError={contextSizeUpdateError}
              onContextSizeChange={onContextSizeChange}
              reasoningEffortId={reasoningEffortId}
              canChangeReasoningEffort={canChangeReasoningEffort}
              reasoningEffortUpdateError={reasoningEffortUpdateError}
              onReasoningEffortChange={onReasoningEffortChange}
              approvalPolicyId={approvalPolicyId}
              canChangeApprovalPolicy={canChangeApprovalPolicy}
              approvalPolicyUpdateError={approvalPolicyUpdateError}
              onApprovalPolicyChange={onApprovalPolicyChange}
              projectId={projectId}
              projects={projects}
              messageCount={messageCount}
            />
          </div>
          <Button
            type="submit"
            size="icon"
            disabled={!canSend}
            aria-label="Send message"
            className="size-8 shrink-0 rounded-full"
          >
            <ArrowUp className="size-4" />
          </Button>
        </div>
      </div>
    </form>
  )
}
