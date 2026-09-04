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

// https://vite.dev/config/
export default defineConfig({
  define: {
    __APP_VERSION__: JSON.stringify(appVersion),
  },
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
