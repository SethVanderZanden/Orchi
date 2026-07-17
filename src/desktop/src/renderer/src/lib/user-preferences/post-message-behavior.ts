import type { PostMessageBehavior } from '@/lib/user-preferences/types'

export type PostMessageBehaviorOption = {
  id: PostMessageBehavior
  label: string
  description: string
}

const OPTIONS: PostMessageBehaviorOption[] = [
  {
    id: 'stayOnChat',
    label: 'Stay on chat',
    description: 'Keep the current chat open after the agent finishes responding.'
  },
  {
    id: 'goToBoard',
    label: 'Go to Board',
    description: 'Switch to the agents board when a response completes.'
  },
  {
    id: 'openNewChat',
    label: 'Open new chat',
    description: 'Start a fresh chat tab when a response completes.'
  }
]

export function getPostMessageBehaviorOptions(): PostMessageBehaviorOption[] {
  return OPTIONS
}

export function getPostMessageBehaviorLabel(behavior: PostMessageBehavior): string {
  return OPTIONS.find((option) => option.id === behavior)?.label ?? 'Stay on chat'
}
