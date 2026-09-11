import { ApiError } from '@/api/http'

/**
 * Message à afficher pour un refus du serveur.
 *
 * Les messages d'`ApiError` viennent du backend, qui les rédige déjà en français et nomme la
 * donnée en cause (le poids de l'unité, la dernière ligne d'une vente). On les affiche tels
 * quels : reformuler côté client reviendrait à répliquer les règles métier, dont le serveur est
 * le seul garant (principe II de la constitution). Le repli ne sert qu'aux erreurs sans corps —
 * réseau coupé, serveur injoignable.
 */
export function apiErrorMessage(err: unknown, fallback: string): string {
  return err instanceof ApiError ? err.message : fallback
}
