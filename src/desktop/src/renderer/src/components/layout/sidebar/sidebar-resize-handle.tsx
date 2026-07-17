import { useCallback, useEffect, useRef } from 'react'

import { SIDEBAR_MAX_WIDTH, SIDEBAR_MIN_WIDTH } from '@/providers/project-layout-provider'

type SidebarResizeHandleProps = {
  width: number
  onWidthChange: (width: number) => void
}

export function SidebarResizeHandle({
  width,
  onWidthChange
}: SidebarResizeHandleProps): React.JSX.Element {
  const isDragging = useRef(false)
  const dragStartX = useRef(0)
  const dragStartWidth = useRef(width)

  const handleMouseMove = useCallback(
    (event: MouseEvent) => {
      if (!isDragging.current) {
        return
      }

      const delta = event.clientX - dragStartX.current
      onWidthChange(dragStartWidth.current + delta)
    },
    [onWidthChange]
  )

  const handleMouseUp = useCallback(() => {
    isDragging.current = false
  }, [])

  useEffect(() => {
    window.addEventListener('mousemove', handleMouseMove)
    window.addEventListener('mouseup', handleMouseUp)
    return () => {
      window.removeEventListener('mousemove', handleMouseMove)
      window.removeEventListener('mouseup', handleMouseUp)
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
      }}
    >
      <div className="mx-auto w-px bg-border group-hover:bg-muted-foreground/40" />
    </div>
  )
}
