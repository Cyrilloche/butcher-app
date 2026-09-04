import { listProducts } from '@/api/products'
import { listProductionBatches } from '@/api/productionBatches'
import { listStockUnits } from '@/api/stockUnits'
import { formatWeight } from '@/composables/useStock'
import type { ProductDto, ProductionBatchDto, StockUnitDto, StockUnitStatus } from '@/api/types'

export interface SellableLot {
  stockUnitId: number
  productName: string
  /** batch_number + rang dans le lot — même dérivation que Détail Stock (pas un champ persisté). */
  label: string
  detail: string
  /** `available` : encore intact. `opened` : déjà entamé (vente à la tranche en cours). */
  status: Extract<StockUnitStatus, 'available' | 'opened'>
  /** Prix pour une vente en entier (poids réel × prix/kg, ou prix pièce) — sens seulement si available. */
  price: number
  /** Kilogrammes, poids pesé à l'origine de l'unité — sens seulement si available et au poids. */
  weight: number | null
  /** Prix au kg du lot — pour calculer en direct le prix d'une tranche. null si à la pièce. */
  pricePerKg: number | null
  allowPartialSale: boolean
}

/** Prix d'une unité vendue en entier : poids réel × prix/kg du lot (RG-03), ou prix pièce du lot tel quel. */
function unitPrice(unit: StockUnitDto, batch: ProductionBatchDto): number {
  return unit.weight != null ? Math.round(unit.weight * batch.salePrice * 100) / 100 : batch.salePrice
}

function unitDetail(unit: StockUnitDto, batch: ProductionBatchDto): string {
  return unit.weight != null
    ? `${formatWeight(Math.round(unit.weight * 1000))} · ${batch.salePrice.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} € / kg`
    : 'À la pièce'
}

/**
 * Unités vendables de tous les produits actifs : `available` (vente en entier, ou en
 * tranche si le produit l'autorise) et `opened` (déjà entamées — uniquement vendables
 * à la tranche, cf. RG-05 : le poids restant n'est pas suivi, isFullSale n'a plus d'effet).
 */
export async function listSellableLots(): Promise<SellableLot[]> {
  const [products, batches, allUnits] = await Promise.all([
    listProducts(false),
    listProductionBatches(),
    listStockUnits(),
  ])

  const productById = new Map<number, ProductDto>(products.map((p) => [p.id, p]))
  const unitsByBatch = new Map<number, StockUnitDto[]>()
  for (const unit of allUnits) {
    const list = unitsByBatch.get(unit.batchId) ?? []
    list.push(unit)
    unitsByBatch.set(unit.batchId, list)
  }

  const lots: SellableLot[] = []
  for (const batch of batches) {
    const product = productById.get(batch.productId)
    if (!product) continue
    // Le rang tient sur l'ensemble du lot (pas seulement les unités dispo), pour rester
    // stable au fil des ventes — cf. docs/data-model.md §3.5.
    const sortedAll = [...(unitsByBatch.get(batch.id) ?? [])].sort((a, b) => a.id - b.id)
    sortedAll.forEach((unit, index) => {
      if (unit.status !== 'available' && unit.status !== 'opened') return
      lots.push({
        stockUnitId: unit.id,
        productName: product.name,
        label: `${batch.batchNumber}-${index + 1}`,
        detail: unitDetail(unit, batch),
        status: unit.status,
        price: unitPrice(unit, batch),
        weight: unit.weight,
        pricePerKg: unit.weight != null ? batch.salePrice : null,
        allowPartialSale: product.allowPartialSale,
      })
    })
  }
  return lots
}
