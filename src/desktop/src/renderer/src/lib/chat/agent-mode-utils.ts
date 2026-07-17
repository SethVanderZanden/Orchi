import type { AgentMode, AgentModeOption } from '@/lib/chat/types'

export const FALLBACK_MODE_OPTIONS: AgentModeOption[] = [
  { id: 'default', label: 'Default', description: null },
  {
    id: 'orchestration',
    label: 'Orchestration',
    description: 'Splits work into plans that can be kicked off to implementation agents.'
  },
  {
    id: 'review',
    label: 'Review',
    description:
      'Produces review plans after implementation to verify work against the original plan.'
  }
]

export function modeIdsEqual(left: AgentMode, right: AgentMode): boolean {
  return left.toLowerCase() === right.toLowerCase()
}

export function getNextAgentMode(current: AgentMode, options: AgentModeOption[]): AgentMode {
  if (options.length === 0) {
    return current
  }

  const currentIndex = options.findIndex((option) => modeIdsEqual(option.id, current))
  const nextIndex = currentIndex === -1 ? 0 : (currentIndex + 1) % options.length
  return options[nextIndex]!.id
}

export function resolveAgentModeOptions(options: AgentModeOption[] | undefined): AgentModeOption[] {
  return options && options.length > 0 ? options : FALLBACK_MODE_OPTIONS
}
