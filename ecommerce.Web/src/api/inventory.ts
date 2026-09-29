import { api } from './client'
import type { InventoryJob } from './types'

export const getJobs = () => api.get<InventoryJob[]>('/api/inventory/jobs').then((r) => r.data)

// Returns 202 as soon as the file is stored; processing happens in a background worker.
export const uploadInventoryFile = (file: File, onProgress?: (percent: number) => void) => {
  const form = new FormData()
  form.append('formFile', file) // must match the IFormFile parameter name
  return api
    .post<{ jobId: string }>('/api/inventory/job', form, {
      onUploadProgress: (e) => e.total && onProgress?.(Math.round((e.loaded / e.total) * 100)),
    })
    .then((r) => r.data)
}
