// Données mock pour les vues Stock, en attendant le branchement à l'API
// (cf. CLAUDE.md §2 — les vues seront d'abord validées visuellement).
// Les formes ci-dessous suivent volontairement le modèle de données réel
// (product / production_batch / stock_unit, cf. docs/data-model.md) pour
// que remplacer ces fonctions par des appels API change peu les vues.

export type SaleMode = 'by_weight' | 'by_piece'
export type StockUnitStatus = 'available' | 'opened' | 'sold' | 'personal' | 'lost'

export interface Product {
  id: number
  code: string
  name: string
  saleMode: SaleMode
  /** Libellé d'unité au pluriel, tel qu'affiché à l'utilisateur ("sachets", "pièces"...). */
  unitLabel: string
  /** Unité utilisée pour le prix ("kg" ou "pièce"). */
  priceUnit: string
}

export interface StockUnit {
  id: number
  number: string
  weightGrams: number | null
  status: StockUnitStatus
}

export interface ProductionBatch {
  id: number
  batchNumber: string
  dateLabel: string
  priceLabel: string
  units: StockUnit[]
}

interface ProductWithBatches extends Product {
  batches: ProductionBatch[]
}

const catalog: ProductWithBatches[] = [
  {
    id: 1,
    code: 'SC',
    name: 'Saucisse curry',
    saleMode: 'by_weight',
    unitLabel: 'sachets',
    priceUnit: 'kg',
    batches: [
      {
        id: 1,
        batchNumber: 'SC-260902',
        dateLabel: '2 septembre 2026',
        priceLabel: '14,50 € / kg',
        units: [470, 445, 510, 480, 465, 490, 455, 500].map((g, i) => unit(i + 1, 'SC-260902', g, 'available')),
      },
      {
        id: 2,
        batchNumber: 'SC-260823',
        dateLabel: '23 août 2026',
        priceLabel: '14,00 € / kg',
        units: [430, 460, 445, 475, 450, 440].map((g, i) => unit(i + 1, 'SC-260823', g, 'available')),
      },
    ],
  },
  {
    id: 2,
    code: 'SA',
    name: 'Saucisse andalouse',
    saleMode: 'by_weight',
    unitLabel: 'sachets',
    priceUnit: 'kg',
    batches: [
      {
        id: 3,
        batchNumber: 'SA-260902',
        dateLabel: '2 septembre 2026',
        priceLabel: '14,50 € / kg',
        units: [455, 470, 440, 485, 460, 450, 445, 480, 465].map((g, i) =>
          unit(i + 1, 'SA-260902', g, 'available'),
        ),
      },
    ],
  },
  {
    id: 3,
    code: 'JS',
    name: 'Jambon sec',
    saleMode: 'by_weight',
    unitLabel: 'entiers',
    priceUnit: 'kg',
    batches: [
      {
        id: 4,
        batchNumber: 'JS-260712',
        dateLabel: '12 juillet 2026',
        priceLabel: '22,00 € / kg',
        units: [unit(1, 'JS-260712', 4600, 'available'), unit(2, 'JS-260712', 4850, 'available'), unit(3, 'JS-260712', 4350, 'opened')],
      },
    ],
  },
  {
    id: 4,
    code: 'TC',
    name: 'Terrine de campagne',
    saleMode: 'by_piece',
    unitLabel: 'pièces',
    priceUnit: 'pièce',
    batches: [
      {
        id: 5,
        batchNumber: 'TC-260830',
        dateLabel: '30 août 2026',
        priceLabel: '6,00 € / pièce',
        units: Array.from({ length: 7 }, (_, i) => unit(i + 1, 'TC-260830', null, 'available')),
      },
    ],
  },
  {
    id: 5,
    code: 'PT',
    name: 'Pâté de tête',
    saleMode: 'by_piece',
    unitLabel: 'pièces',
    priceUnit: 'pièce',
    batches: [],
  },
]

function unit(n: number, batchNumber: string, weightGrams: number | null, status: StockUnitStatus): StockUnit {
  return { id: n, number: `${batchNumber}-${n}`, weightGrams, status }
}

/** Une unité compte comme "en stock" si elle n'est pas encore sortie (vente/perso/perte). */
function isInStock(unit: StockUnit) {
  return unit.status === 'available' || unit.status === 'opened'
}

export function formatWeight(grams: number): string {
  return grams >= 1000
    ? `${(grams / 1000).toLocaleString('fr-FR', { maximumFractionDigits: 2 })} kg`
    : `${grams} g`
}

/** Accorde un libellé pluriel ("sachets") au singulier quand count <= 1. */
export function pluralize(label: string, count: number): string {
  return count > 1 ? label : label.replace(/s$/, '')
}

/** Catalogue produit "plat", pour les écrans qui n'ont pas besoin des lots (ex. sélecteur d'Ajout Stock). */
export const productCatalog: Product[] = catalog.map(({ batches: _batches, ...product }) => product)

export interface StockDashboardProduct {
  code: string
  name: string
  href: string
  meta: string
  qty: number
  qtyLabel: string
  openedLabel: string | null
  isEmpty: boolean
}

/** Résumé par produit pour le tableau de bord Stock (Stock Dashboard). */
export function getStockDashboard(): { products: StockDashboardProduct[]; totalAvailableUnits: number } {
  const products = catalog.map((product) => {
    const units = product.batches.flatMap((b) => b.units)
    const available = units.filter((u) => u.status === 'available')
    const opened = units.filter((u) => u.status === 'opened')
    const inStock = available.length + opened.length
    const totalGrams = units.filter(isInStock).reduce((sum, u) => sum + (u.weightGrams ?? 0), 0)

    const metaParts = [product.saleMode === 'by_weight' ? 'Au poids' : 'À la pièce']
    if (product.saleMode === 'by_weight' && totalGrams > 0) metaParts.push(`${formatWeight(totalGrams)} au total`)

    return {
      code: product.code,
      name: product.name,
      href: `/stock/${product.code}`,
      meta: metaParts.join(' · '),
      qty: available.length,
      qtyLabel: pluralize(product.unitLabel, available.length),
      openedLabel: opened.length > 0 ? `${opened.length} entamé${opened.length > 1 ? 's' : ''}` : null,
      isEmpty: inStock === 0,
    }
  })

  const totalAvailableUnits = products.reduce((sum, p) => sum + p.qty, 0)
  return { products, totalAvailableUnits }
}

export interface StockDetailUnit {
  number: string
  weightLabel: string | null
  status: StockUnitStatus
}

export interface StockDetailBatch {
  batchNumber: string
  dateLabel: string
  priceLabel: string
  units: StockDetailUnit[]
}

export interface StockDetail {
  name: string
  summary: string
  batches: StockDetailBatch[]
}

/** Détail d'un produit (Détail Stock) : lots + unités encore en stock (available/opened). */
export function getStockDetail(code: string): StockDetail | null {
  const product = catalog.find((p) => p.code === code.toUpperCase())
  if (!product) return null

  let count = 0
  let totalGrams = 0
  const batches: StockDetailBatch[] = product.batches.map((batch) => ({
    batchNumber: batch.batchNumber,
    dateLabel: batch.dateLabel,
    priceLabel: batch.priceLabel,
    units: batch.units
      .filter(isInStock)
      .map((u) => {
        count += 1
        totalGrams += u.weightGrams ?? 0
        return {
          number: u.number,
          weightLabel: u.weightGrams != null ? formatWeight(u.weightGrams) : null,
          status: u.status,
        }
      }),
  }))

  const summaryParts = [`${count} ${pluralize(product.unitLabel, count)} en stock`]
  if (product.saleMode === 'by_weight' && totalGrams > 0) summaryParts.push(formatWeight(totalGrams))

  return { name: product.name, summary: summaryParts.join(' · '), batches }
}
