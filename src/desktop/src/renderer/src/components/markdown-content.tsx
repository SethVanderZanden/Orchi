import type { Components } from 'react-markdown'
import ReactMarkdown from 'react-markdown'
import remarkBreaks from 'remark-breaks'
import remarkGfm from 'remark-gfm'

import { cn } from '@/lib/utils'

type MarkdownContentProps = {
  children: string
  className?: string
}

function isInlineCode(className: string | undefined, children: React.ReactNode): boolean {
  if (className?.includes('language-')) {
    return false
  }

  return !String(children).includes('\n')
}

const markdownComponents: Components = {
  table({ children, ...props }) {
    return (
      <div className="my-3 max-w-full overflow-x-auto rounded-md border border-border/60">
        <table className="m-0 w-full border-collapse text-left text-[0.925em]" {...props}>
          {children}
        </table>
      </div>
    )
  },
  th({ children, ...props }) {
    return (
      <th className="border-b border-border/70 bg-muted/40 px-2.5 py-1.5 font-semibold" {...props}>
        {children}
      </th>
    )
  },
  td({ children, ...props }) {
    return (
      <td className="border-b border-border/40 px-2.5 py-1.5 align-top" {...props}>
        {children}
      </td>
    )
  },
  code({ className, children, ...props }) {
    if (!isInlineCode(className, children)) {
      return (
        <code className={className} {...props}>
          {children}
        </code>
      )
    }

    return (
      <code
        className={cn(
          'mx-0.5 inline-flex items-center rounded-md border border-border/70 bg-secondary px-1.5 py-0.5 font-mono text-[0.8125em] font-medium leading-none text-secondary-foreground',
          className
        )}
        {...props}
      >
        {children}
      </code>
    )
  }
}

export function MarkdownContent({ children, className }: MarkdownContentProps): React.JSX.Element {
  return (
    <div
      className={cn(
        'prose prose-sm prose-neutral dark:prose-invert max-w-none text-inherit',
        'prose-headings:font-semibold prose-headings:text-inherit',
        'prose-p:my-2 prose-p:leading-relaxed prose-p:text-inherit first:prose-p:mt-0 last:prose-p:mb-0',
        'prose-li:text-inherit prose-strong:text-inherit',
        'prose-ul:my-2 prose-ol:my-2 prose-li:my-0.5',
        'prose-pre:bg-muted/60 prose-pre:text-inherit prose-code:text-inherit',
        'prose-code:before:content-none prose-code:after:content-none',
        'prose-a:text-inherit prose-a:underline prose-a:underline-offset-2',
        'prose-table:my-0 prose-th:text-inherit prose-td:text-inherit',
        '[&>*:first-child]:mt-0 [&>*:last-child]:mb-0',
        className
      )}
    >
      <ReactMarkdown remarkPlugins={[remarkGfm, remarkBreaks]} components={markdownComponents}>
        {children}
      </ReactMarkdown>
    </div>
  )
}
