import { SidebarTrigger } from '@/components/ui/sidebar'
import { Separator } from '@/components/ui/separator'
import { cn } from '@/lib/utils'

type AppPageHeaderProps = {
  title: string
  description?: string
  className?: string
  children?: React.ReactNode
}

export function AppPageHeader({
  title,
  description,
  className,
  children
}: AppPageHeaderProps): React.JSX.Element {
  return (
    <header
      className={cn(
        'flex h-14 shrink-0 items-center gap-2 border-b px-4',
        className
      )}
    >
      <SidebarTrigger className="-ml-1" />
      <Separator orientation="vertical" className="mr-1 h-4" />
      <div className="min-w-0 flex-1">
        <h1 className="truncate text-sm font-semibold">{title}</h1>
        {description ? (
          <p className="text-muted-foreground truncate text-xs">{description}</p>
        ) : null}
      </div>
      {children}
    </header>
  )
}
