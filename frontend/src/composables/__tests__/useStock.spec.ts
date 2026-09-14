import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { ProductDto, ProductionBatchDto, StockUnitDto } from '@/api/types'
import type * as ProductsApi from '@/api/products'
import type * as BatchesApi from '@/api/productionBatches'
import type * as UnitsApi from '@/api/stockUnits'
import { formatWeight, getStockDashboard, getStockDetail, pluralize } from '../useStock'

vi.mock('@/api/products', () => ({ listProducts: vi.fn<typeof ProductsApi.listProducts>() }))
vi.mock('@/api/productionBatches', () => ({ listProductionBatches: vi.fn<typeof BatchesApi.listProductionBatches>() }))
vi.mock('@/api/stockUnits', () => ({ listStockUnits: vi.fn<typeof UnitsApi.listStockUnits>() }))

const { listProducts } = vi.mocked(await import('@/api/products'))
const { listProductionBatches } = vi.mocked(await import('@/api/productionBatches'))
const { listStockUnits } = vi.mocked(await import('@/api/stockUnits'))

function product(overrides: Partial<ProductDto> & Pick<ProductDto, 'id' | 'code' | 'name'>): ProductDto {
  return {
    saleMode: 'by_weight',
    allowPartialSale: false,
    isActive: true,
    isUsed: true,
    remainingStockUnitCount: 0,
    ...overrides,
  }
}

function batch(overrides: Partial<ProductionBatchDto> & Pick<ProductionBatchDto, 'id' | 'productId'>): ProductionBatchDto {
  return {
    productName: '',
    productionDate: '2026-09-10',
    salePrice: 18.5,
    rawMaterialRef: null,
    expiryDate: null,
    notes: null,
    createdByName: null,
    ...overrides,
  }
}

function unit(overrides: Partial<StockUnitDto> & Pick<StockUnitDto, 'id' | 'batchId'>): StockUnitDto {
  const weight = overrides.weight === undefined ? 1 : overrides.weight
  return {
    unitNumber: `SC-260910-${overrides.id}`,
    weight,
    remainingWeight: weight,
    status: 'available',
    ...overrides,
  }
}

beforeEach(() => {
  vi.resetAllMocks()
})

describe('formatWeight et pluralize', () => {
  it('parle en grammes sous le kilo, en kilos au-delà, à la française', () => {
    expect(formatWeight(0)).toBe('0 g')
    expect(formatWeight(950)).toBe('950 g')
    expect(formatWeight(1000)).toBe('1 kg')
    expect(formatWeight(1234)).toBe('1,23 kg')
  })

  it('accorde le libellé au singulier jusqu’à un', () => {
    expect(pluralize('unités', 0)).toBe('unité')
    expect(pluralize('unités', 1)).toBe('unité')
    expect(pluralize('unités', 2)).toBe('unités')
  })
})

describe('getStockDashboard', () => {
  it('compte les unités sur l’étagère, entamées comprises, et totalise le poids encore vendable', async () => {
    listProducts.mockResolvedValue([
      product({ id: 1, code: 'JB', name: 'Jambon', allowPartialSale: true }),
      product({ id: 2, code: 'TR', name: 'Terrine', saleMode: 'by_piece' }),
      product({ id: 3, code: 'SC', name: 'Saucisson' }),
    ])
    listProductionBatches.mockResolvedValue([batch({ id: 10, productId: 1 }), batch({ id: 20, productId: 2 })])
    listStockUnits.mockResolvedValue([
      unit({ id: 1, batchId: 10, weight: 1.2 }),
      unit({ id: 2, batchId: 10, weight: 5, remainingWeight: 2.5, status: 'opened' }),
      unit({ id: 3, batchId: 10, weight: 4, remainingWeight: 0, status: 'sold' }),
      unit({ id: 4, batchId: 20, weight: null }),
    ])

    const { products, totalUnitsInStock } = await getStockDashboard()

    expect(listProducts).toHaveBeenCalledWith(false)
    expect(products[0]).toMatchObject({
      code: 'JB',
      href: '/stock/JB',
      qty: 2,
      qtyLabel: 'unités',
      openedLabel: 'dont 1 entamé',
      openedCount: 1,
      meta: 'Au poids · 3,7 kg à vendre',
      saleModeLabel: 'Au poids',
      remainingGrams: 3700,
      isEmpty: false,
    })
    expect(products[1]).toMatchObject({
      qty: 1,
      qtyLabel: 'pièce',
      openedLabel: null,
      meta: 'À la pièce',
      remainingGrams: null,
    })
    expect(products[2]).toMatchObject({ qty: 0, isEmpty: true, meta: 'Au poids', remainingGrams: 0 })
    expect(totalUnitsInStock).toBe(3)
  })

  it('accorde « entamés » au pluriel', async () => {
    listProducts.mockResolvedValue([product({ id: 1, code: 'JB', name: 'Jambon' })])
    listProductionBatches.mockResolvedValue([batch({ id: 10, productId: 1 })])
    listStockUnits.mockResolvedValue([
      unit({ id: 1, batchId: 10, status: 'opened', remainingWeight: 0.5 }),
      unit({ id: 2, batchId: 10, status: 'opened', remainingWeight: 0.25 }),
    ])

    const { products } = await getStockDashboard()

    expect(products[0]!.openedLabel).toBe('dont 2 entamés')
  })
})

describe('getStockDetail', () => {
  it('rend null pour un code inconnu', async () => {
    listProducts.mockResolvedValue([product({ id: 1, code: 'SC', name: 'Saucisson' })])

    await expect(getStockDetail('XX')).resolves.toBeNull()
    expect(listProductionBatches).not.toHaveBeenCalled()
  })

  it('trouve le produit sans tenir compte de la casse, désactivé compris', async () => {
    listProducts.mockResolvedValue([product({ id: 1, code: 'SC', name: 'Saucisson', isActive: false })])
    listProductionBatches.mockResolvedValue([])
    listStockUnits.mockResolvedValue([])

    const detail = await getStockDetail('sc')

    expect(listProducts).toHaveBeenCalledWith(true)
    expect(listProductionBatches).toHaveBeenCalledWith(1)
    expect(detail).toMatchObject({ name: 'Saucisson', isActive: false, summary: '0 unité en stock', batches: [] })
  })

  it('présente les fournées de la plus récente à la plus ancienne, avec le rang des fournées d’un même jour', async () => {
    listProducts.mockResolvedValue([product({ id: 1, code: 'SC', name: 'Saucisson' })])
    listProductionBatches.mockResolvedValue([
      batch({ id: 5, productId: 1, productionDate: '2026-09-01', salePrice: 20 }),
      batch({ id: 8, productId: 1, productionDate: '2026-09-10' }),
      batch({ id: 7, productId: 1, productionDate: '2026-09-10' }),
    ])
    listStockUnits.mockResolvedValue([
      unit({ id: 1, batchId: 5 }),
      unit({ id: 2, batchId: 7 }),
      unit({ id: 3, batchId: 8 }),
    ])

    const detail = await getStockDetail('SC')

    expect(detail!.batches.map((b) => [b.id, b.dayRankLabel])).toEqual([
      [8, '2ᵉ fournée'],
      [7, '1ʳᵉ fournée'],
      [5, null],
    ])
    expect(detail!.batches[2]).toMatchObject({ dateLabel: '1 septembre 2026', priceLabel: '20,00 € / kg', priceUnit: 'kg', salePrice: 20 })
  })

  it('ne garde que les unités en stock, retire les fournées écoulées et totalise le restant', async () => {
    listProducts.mockResolvedValue([product({ id: 1, code: 'JB', name: 'Jambon', allowPartialSale: true })])
    listProductionBatches.mockResolvedValue([
      batch({ id: 10, productId: 1, rawMaterialRef: 'porc — grossiste X', expiryDate: '2026-12-01', notes: 'fumé', createdByName: 'Mamie' }),
      batch({ id: 11, productId: 1, productionDate: '2026-08-01' }),
    ])
    listStockUnits.mockResolvedValue([
      unit({ id: 2, batchId: 10, weight: 5.4, remainingWeight: 2.15, status: 'opened', unitNumber: 'JB-260910-2' }),
      unit({ id: 1, batchId: 10, weight: 0.75, unitNumber: 'JB-260910-1' }),
      unit({ id: 3, batchId: 10, status: 'lost' }),
      unit({ id: 4, batchId: 11, status: 'sold', remainingWeight: 0 }),
    ])

    const detail = await getStockDetail('JB')

    expect(detail!.summary).toBe('2 unités en stock · 2,9 kg à vendre')
    expect(detail!.batches).toHaveLength(1)
    const [only] = detail!.batches
    expect(only!.untouched).toEqual({ rawMaterialRef: 'porc — grossiste X', expiryDate: '2026-12-01', notes: 'fumé' })
    expect(only!.createdByName).toBe('Mamie')
    expect(only!.units).toEqual([
      {
        id: 1,
        number: 'JB-260910-1',
        weightKg: 0.75,
        remainingKg: 0.75,
        weightLabel: '750 g',
        remainingLabel: '750 g',
        isEmptied: false,
        status: 'available',
      },
      {
        id: 2,
        number: 'JB-260910-2',
        weightKg: 5.4,
        remainingKg: 2.15,
        weightLabel: '5,4 kg',
        remainingLabel: '2,15 kg',
        isEmptied: false,
        status: 'opened',
      },
    ])
  })

  it('annonce un restant nul plutôt que de le taire : une clôture est due', async () => {
    listProducts.mockResolvedValue([product({ id: 1, code: 'JB', name: 'Jambon' })])
    listProductionBatches.mockResolvedValue([batch({ id: 10, productId: 1 })])
    listStockUnits.mockResolvedValue([unit({ id: 1, batchId: 10, weight: 5, remainingWeight: 0, status: 'opened' })])

    const detail = await getStockDetail('JB')

    expect(detail!.summary).toBe('1 unité en stock · 0 g à vendre')
    expect(detail!.batches[0]!.units[0]!.isEmptied).toBe(true)
  })

  it('ne parle pas de poids pour un produit à la pièce', async () => {
    listProducts.mockResolvedValue([product({ id: 1, code: 'TR', name: 'Terrine', saleMode: 'by_piece' })])
    listProductionBatches.mockResolvedValue([batch({ id: 10, productId: 1, salePrice: 6 })])
    listStockUnits.mockResolvedValue([unit({ id: 1, batchId: 10, weight: null }), unit({ id: 2, batchId: 10, weight: null })])

    const detail = await getStockDetail('TR')

    expect(detail!.summary).toBe('2 pièces en stock')
    expect(detail!.batches[0]!.priceLabel).toBe('6,00 € / pièce')
    expect(detail!.batches[0]!.units[0]).toMatchObject({ weightLabel: null, remainingLabel: null, isEmptied: false })
  })
})
