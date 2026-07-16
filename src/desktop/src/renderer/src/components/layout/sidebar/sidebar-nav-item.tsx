import { Slot } from '@radix-ui/react-slot'
import { cva, type VariantProps } from 'class-variance-authority'

import { cn } from '@/lib/utils'

const sidebarNavItemVariants = cva(
  'group flex w-full min-w-0 items-center gap-2 rounded-md text-left transition-colors duration-150 ease-out',
  {
    variants: {
      size: {
        default: 'px-2.5 py-2 text-sm',
        compact: 'px-2.5 py-1.5 text-xs'
      },
      isActive: {
        true: 'bg-sidebar-accent font-medium text-sidebar-accent-foreground',
        false: 'text-sidebar-muted hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'
      }
    },
    defaultVariants: {
      size: 'default',
      isActive: false
    }
  }
)

type SidebarNavItemProps = React.ButtonHTMLAttributes<HTMLButtonElement> &
  VariantProps<typeof sidebarNavItemVariants> & {
    asChild?: boolean
    leading?: React.ReactNode
    trailing?: React.ReactNode
  }

export function SidebarNavItem({
  className,
  size,
  isActive,
  asChild = false,
  leading,
  trailing,
  children,
  ...props
}: SidebarNavItemProps): React.JSX.Element {
  const Comp = asChild ? Slot : 'button'

  return (
    <Comp className={cn(sidebarNavItemVariants({ size, isActive }), className)} {...props}>
      {leading ? <span className="flex shrink-0 items-center gap-2">{leading}</span> : null}
      <span className="min-w-0 flex-1 line-clamp-2 break-words">{children}</span>
      {trailing ? <span className="shrink-0 self-start">{trailing}</span> : null}
    </Comp>
  )
}
