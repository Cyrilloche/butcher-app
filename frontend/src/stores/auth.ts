import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import * as authApi from '@/api/auth'
import type { MeDto } from '@/api/types'

/**
 * Session auth (ADR-009) : access token JWT gardé en mémoire uniquement
 * (jamais localStorage), 15 min de durée de vie, perdu au rechargement —
 * c'est voulu. Le refresh token (cookie httpOnly, Path=/api/auth) sert à
 * en récupérer un nouveau silencieusement via ensureReady().
 *
 * Le compte connecté (ADR-011) est chargé à l'ouverture de la session, pas à chaque rafraîchissement
 * de jeton : `me()` passe par `apiFetch`, qui rafraîchit lui-même sur un 401, et le recharger à ce
 * moment-là ouvrirait une boucle. Il sert à adapter l'interface ; le serveur, lui, vérifie les droits
 * à chaque requête.
 */
export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(null)
  const expiresAtUtc = ref<string | null>(null)
  const account = ref<MeDto | null>(null)

  const isAuthenticated = computed(() => accessToken.value !== null)
  const isAdmin = computed(() => account.value?.role === 'admin')

  let readyPromise: Promise<void> | null = null

  function setSession(auth: { accessToken: string; expiresAtUtc: string }) {
    accessToken.value = auth.accessToken
    expiresAtUtc.value = auth.expiresAtUtc
  }

  function clearSession() {
    accessToken.value = null
    expiresAtUtc.value = null
    account.value = null
  }

  /** Relit le compte connecté, par exemple après avoir changé son propre nom. */
  async function loadAccount() {
    account.value = await authApi.me()
  }

  async function login(email: string, password: string) {
    setSession(await authApi.login({ email, password }))
    try {
      await loadAccount()
    } catch (err) {
      // Une session sans compte ne saurait pas ce qu'elle a le droit d'afficher : on n'en garde pas.
      clearSession()
      throw err
    }
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
      readyPromise = refresh()
        .then(loadAccount)
        .catch(() => {
          clearSession()
        })
    }
    return readyPromise
  }

  return {
    accessToken,
    expiresAtUtc,
    account,
    isAuthenticated,
    isAdmin,
    login,
    logout,
    refresh,
    loadAccount,
    ensureReady,
  }
})
