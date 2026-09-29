import { useCallback, useEffect, useState } from 'react'
import { getErrorMessage } from '../api/client'
import type { AsyncStatus } from '../store/asyncStatus'

interface State<T> {
  status: AsyncStatus
  data: T | null
  error: string | null
}

// Same 4-state contract as the slices, for data only one page needs.
// `load` must be stable (a module-level function or useCallback).
export function useAsync<T>(load: () => Promise<T>) {
  // Starts in 'loading' because the request fires on mount.
  const [state, setState] = useState<State<T>>({ status: 'loading', data: null, error: null })
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    let cancelled = false
    load()
      .then((data) => !cancelled && setState({ status: 'success', data, error: null }))
      .catch((err) => !cancelled && setState({ status: 'error', data: null, error: getErrorMessage(err) }))
    return () => {
      cancelled = true
    }
  }, [load, attempt])

  const reload = useCallback(() => {
    setState((s) => ({ ...s, status: 'loading', error: null }))
    setAttempt((n) => n + 1)
  }, [])

  return { ...state, reload }
}
