import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import * as authApi from '@/api/auth'

/**
 * Session auth (ADR-009) : access token JWT gardé en mémoire uniquement
 * (jamais localStorage), 15 min de durée de vie, perdu au rechargement —
 * c'est voulu. Le refresh token (cookie httpOnly, Path=/api/auth) sert à
 * en récupérer un nouveau silencieusement via ensureReady().
 */
export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(null)
  const expiresAtUtc = ref<string | null>(null)
  const isAuthenticated = computed(() => accessToken.value !== null)

  let readyPromise: Promise<void> | null = null

  function setSession(auth: { accessToken: string; expiresAtUtc: string }) {
    accessToken.value = auth.accessToken
    expiresAtUtc.value = auth.expiresAtUtc
  }

  function clearSession() {
    accessToken.value = null
    expiresAtUtc.value = null
  }

  async function login(email: string, password: string) {
    setSession(await authApi.login({ email, password }))
  }

  async function refresh() {
    setSession(await authApi.refresh())
  }

  async function logout() {
    clearSession()
    readyPromise = null
    try {
      await authApi.logout()
    } catch {
      // Le cookie est de toute façon effacé côté serveur si la requête aboutit ;
      // en cas d'échec réseau, l'état local est déjà nettoyé, rien de plus à faire.
    }
  }

  /**
   * À appeler une fois avant la première décision de garde de route : tente
   * un refresh silencieux (cookie) pour retrouver une session après un F5.
   * Un échec est normal pour un visiteur non connecté — pas une erreur à
   * remonter, isAuthenticated restera simplement false.
   */
  function ensureReady(): Promise<void> {
    if (!readyPromise) {
      readyPromise = refresh().catch(() => {
        clearSession()
      })
    }
    return readyPromise
  }

  return { accessToken, expiresAtUtc, isAuthenticated, login, logout, refresh, ensureReady }
})
