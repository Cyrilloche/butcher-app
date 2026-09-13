/**
 * Tri des tableaux sur écran large (FR-018), partagé par Ventes, Stock, Produits et Clients.
 *
 * Fonctions pures : chaque écran fournit sa comparaison par colonne, le sens et le départage restent
 * les mêmes partout.
 */

export type SortDirection = 'asc' | 'desc'

export interface TableSort<K extends string> {
  key: K
  direction: SortDirection
}

/** Colonne d'un `AppSortableTable`. */
export interface TableColumn<K extends string> {
  /** Identifiant de la colonne, unique dans le tableau. */
  id: string
  label: string
  /** Clé de tri ; sans elle, l'en-tête n'est pas cliquable. */
  sortKey?: K
  /** Sens au premier clic : `desc` pour une date, un montant ou une quantité. */
  firstDirection?: SortDirection
  numeric?: boolean
}

/**
 * Tri obtenu en cliquant sur une colonne : la même colonne inverse le sens, une autre part dans son
 * sens naturel (`firstDirection`) — du plus grand pour une date ou un montant, de A à Z pour un nom.
 */
export function nextSort<K extends string>(
  current: TableSort<K>,
  key: K,
  firstDirection: SortDirection = 'asc',
): TableSort<K> {
  if (current.key === key) return { key, direction: current.direction === 'asc' ? 'desc' : 'asc' }
  return { key, direction: firstDirection }
}

/** Comparaison de libellés en français, sans tenir compte des accents ni de la casse. */
export function compareText(a: string, b: string): number {
  return a.localeCompare(b, 'fr', { sensitivity: 'base' })
}

/**
 * Trie sans modifier la liste reçue. `tiebreak` départage les égalités, toujours dans le même sens,
 * pour que l'ordre reste stable d'un tri à l'autre.
 */
export function sortRows<T, K extends string>(
  rows: T[],
  sort: TableSort<K>,
  compare: (key: K, a: T, b: T) => number,
  tiebreak: (a: T, b: T) => number = () => 0,
): T[] {
  const sign = sort.direction === 'asc' ? 1 : -1
  return [...rows].sort((a, b) => sign * compare(sort.key, a, b) || tiebreak(a, b))
}
