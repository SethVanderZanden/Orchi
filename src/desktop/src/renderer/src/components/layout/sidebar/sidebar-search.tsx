import { Search } from 'lucide-react'

import { cn } from '@/lib/utils'

type SidebarSearchProps = {
  value: string
  onChange: (value: string) => void
  placeholder?: string
  'aria-label'?: string
  className?: string
}

export function SidebarSearch({
  value,
  onChange,
  placeholder = 'Search chats',
  'aria-label': ariaLabel = 'Search chats',
  className
}: SidebarSearchProps): React.JSX.Element {
  return (
    <div className={cn('px-3 pb-2', className)}>
      <div className="relative">
        <Search
          className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-sidebar-muted"
          aria-hidden
        />
        <input
          type="search"
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder={placeholder}
          aria-label={ariaLabel}
          className="h-9 w-full rounded-md bg-sidebar-search py-2 pl-9 pr-3 text-sm text-sidebar-foreground transition-colors duration-150 ease-out placeholder:text-sidebar-muted focus:bg-sidebar-accent/50 focus:outline-none focus:ring-0"
        />
      </div>
    </div>
  )
}
