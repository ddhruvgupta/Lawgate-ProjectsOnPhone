/// <reference types="vitest" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    headers: {
      // Prevent the browser from caching source files and dep chunks.
      // Without this, a Vite cache clear (new dep hashes) leaves the browser
      // serving stale main.tsx that imports now-dead old hash URLs → 504s.
      'Cache-Control': 'no-store',
    },
    // Use polling for file watching in Docker on Windows (inotify doesn't
    // propagate host filesystem events into the container via volume mounts).
    watch: {
      usePolling: true,
      interval: 500,
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    exclude: ['node_modules', 'dist'],
  },
})
