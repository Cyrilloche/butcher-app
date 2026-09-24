import { existsSync, readFileSync } from 'node:fs'
import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import vuetify from 'vite-plugin-vuetify'
import { VitePWA } from 'vite-plugin-pwa'

import pkg from './package.json' with { type: 'json' }

/**
 * Version affichée dans l'app (écran de connexion). La source de vérité en
 * production est le tag Git `frontend-vX.Y.Z`, transmis par la CI au build
 * Docker (ARG APP_VERSION). Hors release, on retombe sur la version du
 * package.json suffixée `-dev` pour que l'origine soit sans ambiguïté.
 */
const appVersion = process.env.APP_VERSION || `${pkg.version}-dev`

/**
 * Essais de l'assistant vocal sur téléphone (specs/006-assistant-vocal/quickstart.md) : le micro
 * n'est accessible qu'en HTTPS, et l'API doit être jointe par la même adresse que la page.
 * `SALOIR_DEV_HTTPS=1 npm run dev -- --host` sert en HTTPS avec le certificat auto-signé de
 * l'enregistreur du corpus, et relaie /api vers le backend local. Sans la variable, rien ne change.
 */
const devHttps = process.env.SALOIR_DEV_HTTPS === '1'
const certDir = fileURLToPath(new URL('../development/assistant-corpus/.cert/', import.meta.url))
if (devHttps && !existsSync(`${certDir}cert.pem`)) {
  throw new Error(`Certificat introuvable dans ${certDir} : lancer une fois l'enregistreur (spikes/assistant-vocal/recorder).`)
}

// https://vite.dev/config/
export default defineConfig({
  define: {
    __APP_VERSION__: JSON.stringify(appVersion),
    ...(devHttps ? { 'import.meta.env.VITE_API_BASE_URL': JSON.stringify('') } : {}),
  },
  server: devHttps
    ? {
        https: { cert: readFileSync(`${certDir}cert.pem`), key: readFileSync(`${certDir}key.pem`) },
        proxy: { '/api': process.env.SALOIR_API_URL ?? 'http://localhost:5045' },
      }
    : undefined,
  plugins: [
    vue(),
    vueDevTools(),
    vuetify({ autoImport: true }),
    VitePWA({
      registerType: 'autoUpdate',
      manifest: {
        name: 'Saloir',
        short_name: 'Saloir',
        description: 'Gestion de production, stock et ventes — charcuterie artisanale',
        theme_color: '#C4623C',
        background_color: '#ECE2D0',
        display: 'standalone',
        icons: [
          {
            src: 'pwa-192x192.png',
            sizes: '192x192',
            type: 'image/png',
          },
          {
            src: 'pwa-512x512.png',
            sizes: '512x512',
            type: 'image/png',
          },
        ],
      },
    }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
})
