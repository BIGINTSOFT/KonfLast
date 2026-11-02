import react from '@vitejs/plugin-react'
import { spawn, spawnSync, type ChildProcess } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import { defineConfig, type Plugin } from 'vite'

// Geliştirmede API çağrıları ERP.Api'ye proxy'lenir; böylece tarayıcı açısından
// tek origin vardır ve CORS gerekmez.
const apiTarget = process.env.ERP_API_URL ?? 'http://localhost:5272'

// `npm run dev` ile ERP.Api de ayağa kalkar. API zaten çalışıyorsa (Visual Studio,
// ayrı terminal) dokunulmaz; ERP_API_AUTOSTART=0 ile tamamen kapatılabilir.
function erpApi(): Plugin {
  let api: ChildProcess | undefined

  const stop = () => {
    if (!api?.pid || api.exitCode !== null) return
    // `dotnet run` asıl uygulamayı alt süreç olarak açar; ağacın tamamı kapatılmalı.
    if (process.platform === 'win32') spawnSync('taskkill', ['/pid', String(api.pid), '/T', '/F'], { stdio: 'ignore' })
    else api.kill()
    api = undefined
  }

  return {
    name: 'erp-api',
    apply: 'serve',
    async configureServer(server) {
      if (process.env.ERP_API_AUTOSTART === '0' || process.env.ERP_API_URL) return
      // /health veritabanı yoksa 503 döner; herhangi bir yanıt API'nin ayakta olduğunu gösterir.
      const running = await fetch(`${apiTarget}/health`, { signal: AbortSignal.timeout(1500) }).then(
        () => true,
        () => false,
      )
      if (running) return

      const project = fileURLToPath(new URL('../src/ERP.Api', import.meta.url))
      server.config.logger.info(`ERP.Api başlatılıyor (${apiTarget})...`)
      api = spawn('dotnet', ['run', '--project', project, '--launch-profile', 'Tüm modüller'], { stdio: 'inherit' })
      api.on('exit', (code) => {
        if (api) server.config.logger.warn(`ERP.Api kapandı (kod ${code}).`)
        api = undefined
      })
      server.httpServer?.on('close', stop)
      process.on('exit', stop)
      process.on('SIGINT', () => process.exit())
      process.on('SIGTERM', () => process.exit())
    },
  }
}

export default defineConfig({
  plugins: [react(), erpApi()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
      '/health': { target: apiTarget, changeOrigin: true },
      '/hubs': { target: apiTarget, changeOrigin: true, ws: true },
    },
  },
})
