import { useLayoutEffect, useRef } from 'react'

export function useLiveRef<T>(value: T): React.MutableRefObject<T> {
  const ref = useRef(value)
  useLayoutEffect(() => {
    ref.current = value
  })
  return ref
}
