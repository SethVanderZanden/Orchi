import { ProjectNavigator } from '@/components/project/project-navigator'
import { ProjectShellLayout } from '@/components/layout/project-shell-layout'
import { TooltipProvider } from '@/components/ui/tooltip'

export function AppLayout(): React.JSX.Element {
  return (
    <TooltipProvider delayDuration={0}>
      <div className="flex h-full min-h-0 min-w-0 bg-background text-foreground">
        <ProjectNavigator />
        <ProjectShellLayout />
      </div>
    </TooltipProvider>
  )
}
