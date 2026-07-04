import { cn } from '@/lib/utils'

type SidebarSectionHeaderProps = {
  children: React.ReactNode
  className?: string
  isFirst?: boolean
}

export function SidebarSectionHeader({
  children,
  className,
  isFirst = false
}: SidebarSectionHeaderProps): React.JSX.Element {
  return (
    <h2
      className={cn(
        'px-3 pb-2 text-[11px] font-semibold uppercase tracking-widest text-sidebar-muted',
        isFirst ? 'pt-3' : 'pt-5',
        className
      )}
    >
      {children}
    </h2>
  )
}
