import { useEffect, useRef, useState, type RefObject } from 'react'

export function useElementWidth<T extends HTMLElement>(
  externalRef?: RefObject<T | null>
): { width: number; ref: RefObject<T | null> } {
  const internalRef = useRef<T | null>(null)
  const ref = externalRef ?? internalRef
  const [width, setWidth] = useState(0)

  useEffect(() => {
    const element = ref.current
    if (!element) {
      return
    }

    const updateWidth = (): void => {
      setWidth(element.getBoundingClientRect().width)
    }

    updateWidth()

    const observer = new ResizeObserver(updateWidth)
    observer.observe(element)
    return () => observer.disconnect()
  }, [ref])

  return { width, ref }
}
