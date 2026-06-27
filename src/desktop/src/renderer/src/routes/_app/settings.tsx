import { createFileRoute, Link } from '@tanstack/react-router'
import { SettingsIcon } from 'lucide-react'

import { AppPageHeader } from '@/components/layout/app-page-header'
import { Button } from '@/components/ui/button'

export const Route = createFileRoute('/_app/settings')({
  component: SettingsPage
})

function SettingsPage(): React.JSX.Element {
  return (
    <div className="flex h-full min-h-0 flex-1 flex-col">
      <AppPageHeader title="Settings" description="Workspace and app preferences" />

      <main className="mx-auto flex w-full max-w-2xl flex-1 flex-col gap-6 p-6">
        <section className="space-y-2">
          <h2 className="text-sm font-medium">Navigation check</h2>
          <p className="text-muted-foreground text-sm leading-relaxed">
            This page uses the same base layout as chat. The sidebar stays mounted, collapsible,
            and available while you move between routes.
          </p>
        </section>

        <section className="rounded-xl border p-4">
          <p className="text-muted-foreground mb-4 text-sm">
            Jump back to a conversation without losing sidebar state.
          </p>
          <Button asChild variant="outline">
            <Link to="/">Open chats</Link>
          </Button>
        </section>

        <section className="text-muted-foreground flex items-center gap-2 text-xs">
          <SettingsIcon className="size-3.5" />
          More settings panels will land here as Orchi grows.
        </section>
      </main>
    </div>
  )
}
