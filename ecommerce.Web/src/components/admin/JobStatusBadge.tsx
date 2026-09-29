import type { InventoryJob, InventoryJobStatus } from '../../api/types'
import { isJobStalled } from '../../hooks/useInventoryJobs'
import { Badge } from '../ui/badge'

// Keys are the backend's raw statuses; labels are what the admin needs to know.
const LABELS: Record<InventoryJobStatus, string> = {
  Stored: 'Waiting',
  Processing: 'Processing',
  Accepted: 'Done', // the worker's success state
  Done: 'Done',
  Failed: 'Failed',
}

const TONES: Record<InventoryJobStatus, 'neutral' | 'accent' | 'positive'> = {
  Stored: 'neutral',
  Processing: 'accent',
  Accepted: 'positive',
  Done: 'positive',
  Failed: 'accent',
}

export function JobStatusBadge({ job }: { job: InventoryJob }) {
  if (isJobStalled(job)) {
    return (
      <span title={`Still "${job.status}" after 10+ minutes. The server may have restarted; upload the file again.`}>
        <Badge tone="caution">Stalled</Badge>
      </span>
    )
  }

  // Failed gets a filled accent treatment so it stays visually distinct from stalled's caution tone.
  return (
    <span title={job.status}>
      <Badge
        tone={TONES[job.status] ?? 'neutral'}
        className={job.status === 'Failed' ? 'border-accent bg-accent text-paper' : undefined}
      >
        {job.status === 'Processing' && <span className="size-1.5 rounded-full bg-current animate-pulse" />}
        {LABELS[job.status] ?? job.status}
      </Badge>
    </span>
  )
}
