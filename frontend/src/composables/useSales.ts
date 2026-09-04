import { listProducts } from '@/api/products'
import { listProductionBatches } from '@/api/productionBatches'
import { listStockUnits } from '@/api/stockUnits'
import { formatWeight } from '@/composables/useStock'
import type { ProductDto, ProductionBatchDto, StockUnitDto } from '@/api/types'

export interface AvailableLot {
  stockUnitId: number
  productName: string
  /** batch_number + rang dans le lot — même dérivation que Détail Stock (pas un champ persisté). */
  label: string
  detail: string
  price: number
}

/** Prix d'une unité : poids réel × prix/kg du lot (RG-03), ou prix pièce du lot tel quel. */
function unitPrice(unit: StockUnitDto, batch: ProductionBatchDto): number {
  return unit.weight != null ? Math.round(unit.weight * batch.salePrice * 100) / 100 : batch.salePrice
}

function unitDetail(unit: StockUnitDto, batch: ProductionBatchDto): string {
  return unit.weight != null
    ? `${formatWeight(Math.round(unit.weight * 1000))} · ${batch.salePrice.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} € / kg`
    : 'À la pièce'
}

/** Unités `available` de tous les produits actifs, prêtes à être vendues. */
export async function listAvailableLots(): Promise<AvailableLot[]> {
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

  const lots: AvailableLot[] = []
  for (const batch of batches) {
    const product = productById.get(batch.productId)
    if (!product) continue
    // Le rang tient sur l'ensemble du lot (pas seulement les unités dispo), pour rester
    // stable au fil des ventes — cf. docs/data-model.md §3.5.
    const sortedAll = [...(unitsByBatch.get(batch.id) ?? [])].sort((a, b) => a.id - b.id)
    sortedAll.forEach((unit, index) => {
      if (unit.status !== 'available') return
      lots.push({
        stockUnitId: unit.id,
        productName: product.name,
        label: `${batch.batchNumber}-${index + 1}`,
        detail: unitDetail(unit, batch),
        price: unitPrice(unit, batch),
      })
    })
  }
  return lots
}
