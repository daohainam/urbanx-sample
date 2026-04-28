import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react-swc'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath, URL } from 'node:url'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const envVars = loadEnv(mode, process.cwd(), '')
  const proxyTarget = envVars.VITE_DEV_PROXY_TARGET || 'http://localhost:5000'

  return {
    plugins: [react(), tailwindcss()],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      proxy: {
        '/api': { target: proxyTarget, changeOrigin: true },
        '/bff': { target: proxyTarget, changeOrigin: true },
        '/signin-oidc': { target: proxyTarget, changeOrigin: true },
        '/signout-callback-oidc': { target: proxyTarget, changeOrigin: true },
        '/connect': { target: proxyTarget, changeOrigin: true },
        '/.well-known': { target: proxyTarget, changeOrigin: true },
      },
    },
  }
})
