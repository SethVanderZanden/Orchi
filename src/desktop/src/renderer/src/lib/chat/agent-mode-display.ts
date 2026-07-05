import { Bot, Network, Shield, type LucideIcon } from 'lucide-react'

import type { AgentMode } from '@/lib/chat/types'

export type AgentModeDisplay = {
  Icon: LucideIcon
  label: string
  badgeClassName: string
}

const ORCHESTRATION: AgentModeDisplay = {
  Icon: Network,
  label: 'Orchestration',
  badgeClassName: 'border-amber-500/30 bg-amber-500/15 text-amber-200'
}
const AGENT: AgentModeDisplay = {
  Icon: Bot,
  label: 'Agent',
  badgeClassName: 'border-primary/30 bg-primary/15 text-primary'
}
const REVIEW: AgentModeDisplay = {
  Icon: Shield,
  label: 'Review',
  badgeClassName: 'border-violet-500/30 bg-violet-500/15 text-violet-200'
}

export function getAgentModeDisplay(mode: AgentMode): AgentModeDisplay {
  switch (mode.toLowerCase()) {
    case 'orchestration':
      return ORCHESTRATION
    case 'default':
    case 'implementation':
      return AGENT
    case 'review':
      return REVIEW
    default:
      return AGENT
  }
}
