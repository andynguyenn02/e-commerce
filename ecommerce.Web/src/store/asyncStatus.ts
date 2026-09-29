// One string status instead of isLoading: boolean, so "never fetched" (idle)
// and "fetched, nothing found" (success + empty) render differently.
export type AsyncStatus = 'idle' | 'loading' | 'success' | 'error'

export interface AsyncState {
  status: AsyncStatus
  error: string | null
}

export const initialAsyncState: AsyncState = { status: 'idle', error: null }
