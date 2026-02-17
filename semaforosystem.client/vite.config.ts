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
      },
      '/api': {
        target: 'https://localhost:7106',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
