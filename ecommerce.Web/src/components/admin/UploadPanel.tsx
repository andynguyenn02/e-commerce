import { useRef, useState, type DragEvent } from 'react'
import { toast } from 'sonner'
import { Upload } from 'lucide-react'
import { getErrorMessage } from '../../api/client'
import { uploadInventoryFile } from '../../api/inventory'
import { cn } from '../../lib/cn'
import { Button } from '../ui/button'

// Same list as UploadCommandHandler. The backend check is case-sensitive,
// so "STOCK.CSV" is rejected there too — we tell the user up front.
const ALLOWED = ['.csv', '.xls', '.xlsx']
// Kestrel's default request body limit (~30 MB); larger uploads get a 413.
const MAX_BYTES = 30 * 1024 * 1024

function checkFile(file: File): string | null {
  const dot = file.name.lastIndexOf('.')
  const ext = dot >= 0 ? file.name.slice(dot) : ''
  if (!ALLOWED.includes(ext)) {
    return ALLOWED.includes(ext.toLowerCase())
      ? `Rename the file with a lowercase extension (${ext.toLowerCase()})`
      : 'Only .csv, .xls or .xlsx files are allowed'
  }
  if (file.size === 0) return 'The file is empty'
  if (file.size > MAX_BYTES) return 'File is larger than 30 MB'
  return null
}

const formatSize = (bytes: number) =>
  bytes < 1024 * 1024 ? `${(bytes / 1024).toFixed(1)} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`

export function UploadPanel({ onUploaded }: { onUploaded: () => void }) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [dragging, setDragging] = useState(false)
  const [progress, setProgress] = useState<number | null>(null)
  const uploading = progress !== null

  const pick = (picked: File | undefined) => {
    if (!picked) return
    setFile(picked)
    setError(checkFile(picked))
  }

  const onDrop = (e: DragEvent) => {
    e.preventDefault()
    setDragging(false)
    if (!uploading) pick(e.dataTransfer.files[0])
  }

  const reset = () => {
    setFile(null)
    setError(null)
    if (inputRef.current) inputRef.current.value = ''
  }

  const onUpload = async () => {
    if (!file) {
      setError('Please choose a file')
      return
    }
    const problem = checkFile(file)
    if (problem) {
      setError(problem)
      return
    }
    setProgress(0)
    try {
      await uploadInventoryFile(file, setProgress)
      // 202: stored and queued, NOT processed yet. The table shows progress.
      toast.success(`${file.name} received, processing in the background`)
      reset()
      onUploaded()
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setProgress(null)
    }
  }

  return (
    <div className="rounded-sm border border-rule p-5">
      <div
        className={cn(
          'rounded-sm border-2 border-dashed border-rule px-6 py-10 text-center cursor-pointer transition-colors',
          dragging && 'border-ink bg-paper-sunk',
          error && 'border-accent',
        )}
        onDragOver={(e) => {
          e.preventDefault()
          setDragging(true)
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={onDrop}
        onClick={() => !uploading && inputRef.current?.click()}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => (e.key === 'Enter' || e.key === ' ') && inputRef.current?.click()}
      >
        <input
          ref={inputRef}
          type="file"
          accept={ALLOWED.join(',')}
          hidden
          onChange={(e) => pick(e.target.files?.[0])}
        />
        <Upload size={20} className="mx-auto mb-2 text-muted" aria-hidden />
        {file ? (
          <>
            <div>{file.name}</div>
            <div className="text-[13px] text-muted">{formatSize(file.size)} · click to choose another file</div>
          </>
        ) : (
          <>
            <div>Drop a file here or click to choose</div>
            <div className="text-[13px] text-muted">.csv, .xls or .xlsx · up to 30 MB</div>
          </>
        )}
      </div>

      {error && <p className="mt-2 text-[13px] text-accent">{error}</p>}

      <div className="mt-4 flex justify-end gap-2">
        {file && !uploading && (
          <Button variant="outline" onClick={reset}>
            Clear
          </Button>
        )}
        <Button disabled={uploading || !!(file && error)} onClick={onUpload}>
          {uploading ? `Uploading ${progress}%` : 'Upload'}
        </Button>
      </div>
    </div>
  )
}
