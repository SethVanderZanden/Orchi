import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'

import { cn } from '@/lib/utils'

type MarkdownContentProps = {
  children: string
  className?: string
}

export function MarkdownContent({ children, className }: MarkdownContentProps): React.JSX.Element {
  return (
    <div
      className={cn(
        'prose prose-sm prose-neutral dark:prose-invert max-w-none text-foreground prose-headings:font-semibold prose-headings:text-foreground prose-p:leading-relaxed prose-p:text-foreground prose-li:text-foreground prose-strong:text-foreground prose-pre:bg-muted prose-pre:text-foreground prose-code:text-foreground prose-a:text-primary',
        className
      )}
    >
      <ReactMarkdown remarkPlugins={[remarkGfm]}>{children}</ReactMarkdown>
    </div>
  )
}
