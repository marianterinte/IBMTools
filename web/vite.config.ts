import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Spec: Documentation/dev-prism-ai-specs/04_ARCHITECTURE.md. Development proxy only.
export default defineConfig({
  plugins: [react()],
  server: {
    host: '127.0.0.1',
    port: 5173,
    strictPort: true,
    proxy: { '/api/v1/health': { target: 'https://localhost:5081', changeOrigin: true } },
  },
})
