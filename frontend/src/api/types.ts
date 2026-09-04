// Types miroir des DTO C# du backend (Butcher.Api.Application.Dtos).
// Casse : propriétés en camelCase (policy par défaut System.Text.Json),
// valeurs d'enum en snake_case (JsonStringEnumConverter + SnakeCaseLower,
// cf. Program.cs et CLAUDE.md C-11).

export type SaleMode = 'by_weight' | 'by_piece'
export type StockUnitStatus = 'available' | 'opened' | 'sold' | 'personal' | 'lost'
export type MovementType = 'sale' | 'personal' | 'loss'

// --- Auth --------------------------------------------------------------

export interface LoginRequest {
  email: string
  password: string
}

export interface AuthResponseDto {
  accessToken: string
  expiresAtUtc: string
}

// --- Product -------------------------------------------------------------

export interface ProductDto {
  id: number
  code: string
  name: string
  saleMode: SaleMode
  saleUnitId: number
  saleUnitLabel: string
  isActive: boolean
}

export interface CreateProductRequest {
  code: string
  name: string
  saleMode: SaleMode
  saleUnitId: number
}

export interface UpdateProductRequest {
  name: string
  saleUnitId: number
}

// --- ProductionBatch -------------------------------------------------------------

export interface ProductionBatchDto {
  id: number
  batchNumber: string
  productId: number
  productName: string
  productionDate: string
  salePrice: number
  rawMaterialRef: string | null
  expiryDate: string | null
  notes: string | null
}

export interface CreateProductionBatchRequest {
  productId: number
  productionDate: string
  salePrice: number
  rawMaterialRef?: string | null
  expiryDate?: string | null
  notes?: string | null
}

export interface UpdateProductionBatchRequest {
  salePrice: number
  rawMaterialRef?: string | null
  expiryDate?: string | null
  notes?: string | null
}

// --- StockUnit -------------------------------------------------------------

export interface StockUnitDto {
  id: number
  batchId: number
  batchNumber: string
  /** Kilogrammes, decimal(10,3) côté backend — pas des grammes. */
  weight: number | null
  status: StockUnitStatus
}

export interface AddStockUnitsRequest {
  /** Kilogrammes — un item par unité pesée. Requis si le produit est `by_weight`. */
  weights?: number[]
  /** Requis si le produit est `by_piece`. */
  quantity?: number
}

// --- StockMovement -------------------------------------------------------------

export interface CreateStockMovementRequest {
  type: MovementType
  isFullSale?: boolean
  soldWeight?: number
  amount?: number
  /** Requis si type = sale (vente existante à laquelle rattacher la ligne), interdit sinon. */
  saleId?: number
  notes?: string
}

export interface UpdateStockMovementRequest {
  soldWeight?: number
  amount?: number
  notes?: string
}

export interface StockMovementDto {
  id: number
  stockUnitId: number
  type: MovementType
  date: string
  soldWeight: number | null
  amount: number | null
  /** Lecture seule — résolu via la vente (sale.customer_id), plus de duplication sur le mouvement. */
  customerId: number | null
  customerName: string | null
  saleId: number | null
  saleNumber: string | null
  notes: string | null
}

// --- Sale -------------------------------------------------------------

export interface SaleDto {
  id: number
  saleNumber: string
  customerId: number
  customerName: string
  date: string
  paid: boolean
  notes: string | null
  total: number
  itemCount: number
  lines: StockMovementDto[]
}

export interface CreateSaleLineRequest {
  stockUnitId: number
  isFullSale: boolean
  soldWeight?: number
  amount: number
  notes?: string
}

export interface CreateSaleRequest {
  customerId: number
  date?: string
  paid: boolean
  notes?: string
  lines: CreateSaleLineRequest[]
}

export interface UpdateSaleRequest {
  customerId: number
  date: string
  paid: boolean
  notes?: string
}

export interface SetSalePaymentRequest {
  paid: boolean
}

// --- Customer -------------------------------------------------------------

export interface CustomerDto {
  id: number
  lastName: string
  firstName: string | null
  phone: string | null
  notes: string | null
}

export interface CreateCustomerRequest {
  lastName: string
  firstName?: string
  phone?: string
  notes?: string
}

export interface UpdateCustomerRequest {
  lastName: string
  firstName?: string
  phone?: string
  notes?: string
}

// --- UnitOfMeasure -------------------------------------------------------------

export interface UnitOfMeasureDto {
  id: number
  label: string
  abbreviation: string
  isActive: boolean
}

// --- Erreurs -------------------------------------------------------------

/** RFC7807 ProblemDetails renvoyé par ExceptionHandlingMiddleware pour les erreurs métier. */
export interface ProblemDetailsDto {
  status: number
  title: string
  detail?: string
}

/** Shape de validation ASP.NET standard (échecs d'annotations [Required]/[Range]...). */
export interface ValidationProblemDetailsDto {
  status: number
  title: string
  errors: Record<string, string[]>
}
