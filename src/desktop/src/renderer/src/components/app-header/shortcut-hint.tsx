import { cn } from '@/lib/utils'

type ShortcutHintProps = {
  children: React.ReactNode
  className?: string
}

export function ShortcutHint({ children, className }: ShortcutHintProps): React.JSX.Element {
  return (
    <kbd
      className={cn(
        'pointer-events-none rounded border border-border bg-muted px-1 py-0.5 font-mono text-[11px] text-muted-foreground',
        className
      )}
    >
      {children}
    </kbd>
  )
}
