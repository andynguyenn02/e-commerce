import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    strictPort: true,
    // FE calls the API with relative paths (no baseURL), so proxy /api to
    // the API running on the host for `npm run dev`, keeping it same-origin.
    proxy: {
      '/api': 'http://localhost:5118',
    },
  },
})
