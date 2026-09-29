import { useState } from 'react'
import type { InventoryJob } from '../../api/types'
import { JobStatusBadge } from '../../components/admin/JobStatusBadge'
import { UploadPanel } from '../../components/admin/UploadPanel'
import { Dialog, DialogActions } from '../../components/ui/dialog'
import { Button } from '../../components/ui/button'
import { EmptyState, ErrorState, Spinner } from '../../components/ui/feedback'
import { TableFrame, Th, Td } from '../../components/ui/table'
import { useInventoryJobs } from '../../hooks/useInventoryJobs'
import { formatDate } from '../../utils/format'

const ERROR_PREVIEW = 60

export default function AdminInventoryPage() {
  const { status, jobs, error, pollError, polling, refresh, retry } = useInventoryJobs()
  const [errorJob, setErrorJob] = useState<InventoryJob | null>(null)

  return (
    <section>
      <h1 className="mb-5">Inventory upload</h1>

      {/* After upload, refresh at once: the new job appears and polling starts. */}
      <UploadPanel onUploaded={refresh} />

      <div className="mb-5 mt-8 flex items-baseline justify-between gap-4 rule-b pb-4">
        <h2 className="m-0">Import jobs</h2>
        {polling && (
          <span className="flex items-center gap-1.5 text-[13px] text-accent">
            <span className="size-1.5 rounded-full bg-current animate-pulse" /> Updating live
          </span>
        )}
        {pollError && <span className="text-[13px] text-accent">Couldn't refresh: {pollError}. Retrying</span>}
      </div>

      {status === 'loading' && <Spinner label="Loading jobs" />}

      {status === 'error' && <ErrorState message={error} onRetry={retry} />}

      {status === 'success' && jobs.length === 0 && (
        <EmptyState>No uploads yet. Upload a file above to start an import.</EmptyState>
      )}

      {status === 'success' && jobs.length > 0 && (
        <TableFrame>
          <thead>
            <tr>
              <Th>File</Th>
              <Th>Status</Th>
              <Th>Uploaded</Th>
              <Th>Email sent</Th>
              <Th>Error</Th>
            </tr>
          </thead>
          <tbody>
            {jobs.map((job) => (
              <tr key={job.jobId}>
                <Td className="font-mono text-[13px]">{job.fileName}</Td>
                <Td>
                  <JobStatusBadge job={job} />
                </Td>
                <Td>{formatDate(job.createdAt)}</Td>
                <Td>{job.emailSentAt ? formatDate(job.emailSentAt) : <span className="text-muted">—</span>}</Td>
                <Td>
                  {job.errorMessage ? (
                    <Button variant="quiet" size="sm" className="hover:text-accent" title="Show full error" onClick={() => setErrorJob(job)}>
                      {job.errorMessage.length > ERROR_PREVIEW
                        ? `${job.errorMessage.slice(0, ERROR_PREVIEW)}…`
                        : job.errorMessage}
                    </Button>
                  ) : (
                    <span className="text-muted">—</span>
                  )}
                </Td>
              </tr>
            ))}
          </tbody>
        </TableFrame>
      )}

      {errorJob && (
        <Dialog title={`Import failed: ${errorJob.fileName}`} onClose={() => setErrorJob(null)}>
          <pre className="whitespace-pre-wrap break-words rounded-sm bg-accent-wash p-3 text-[13px] text-accent-ink max-h-[50vh] overflow-auto">
            {errorJob.errorMessage}
          </pre>
          <DialogActions>
            <Button variant="outline" onClick={() => setErrorJob(null)}>
              Close
            </Button>
          </DialogActions>
        </Dialog>
      )}
    </section>
  )
}
