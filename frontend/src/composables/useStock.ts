import { listProducts } from '@/api/products'
import { listProductionBatches } from '@/api/productionBatches'
import { listStockUnits } from '@/api/stockUnits'
import type { ProductDto, ProductionBatchDto, StockUnitDto, SaleMode } from '@/api/types'

export type { SaleMode }
export type StockUnitStatus = StockUnitDto['status']

export interface Product {
  id: number
  code: string
  name: string
  saleMode: SaleMode
  /** Nom générique d'unité de comptage — plus de personnalisation par produit (décision 2026-09-04). */
  unitLabel: string
  /** Unité affichée à côté du prix : "kg" au poids, "pièce" à la pièce (RG-02/03) — dérivé de saleMode. */
  priceUnit: string
  allowPartialSale: boolean
}

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

export interface StockDetailUnit {
  id: number
  number: string
  /** Poids pesé en kg (null pour un produit à la pièce). */
  weightKg: number | null
  /** Poids encore vendable en kg, tel que le serveur l'a calculé — sert aux mouvements de sortie. */
  remainingKg: number | null
  /** Poids pesé, mis en forme. C'est le poids d'origine, il ne bouge jamais. */
  weightLabel: string | null
  /**
   * Poids encore vendable, mis en forme, `null` si l'unité n'a pas de poids. Vient du serveur
   * (RG-05) : ne jamais le recalculer ici.
   */
  remainingLabel: string | null
  /** Vrai quand l'unité est entamée et qu'il ne reste plus rien à vendre : une clôture est due. */
  isEmptied: boolean
  status: StockUnitStatus
}

export interface StockDetailBatch {
  /** Identifiant de la fournée — nécessaire pour la supprimer depuis le détail stock. */
  id: number
  /**
   * « 2ᵉ fournée » quand plusieurs fabrications partagent la même date, `null` sinon. Libellé
   * d'affichage calculé à la lecture : une fournée n'a pas de numéro, c'est l'unité qui en porte un.
   */
  dayRankLabel: string | null
  dateLabel: string
  priceLabel: string
  /** Prix brut du lot et unité d'affichage (« kg », « pièce ») — pour corriger le prix (RG-10). */
  salePrice: number
  priceUnit: string
  /**
   * Champs du lot absents de l'écran (RF-08/RF-09 reportées en V2) mais écrasés par un `PUT` :
   * ils repartent tels quels à chaque correction du prix, sous peine d'être effacés.
   */
  untouched: { rawMaterialRef: string | null; expiryDate: string | null; notes: string | null }
  /** Nom du compte qui a enregistré la fournée ; `null` avant les comptes nominatifs (RF-27). */
  createdByName: string | null
  units: StockDetailUnit[]
}

export interface StockDetail {
  name: string
  /** Faux pour un produit désactivé : l'écran reste consultable, il est simplement signalé. */
  isActive: boolean
  summary: string
  batches: StockDetailBatch[]
}

function toProduct(dto: ProductDto): Product {
  return {
    id: dto.id,
    code: dto.code,
    name: dto.name,
    saleMode: dto.saleMode,
    unitLabel: dto.saleMode === 'by_weight' ? 'unités' : 'pièces',
    priceUnit: dto.saleMode === 'by_weight' ? 'kg' : 'pièce',
    allowPartialSale: dto.allowPartialSale,
  }
}

/** Une unité compte comme "en stock" si elle n'est pas encore sortie (vente/perso/perte). */
function isInStock(unit: StockUnitDto) {
  return unit.status === 'available' || unit.status === 'opened'
}

/** weight (kg, decimal(10,3)) -> grammes entiers, en évitant les artefacts de flottant. */
function weightToGrams(weightKg: number): number {
  return Math.round(weightKg * 1000)
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

function formatDateLabel(dateOnly: string): string {
  // dateOnly: "YYYY-MM-DD" (System.Text.Json DateOnly).
  return new Date(`${dateOnly}T00:00:00`).toLocaleDateString('fr-FR', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  })
}

function formatPriceLabel(salePrice: number, priceUnit: string): string {
  return `${salePrice.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} € / ${priceUnit}`
}

/** batch_number du lot parent, tri par production_date décroissante (le plus récent en premier). */
function sortBatchesRecentFirst(batches: ProductionBatchDto[]): ProductionBatchDto[] {
  return [...batches].sort((a, b) => b.productionDate.localeCompare(a.productionDate))
}

/**
 * Distingue les fournées d'un même jour par leur rang, faute de numéro à leur donner.
 * Une seule fabrication ce jour-là ne mérite aucun rang : « Fabriqué le 10/09 » suffit.
 */
function dayRankLabels(batches: ProductionBatchDto[]): Map<number, string | null> {
  const byDate = new Map<string, ProductionBatchDto[]>()
  for (const batch of batches) {
    const list = byDate.get(batch.productionDate) ?? []
    list.push(batch)
    byDate.set(batch.productionDate, list)
  }

  const labels = new Map<number, string | null>()
  for (const sameDay of byDate.values()) {
    const chronological = [...sameDay].sort((a, b) => a.id - b.id)
    chronological.forEach((batch, index) => {
      labels.set(batch.id, sameDay.length > 1 ? `${index + 1}${index === 0 ? 'ʳᵉ' : 'ᵉ'} fournée` : null)
    })
  }
  return labels
}

export async function listActiveProducts(): Promise<Product[]> {
  const products = await listProducts(false)
  return products.map(toProduct)
}

export async function getStockDashboard(): Promise<{
  products: StockDashboardProduct[]
  totalUnitsInStock: number
}> {
  const [productDtos, batchDtos, unitDtos] = await Promise.all([
    listProducts(false),
    listProductionBatches(),
    listStockUnits(),
  ])

  const batchesByProduct = new Map<number, ProductionBatchDto[]>()
  for (const batch of batchDtos) {
    const list = batchesByProduct.get(batch.productId) ?? []
    list.push(batch)
    batchesByProduct.set(batch.productId, list)
  }
  const unitsByBatch = new Map<number, StockUnitDto[]>()
  for (const unit of unitDtos) {
    const list = unitsByBatch.get(unit.batchId) ?? []
    list.push(unit)
    unitsByBatch.set(unit.batchId, list)
  }

  const products = productDtos.map((productDto) => {
    const product = toProduct(productDto)
    const batches = batchesByProduct.get(productDto.id) ?? []
    const units = batches.flatMap((b) => unitsByBatch.get(b.id) ?? [])
    const available = units.filter((u) => u.status === 'available')
    const opened = units.filter((u) => u.status === 'opened')
    const inStock = available.length + opened.length
    // Même règle que le résumé du détail d'un produit : les deux écrans doivent annoncer le même
    // poids, sous peine d'user la confiance dans l'outil plus vite que l'absence d'information.
    const totalGrams = units
      .filter(isInStock)
      .reduce((sum, u) => sum + (u.remainingWeight != null ? weightToGrams(u.remainingWeight) : 0), 0)

    const metaParts = [product.saleMode === 'by_weight' ? 'Au poids' : 'À la pièce']
    if (product.saleMode === 'by_weight' && inStock > 0) {
      metaParts.push(`${formatWeight(totalGrams)} à vendre`)
    }

    return {
      code: product.code,
      name: product.name,
      href: `/stock/${product.code}`,
      meta: metaParts.join(' · '),
      // Un jambon entamé reste un objet sur l'étagère : il compte dans le nombre d'unités, et le
      // badge précise seulement combien le sont. L'exclure affichait « 0 unité » face à du stock.
      qty: inStock,
      qtyLabel: pluralize(product.unitLabel, inStock),
      openedLabel: opened.length > 0 ? `dont ${opened.length} entamé${opened.length > 1 ? 's' : ''}` : null,
      isEmpty: inStock === 0,
    }
  })

  const totalUnitsInStock = products.reduce((sum, p) => sum + p.qty, 0)
  return { products, totalUnitsInStock }
}

/** Détail d'un produit (Détail Stock) : lots + unités encore en stock (available/opened). */
export async function getStockDetail(code: string): Promise<StockDetail | null> {
  const productDtos = await listProducts(true)
  const productDto = productDtos.find((p) => p.code.toUpperCase() === code.toUpperCase())
  if (!productDto) return null
  const product = toProduct(productDto)

  const [batchDtos, unitDtos] = await Promise.all([
    listProductionBatches(productDto.id),
    listStockUnits(),
  ])
  const unitsByBatch = new Map<number, StockUnitDto[]>()
  for (const unit of unitDtos) {
    const list = unitsByBatch.get(unit.batchId) ?? []
    list.push(unit)
    unitsByBatch.set(unit.batchId, list)
  }

  const rankLabels = dayRankLabels(batchDtos)

  let count = 0
  let totalGrams = 0
  const batches: StockDetailBatch[] = sortBatchesRecentFirst(batchDtos)
    .map((batch) => {
      // Le numéro d'une unité vient du serveur : il est écrit sur son étiquette et ne se
      // recompose pas ici, sous peine de changer au fil des ventes alors que le papier, lui,
      // ne change pas. Tri par id pour un ordre d'affichage stable.
      const batchUnits = [...(unitsByBatch.get(batch.id) ?? [])].sort((a, b) => a.id - b.id)
      return {
        id: batch.id,
        dayRankLabel: rankLabels.get(batch.id) ?? null,
        dateLabel: formatDateLabel(batch.productionDate),
        priceLabel: formatPriceLabel(batch.salePrice, product.priceUnit),
        salePrice: batch.salePrice,
        priceUnit: product.priceUnit,
        untouched: {
          rawMaterialRef: batch.rawMaterialRef,
          expiryDate: batch.expiryDate,
          notes: batch.notes,
        },
        createdByName: batch.createdByName,
        units: batchUnits
          .filter((unit) => isInStock(unit))
          .map((unit) => {
            count += 1
            const grams = unit.weight != null ? weightToGrams(unit.weight) : 0
            // Le total dit ce qui est encore vendable, pas ce qui a été fabriqué : un jambon
            // entamé y entre pour son restant. Le décompte d'unités, lui, ne bouge pas — c'est un
            // objet sur l'étagère, entamé ou non.
            totalGrams += unit.remainingWeight != null ? weightToGrams(unit.remainingWeight) : 0
            return {
              id: unit.id,
              number: unit.unitNumber,
              weightKg: unit.weight,
              remainingKg: unit.remainingWeight,
              weightLabel: unit.weight != null ? formatWeight(grams) : null,
              remainingLabel:
                unit.remainingWeight != null
                  ? formatWeight(weightToGrams(unit.remainingWeight))
                  : null,
              isEmptied: unit.status === 'opened' && unit.remainingWeight === 0,
              status: unit.status,
            }
          }),
      }
    })
    // Une fournée entièrement vendue ou perdue ne garde plus aucune unité en stock —
    // sa carte n'a plus de raison d'apparaître dans le détail.
    .filter((batch) => batch.units.length > 0)

  const summaryParts = [`${count} ${pluralize(product.unitLabel, count)} en stock`]
  // Un produit au poids annonce toujours son restant, même nul : « 1 unité · 0 g » dit qu'il ne
  // reste rien à vendre et qu'une clôture est due. Masquer le zéro laisserait croire à un bug.
  if (product.saleMode === 'by_weight' && count > 0) {
    summaryParts.push(`${formatWeight(totalGrams)} à vendre`)
  }

  return { name: product.name, isActive: productDto.isActive, summary: summaryParts.join(' · '), batches }
}

