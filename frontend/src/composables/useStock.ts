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

  let count = 0
  let totalGrams = 0
  const batches: StockDetailBatch[] = sortBatchesRecentFirst(batchDtos).map((batch) => {
    // Numéro par unité = batch_number + rang dans le lot (pas une colonne persistée,
    // cf. docs/data-model.md §3.5) — trié par id pour un ordre stable.
    // Le rang (donc le numéro affiché) se fixe sur l'ordre complet du lot, y compris
    // les unités déjà sorties — sinon le numéro d'une unité changerait au fil des ventes.
    const batchUnits = [...(unitsByBatch.get(batch.id) ?? [])].sort((a, b) => a.id - b.id)
    return {
      batchNumber: batch.batchNumber,
      dateLabel: formatDateLabel(batch.productionDate),
      priceLabel: formatPriceLabel(batch.salePrice, product.priceUnit),
      units: batchUnits
        .map((unit, index) => ({ unit, number: `${batch.batchNumber}-${index + 1}` }))
        .filter(({ unit }) => isInStock(unit))
        .map(({ unit, number }) => {
          count += 1
          const grams = unit.weight != null ? weightToGrams(unit.weight) : 0
          totalGrams += grams
          return {
            id: unit.id,
            number,
            weightLabel: unit.weight != null ? formatWeight(grams) : null,
            status: unit.status,
          }
        }),
    }
  })

  const summaryParts = [`${count} ${pluralize(product.unitLabel, count)} en stock`]
  if (product.saleMode === 'by_weight' && totalGrams > 0) summaryParts.push(formatWeight(totalGrams))

  return { name: product.name, summary: summaryParts.join(' · '), batches }
}
