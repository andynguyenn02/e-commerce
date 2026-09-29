import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Provider } from 'react-redux'
import { RouterProvider } from 'react-router-dom'
import { Toaster } from 'sonner'
import { store } from './store'
import { router } from './routes/router'
import { initSession } from './session'
import './index.css'

initSession()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Provider store={store}>
      <RouterProvider router={router} />
      {/* Square, ruled toasts so confirmations match the rest of the app
          instead of arriving as rounded cards with a drop shadow. */}
      <Toaster
        position="top-right"
        toastOptions={{
          unstyled: true,
          classNames: {
            toast:
              'flex w-full items-start gap-2.5 rounded-sm border border-ink bg-paper px-3.5 py-3 text-sm text-ink',
            title: 'font-medium',
            description: 'text-[13px] text-muted',
            actionButton:
              'ml-auto shrink-0 rounded-sm bg-ink px-2 py-1 text-[12px] font-medium text-paper hover:bg-ink-soft',
            cancelButton: 'ml-auto shrink-0 text-[12px] text-muted hover:text-ink',
            icon: 'shrink-0',
            error: 'border-accent text-accent-ink',
            success: 'border-positive text-ink',
          },
        }}
      />
    </Provider>
  </StrictMode>,
)
