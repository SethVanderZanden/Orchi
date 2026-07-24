import { createFileRoute, Navigate } from '@tanstack/react-router'

export const Route = createFileRoute('/_app/board')({
  component: BoardRedirect
})

function BoardRedirect(): React.JSX.Element {
  return <Navigate to="/" replace />
}
