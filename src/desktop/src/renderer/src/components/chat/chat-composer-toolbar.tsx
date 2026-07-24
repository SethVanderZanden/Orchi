import { memo } from 'react'

import { ChatModeDropdown } from '@/components/chat/chat-mode-dropdown'
import { ChatModelSelector } from '@/components/chat/chat-model-selector'
import { ChatContextSizeSelector } from '@/components/chat/chat-context-size-selector'
import { ChatCliOptionSelector } from '@/components/chat/chat-cli-option-selector'
import { ChatWorktreeToggle } from '@/components/chat/chat-worktree-toggle'
import type { AgentMode } from '@/lib/chat/types'
import type { Project } from '@/lib/projects/types'

export type ChatComposerToolbarProps = {
  chatId: string
  disabled?: boolean
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
  messageCount?: number
}

/** Model/mode selectors — memoized so keystrokes in the textarea do not re-render them. */
export const ChatComposerToolbar = memo(function ChatComposerToolbar({
  chatId,
  disabled = false,
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
}: ChatComposerToolbarProps): React.JSX.Element {
  return (
    <div className="flex min-w-0 flex-wrap items-center gap-1.5">
      {showModeControls ? (
        <ChatModeDropdown
          mode={mode}
          disabled={!canChangeMode}
          onModeChange={onModeChange}
          onClear={() => onModeChange('default')}
        />
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
      <ChatContextSizeSelector
        agentId={agentId}
        contextSizeId={contextSizeId}
        mode={mode}
        disabled={!canChangeContextSize}
        error={contextSizeUpdateError}
        onContextSizeChange={onContextSizeChange}
        compact
      />
      <ChatCliOptionSelector
        agentId={agentId}
        kind="model_reasoning_effort"
        optionId={reasoningEffortId}
        mode={mode}
        disabled={!canChangeReasoningEffort}
        error={reasoningEffortUpdateError}
        onOptionChange={onReasoningEffortChange}
        compact
      />
      <ChatCliOptionSelector
        agentId={agentId}
        kind="approval_policy"
        optionId={approvalPolicyId}
        mode={mode}
        disabled={!canChangeApprovalPolicy}
        error={approvalPolicyUpdateError}
        onOptionChange={onApprovalPolicyChange}
        compact
      />
      <ChatWorktreeToggle
        chatId={chatId}
        projectId={projectId}
        projects={projects}
        messageCount={messageCount}
        disabled={disabled}
      />
      {modeUpdateError ? <p className="sr-only">{modeUpdateError}</p> : null}
    </div>
  )
})
