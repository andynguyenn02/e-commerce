import { useEffect, useState } from 'react'

// Delays a fast-changing value (e.g. a search box) so typing 10 characters
// fires one request instead of 10.
export function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState(value)

  useEffect(() => {
    const id = setTimeout(() => setDebounced(value), delayMs)
    return () => clearTimeout(id)
  }, [value, delayMs])

  return debounced
}
