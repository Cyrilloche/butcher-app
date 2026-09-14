import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { ProductDto, ProductionBatchDto, StockUnitDto } from '@/api/types'
import type * as ProductsApi from '@/api/products'
import type * as BatchesApi from '@/api/productionBatches'
import type * as UnitsApi from '@/api/stockUnits'
import { listSellableLots } from '../useSales'

vi.mock('@/api/products', () => ({ listProducts: vi.fn<typeof ProductsApi.listProducts>() }))
vi.mock('@/api/productionBatches', () => ({ listProductionBatches: vi.fn<typeof BatchesApi.listProductionBatches>() }))
vi.mock('@/api/stockUnits', () => ({ listStockUnits: vi.fn<typeof UnitsApi.listStockUnits>() }))

const { listProducts } = vi.mocked(await import('@/api/products'))
const { listProductionBatches } = vi.mocked(await import('@/api/productionBatches'))
const { listStockUnits } = vi.mocked(await import('@/api/stockUnits'))

const products: ProductDto[] = [
  { id: 1, code: 'SC', name: 'Saucisson', saleMode: 'by_weight', allowPartialSale: false, isActive: true, isUsed: true, remainingStockUnitCount: 0 },
  { id: 2, code: 'JB', name: 'Jambon', saleMode: 'by_weight', allowPartialSale: true, isActive: true, isUsed: true, remainingStockUnitCount: 0 },
  { id: 3, code: 'TR', name: 'Terrine', saleMode: 'by_piece', allowPartialSale: false, isActive: true, isUsed: true, remainingStockUnitCount: 0 },
]

function batch(id: number, productId: number, salePrice: number): ProductionBatchDto {
  return { id, productId, productName: '', productionDate: '2026-09-10', salePrice, rawMaterialRef: null, expiryDate: null, notes: null, createdByName: null }
}

function unit(id: number, batchId: number, unitNumber: string, overrides: Partial<StockUnitDto> = {}): StockUnitDto {
  return { id, batchId, unitNumber, weight: 1, remainingWeight: 1, status: 'available', ...overrides }
}

beforeEach(() => {
  vi.resetAllMocks()
  listProducts.mockResolvedValue(products)
})

describe('listSellableLots', () => {
  it('propose les unités intactes et entamées des produits actifs, jamais celles déjà sorties', async () => {
    listProductionBatches.mockResolvedValue([batch(10, 1, 20), batch(99, 42, 10)])
    listStockUnits.mockResolvedValue([
      unit(1, 10, 'SC-260910-1'),
      unit(2, 10, 'SC-260910-2', { status: 'opened', remainingWeight: 0.4 }),
      unit(3, 10, 'SC-260910-3', { status: 'sold' }),
      unit(4, 10, 'SC-260910-4', { status: 'personal' }),
      unit(5, 10, 'SC-260910-5', { status: 'lost' }),
      unit(6, 99, 'XX-260910-1'),
    ])

    const lots = await listSellableLots()

    expect(listProducts).toHaveBeenCalledWith(false)
    expect(lots.map((l) => [l.label, l.status])).toEqual([
      ['SC-260910-1', 'available'],
      ['SC-260910-2', 'opened'],
    ])
    expect(lots[1]!.remainingWeight).toBe(0.4)
  })

  it('pré-calcule le prix d’une vente en entier sur le poids réel, au centime', async () => {
    listProductionBatches.mockResolvedValue([batch(10, 1, 18.5), batch(20, 3, 6)])
    listStockUnits.mockResolvedValue([
      unit(1, 10, 'SC-260910-1', { weight: 1.237, remainingWeight: 1.237 }),
      unit(2, 20, 'TR-260910-1', { weight: null, remainingWeight: null }),
    ])

    const [saucisson, terrine] = await listSellableLots()

    expect(saucisson).toMatchObject({
      productName: 'Saucisson',
      price: 22.88,
      weight: 1.237,
      pricePerKg: 18.5,
      detail: '1,24 kg · 18,50 € / kg',
      allowPartialSale: false,
    })
    expect(terrine).toMatchObject({ price: 6, weight: null, pricePerKg: null, detail: 'À la pièce' })
  })

  it('reprend l’autorisation de vente à la tranche du produit', async () => {
    listProductionBatches.mockResolvedValue([batch(10, 2, 25)])
    listStockUnits.mockResolvedValue([unit(1, 10, 'JB-260910-1')])

    const [jambon] = await listSellableLots()

    expect(jambon!.allowPartialSale).toBe(true)
  })

  it('trie par produit puis par numéro d’étiquette, -2 avant -10', async () => {
    listProductionBatches.mockResolvedValue([batch(10, 1, 20), batch(11, 1, 20), batch(20, 2, 25)])
    listStockUnits.mockResolvedValue([
      unit(1, 11, 'SC-260910-10'),
      unit(2, 10, 'SC-260910-2'),
      unit(3, 20, 'JB-260901-1'),
    ])

    const lots = await listSellableLots()

    expect(lots.map((l) => l.label)).toEqual(['JB-260901-1', 'SC-260910-2', 'SC-260910-10'])
  })
})
