'use client'

import { useEffect, useState } from 'react'
import {
  CircleCheckIcon,
  InfoIcon,
  Loader2Icon,
  OctagonXIcon,
  TriangleAlertIcon
} from 'lucide-react'
import { Toaster as Sonner, type ToasterProps } from 'sonner'

function readDocumentTheme(): NonNullable<ToasterProps['theme']> {
  return document.documentElement.classList.contains('dark') ? 'dark' : 'light'
}

export function Toaster({ ...props }: ToasterProps): React.JSX.Element {
  const [theme, setTheme] = useState<NonNullable<ToasterProps['theme']>>('light')

  useEffect(() => {
    const root = document.documentElement
    const sync = (): void => setTheme(readDocumentTheme())
    sync()

    const observer = new MutationObserver(sync)
    observer.observe(root, { attributes: true, attributeFilter: ['class'] })
    return () => observer.disconnect()
  }, [])

  return (
    <Sonner
      theme={theme}
      className="toaster group"
      icons={{
        success: <CircleCheckIcon className="size-4" />,
        info: <InfoIcon className="size-4" />,
        warning: <TriangleAlertIcon className="size-4" />,
        error: <OctagonXIcon className="size-4" />,
        loading: <Loader2Icon className="size-4 animate-spin" />
      }}
      style={
        {
          '--normal-bg': 'var(--popover)',
          '--normal-text': 'var(--popover-foreground)',
          '--normal-border': 'var(--border)',
          '--border-radius': 'var(--radius)'
        } as React.CSSProperties
      }
      {...props}
    />
  )
}
