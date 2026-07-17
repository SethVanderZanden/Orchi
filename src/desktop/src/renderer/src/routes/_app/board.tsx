import { createFileRoute } from '@tanstack/react-router'

import { AgentKanbanBoard } from '@/components/kanban/agent-kanban-board'

export const Route = createFileRoute('/_app/board')({
  component: BoardPage
})

function BoardPage(): React.JSX.Element {
  return <AgentKanbanBoard />
}
