import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    plugins: [react()],
    server: {
      proxy: {
        '/api': {
          // Docker Compose là cách chạy chuẩn của nhóm; có thể ghi đè bằng
          // API_PROXY_TARGET=http://localhost:5152 khi chạy `dotnet run`.
          target: env.API_PROXY_TARGET || 'http://localhost:8081',
          changeOrigin: true,
        },
      },
    },
  }
})
