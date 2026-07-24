import { useCallback, useEffect, useRef } from 'react'

import { SIDEBAR_MAX_WIDTH, SIDEBAR_MIN_WIDTH } from '@/lib/preferences/sidebar-width'

type SidebarResizeHandleProps = {
  width: number
  onWidthChange: (width: number) => void
  onWidthCommit: (width: number) => void
}

export function SidebarResizeHandle({
  width,
  onWidthChange,
  onWidthCommit
}: SidebarResizeHandleProps): React.JSX.Element {
  const isDragging = useRef(false)
  const dragStartX = useRef(0)
  const dragStartWidth = useRef(width)
  const latestWidth = useRef(width)
  const rafId = useRef<number | undefined>(undefined)

  useEffect(() => {
    latestWidth.current = width
  }, [width])

  const handleMouseMove = useCallback(
    (event: MouseEvent) => {
      if (!isDragging.current) {
        return
      }

      const delta = event.clientX - dragStartX.current
      const nextWidth = dragStartWidth.current + delta
      latestWidth.current = nextWidth

      if (rafId.current !== undefined) {
        return
      }

      rafId.current = window.requestAnimationFrame(() => {
        rafId.current = undefined
        onWidthChange(latestWidth.current)
      })
    },
    [onWidthChange]
  )

  const handleMouseUp = useCallback(() => {
    if (!isDragging.current) {
      return
    }

    isDragging.current = false
    if (rafId.current !== undefined) {
      window.cancelAnimationFrame(rafId.current)
      rafId.current = undefined
    }
    onWidthCommit(latestWidth.current)
  }, [onWidthCommit])

  useEffect(() => {
    window.addEventListener('mousemove', handleMouseMove)
    window.addEventListener('mouseup', handleMouseUp)
    return () => {
      window.removeEventListener('mousemove', handleMouseMove)
      window.removeEventListener('mouseup', handleMouseUp)
      if (rafId.current !== undefined) {
        window.cancelAnimationFrame(rafId.current)
      }
    }
  }, [handleMouseMove, handleMouseUp])

  return (
    <div
      role="separator"
      aria-orientation="vertical"
      aria-label="Resize sidebar"
      aria-valuemin={SIDEBAR_MIN_WIDTH}
      aria-valuemax={SIDEBAR_MAX_WIDTH}
      aria-valuenow={width}
      className="group flex w-1.5 shrink-0 cursor-col-resize items-stretch hover:bg-border/80"
      onMouseDown={(event) => {
        isDragging.current = true
        dragStartX.current = event.clientX
        dragStartWidth.current = width
        latestWidth.current = width
      }}
    >
      <div className="mx-auto w-px bg-border group-hover:bg-muted-foreground/40" />
    </div>
  )
}
