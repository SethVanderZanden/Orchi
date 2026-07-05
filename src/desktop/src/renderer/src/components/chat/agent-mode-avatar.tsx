import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { getAgentModeDisplay } from '@/lib/chat/agent-mode-display'
import type { AgentMode } from '@/lib/chat/types'

type AgentModeAvatarProps = {
  mode: AgentMode
}

export function AgentModeAvatar({ mode }: AgentModeAvatarProps): React.JSX.Element {
  const { Icon, label } = getAgentModeDisplay(mode)

  return (
    <Avatar className="size-7">
      <AvatarFallback aria-label={label} className="bg-muted">
        <Icon className="size-3.5" aria-hidden />
      </AvatarFallback>
    </Avatar>
  )
}
