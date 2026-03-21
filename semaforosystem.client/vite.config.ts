import path from 'path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 2488,
    strictPort: true,
    open: true,
    proxy: {
      '/Auth': {
        target: 'https://localhost:7106',
        changeOrigin: true,
        secure: false,
        configure: (proxy) => {
          proxy.on('error', (_err, _req, res) => {
            const response = res as import('http').ServerResponse
            if (!response.headersSent) {
              response.writeHead(502, { 'Content-Type': 'application/json' })
              response.end(JSON.stringify({ message: 'Backend not ready' }))
            }
          })
        },
      },
      '/api': {
        target: 'https://localhost:7106',
        changeOrigin: true,
        secure: false,
        configure: (proxy) => {
          proxy.on('error', (_err, _req, res) => {
            const response = res as import('http').ServerResponse
            if (!response.headersSent) {
              response.writeHead(502, { 'Content-Type': 'application/json' })
              response.end(JSON.stringify({ message: 'Backend not ready' }))
            }
          })
        },
      },
    },
  },
})
