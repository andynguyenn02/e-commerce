import { useCallback, useEffect, useRef, useState } from 'react'
import { getErrorMessage } from '../api/client'
import { getJobs } from '../api/inventory'
import type { InventoryJob, InventoryJobStatus } from '../api/types'
import type { AsyncStatus } from '../store/asyncStatus'
import { parseApiDate } from '../utils/format'

const POLL_MS = 2500
// InventoryJobWorker goes Stored -> Processing -> Accepted (success) | Failed.
// "Accepted" is its success state; "Done" exists in the enum but is never set.
const IN_PROGRESS: InventoryJobStatus[] = ['Stored', 'Processing']

// The job queue is in memory: if the API restarts mid-job, that job never
// finishes. Give up on jobs with no result after this long, or we'd poll forever.
const STALL_AFTER_MS = 10 * 60 * 1000

export const isJobStalled = (job: InventoryJob, now = Date.now()) =>
  IN_PROGRESS.includes(job.status) && now - parseApiDate(job.createdAt).getTime() > STALL_AFTER_MS

export const isJobActive = (job: InventoryJob) => IN_PROGRESS.includes(job.status) && !isJobStalled(job)

// Loads the job list and keeps refreshing it only while some job is unfinished.
// Uses a setTimeout chain (next poll scheduled after the previous one returns)
// instead of setInterval, so a slow response can never stack up requests.
export function useInventoryJobs() {
  const [status, setStatus] = useState<AsyncStatus>('loading')
  const [jobs, setJobs] = useState<InventoryJob[]>([])
  const [error, setError] = useState<string | null>(null)
  // Background refresh failed; the table keeps the last good data.
  const [pollError, setPollError] = useState<string | null>(null)

  const timer = useRef<number | undefined>(undefined)
  const mounted = useRef(true)
  const hasData = useRef(false)

  const load = useCallback(async function tick(): Promise<void> {
    window.clearTimeout(timer.current)
    try {
      const data = await getJobs()
      if (!mounted.current) return
      hasData.current = true
      setJobs(data)
      setStatus('success')
      setError(null)
      setPollError(null)
      if (data.some(isJobActive)) timer.current = window.setTimeout(tick, POLL_MS)
    } catch (err) {
      if (!mounted.current) return
      const message = getErrorMessage(err)
      if (hasData.current) {
        // Keep the table, retry on the next tick.
        setPollError(message)
        timer.current = window.setTimeout(tick, POLL_MS)
      } else {
        setError(message)
        setStatus('error')
      }
    }
  }, [])

  useEffect(() => {
    mounted.current = true
    // First fetch goes through the same timer the polling uses.
    timer.current = window.setTimeout(load, 0)
    // Leaving the page stops polling for good: no requests keep running in the background.
    return () => {
      mounted.current = false
      window.clearTimeout(timer.current)
    }
  }, [load])

  const retry = useCallback(() => {
    setStatus('loading')
    load()
  }, [load])

  return { status, jobs, error, pollError, polling: jobs.some(isJobActive), refresh: load, retry }
}
