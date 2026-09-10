import { listProducts } from '@/api/products'
import { listProductionBatches } from '@/api/productionBatches'
import { listStockUnits } from '@/api/stockUnits'
import { listStockMovements } from '@/api/stockMovements'
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
  /** Poids pesé en kg (null pour un produit à la pièce) — sert aux mouvements de sortie. */
  weightKg: number | null
  weightLabel: string | null
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
  totalAvailableUnits: number
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
    const totalGrams = units
      .filter(isInStock)
      .reduce((sum, u) => sum + (u.weight != null ? weightToGrams(u.weight) : 0), 0)

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
        units: batchUnits
          .filter((unit) => isInStock(unit))
          .map((unit) => {
            count += 1
            const grams = unit.weight != null ? weightToGrams(unit.weight) : 0
            totalGrams += grams
            return {
              id: unit.id,
              number: unit.unitNumber,
              weightKg: unit.weight,
              weightLabel: unit.weight != null ? formatWeight(grams) : null,
              status: unit.status,
            }
          }),
      }
    })
    // Une fournée entièrement vendue ou perdue ne garde plus aucune unité en stock —
    // sa carte n'a plus de raison d'apparaître dans le détail.
    .filter((batch) => batch.units.length > 0)

  const summaryParts = [`${count} ${pluralize(product.unitLabel, count)} en stock`]
  if (product.saleMode === 'by_weight' && totalGrams > 0) summaryParts.push(formatWeight(totalGrams))

  return { name: product.name, isActive: productDto.isActive, summary: summaryParts.join(' · '), batches }
}

/**
 * Poids restant estimé (kg) d'une unité : poids pesé − somme des poids déjà vendus
 * (RG-05 : le restant n'est pas une donnée stockée, il se recalcule à la demande).
 * `null` pour un produit à la pièce, où la notion de poids n'existe pas.
 *
 * Purement indicatif : sert à annoncer un poids avant confirmation, dans le menu de sortie, et à
 * pré-remplir une vente à la tranche. Le poids réellement enregistré sur une sortie perso ou perte
 * est calculé par le serveur, qui est le seul auteur de cette règle.
 */
export async function getRemainingWeightKg(
  stockUnitId: number,
  unitWeightKg: number | null,
): Promise<number | null> {
  if (unitWeightKg == null) return null
  const movements = await listStockMovements({ stockUnitId })
  const alreadySold = movements
    .filter((m) => m.type === 'sale')
    .reduce((sum, m) => sum + (m.soldWeight ?? 0), 0)
  // Arrondi au gramme : le poids est un decimal(10,3) côté base, et la soustraction
  // de flottants produirait sinon des valeurs du type 0.30000000000000004.
  return Math.max(0, Math.round((unitWeightKg - alreadySold) * 1000) / 1000)
}
