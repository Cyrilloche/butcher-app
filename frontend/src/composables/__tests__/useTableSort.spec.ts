import { describe, expect, it } from 'vitest'
import { compareText, nextSort, sortRows } from '../useTableSort'

describe('nextSort', () => {
  it('inverse le sens quand on reclique sur la colonne triée', () => {
    expect(nextSort({ key: 'name', direction: 'asc' }, 'name')).toEqual({ key: 'name', direction: 'desc' })
    expect(nextSort({ key: 'name', direction: 'desc' }, 'name', 'desc')).toEqual({ key: 'name', direction: 'asc' })
  })

  it('part dans le sens naturel de la nouvelle colonne', () => {
    expect(nextSort<string>({ key: 'name', direction: 'desc' }, 'total', 'desc')).toEqual({ key: 'total', direction: 'desc' })
    expect(nextSort<string>({ key: 'total', direction: 'desc' }, 'name')).toEqual({ key: 'name', direction: 'asc' })
  })
})

describe('compareText', () => {
  it('ignore accents et casse', () => {
    expect(compareText('élodie', 'Elodie')).toBe(0)
    expect(compareText('Émile', 'Fabien')).toBeLessThan(0)
  })
})

describe('sortRows', () => {
  const rows = [
    { id: 1, name: 'Chorizo', qty: 3 },
    { id: 2, name: 'Andouille', qty: 5 },
    { id: 3, name: 'Boudin', qty: 3 },
  ]
  const compare = (key: 'name' | 'qty', a: (typeof rows)[number], b: (typeof rows)[number]) =>
    key === 'name' ? compareText(a.name, b.name) : a.qty - b.qty

  it('trie dans les deux sens sans modifier la liste reçue', () => {
    expect(sortRows(rows, { key: 'name', direction: 'asc' }, compare).map((r) => r.id)).toEqual([2, 3, 1])
    expect(sortRows(rows, { key: 'name', direction: 'desc' }, compare).map((r) => r.id)).toEqual([1, 3, 2])
    expect(rows.map((r) => r.id)).toEqual([1, 2, 3])
  })

  it('départage les égalités toujours dans le même sens', () => {
    const byName = (a: (typeof rows)[number], b: (typeof rows)[number]) => compareText(a.name, b.name)
    expect(sortRows(rows, { key: 'qty', direction: 'asc' }, compare, byName).map((r) => r.id)).toEqual([3, 1, 2])
    expect(sortRows(rows, { key: 'qty', direction: 'desc' }, compare, byName).map((r) => r.id)).toEqual([2, 3, 1])
  })
})
