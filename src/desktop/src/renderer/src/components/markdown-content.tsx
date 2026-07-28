import { memo, useCallback, type ReactNode } from 'react'
import type { Components } from 'react-markdown'
import ReactMarkdown from 'react-markdown'
import remarkBreaks from 'remark-breaks'
import remarkGfm from 'remark-gfm'

import {
  formatDiffStatsCell,
  isWorkspaceDiffStatsMessage,
  stripWorkspaceDiffStatsMarker
} from '@/lib/chat/workspace-diff-stats'
import {
  decodeOpenEditorLinkHref,
  isOpenEditorLinkHref,
  transformOpenEditorBlocksForDisplay
} from '@/lib/open-editor/transform-open-editor-blocks'
import { openEditorFromCommand } from '@/lib/preferences/open-editor-from-command'
import { cn } from '@/lib/utils'

type MarkdownContentProps = {
  children: string
  className?: string
}

function isInlineCode(className: string | undefined, children: ReactNode): boolean {
  if (className?.includes('language-')) {
    return false
  }

  return !String(children).includes('\n')
}

function createMarkdownComponents(
  enableDiffStatsColors: boolean,
  onOpenEditorLink: (command: string) => void
): Components {
  return {
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
      const text = String(children).trim()
      const formatted = enableDiffStatsColors ? formatDiffStatsCell(text) : { text, tone: null }

      return (
        <td
          className={cn(
            'border-b border-border/40 px-2.5 py-1.5 align-top',
            formatted.tone === 'added' && 'font-medium text-emerald-600 dark:text-emerald-400',
            formatted.tone === 'removed' && 'font-medium text-red-600 dark:text-red-400'
          )}
          {...props}
        >
          {formatted.tone ? formatted.text : children}
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
            'mx-0.5 inline max-w-full break-words rounded-md border border-border/70 bg-secondary px-1.5 py-0.5 align-middle font-mono text-[0.8125em] font-medium text-secondary-foreground whitespace-normal',
            className
          )}
          {...props}
        >
          {children}
        </code>
      )
    },
    a({ href, children, ...props }) {
      if (isOpenEditorLinkHref(href)) {
        return (
          <button
            type="button"
            className="inline font-medium text-primary underline underline-offset-2 hover:text-primary/80"
            onClick={() => onOpenEditorLink(decodeOpenEditorLinkHref(href))}
          >
            {children}
          </button>
        )
      }

      return (
        <a href={href} {...props}>
          {children}
        </a>
      )
    }
  }
}

export const MarkdownContent = memo(function MarkdownContent({
  children,
  className
}: MarkdownContentProps): React.JSX.Element {
  const enableDiffStatsColors = isWorkspaceDiffStatsMessage(children)
  const stripped = enableDiffStatsColors ? stripWorkspaceDiffStatsMarker(children) : children
  const markdown = transformOpenEditorBlocksForDisplay(stripped)

  const handleOpenEditorLink = useCallback((command: string) => {
    void openEditorFromCommand(command)
  }, [])

  return (
    <div
      className={cn(
        'prose prose-sm prose-neutral dark:prose-invert max-w-none min-w-0 w-full break-words text-inherit',
        'prose-headings:mt-6 prose-headings:mb-3 prose-headings:font-semibold prose-headings:text-inherit first:prose-headings:mt-0',
        'prose-p:my-5 prose-p:leading-7 prose-p:break-words prose-p:text-inherit first:prose-p:mt-0 last:prose-p:mb-0',
        'prose-li:break-words prose-li:text-inherit prose-strong:text-inherit',
        'prose-ul:my-5 prose-ol:my-5 prose-li:my-1.5',
        'prose-pre:my-5 prose-pre:bg-muted/60 prose-pre:text-inherit prose-pre:code:text-inherit',
        'prose-blockquote:my-5 prose-hr:my-8',
        'prose-code:before:content-none prose-code:after:content-none',
        'prose-a:text-inherit prose-a:underline prose-a:underline-offset-2',
        'prose-table:my-0 prose-th:text-inherit prose-td:text-inherit',
        '[&>*:first-child]:mt-0 [&>*:last-child]:mb-0',
        className
      )}
    >
      <ReactMarkdown
        remarkPlugins={[remarkGfm, remarkBreaks]}
        urlTransform={(url) => url}
        components={createMarkdownComponents(enableDiffStatsColors, handleOpenEditorLink)}
      >
        {markdown}
      </ReactMarkdown>
    </div>
  )
})
