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
  /** Uniquement pertinent si saleMode = by_weight (400 sinon). */
  allowPartialSale: boolean
  isActive: boolean
  /**
   * Vrai dès qu'au moins un lot de production est rattaché : le code et le mode de vente sont alors
   * figés côté serveur. Décrit un fait, l'interface en déduit ce qu'elle passe en lecture seule.
   */
  isUsed: boolean
  /** Unités encore disponibles ou entamées : bloque la désactivation tant qu'il en reste. */
  remainingStockUnitCount: number
}

export interface WriteOffProductStockResult {
  /** Nombre d'unités effectivement sorties du stock par le solde. */
  writtenOffCount: number
}

export interface CreateProductRequest {
  code: string
  name: string
  saleMode: SaleMode
  allowPartialSale: boolean
}

export interface UpdateProductRequest {
  name: string
  allowPartialSale: boolean
  /**
   * Modifiable tant que le produit n'a aucun lot. Sur un produit déjà utilisé, renvoyer la valeur
   * actuelle : le serveur refuse toute valeur différente (409).
   */
  code: string
  /** Même règle que `code` : figé dès le premier lot. */
  saleMode: SaleMode
}

// --- ProductionBatch -------------------------------------------------------------

export interface ProductionBatchDto {
  id: number
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
  /**
   * Le numéro écrit à la main sur l'étiquette de cet objet, au format CODE-YYMMDD-N. C'est son
   * identité pour l'utilisateur : ne jamais le recomposer côté client, il vient du serveur.
   */
  unitNumber: string
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
  /** Lecture seule — résolu côté serveur via stock_unit → production_batch → product. */
  productName: string
  /** Faux si le produit a été désactivé depuis : le mouvement reste dans l'historique. */
  productIsActive: boolean
  /** Le numéro de l'unité sortie, tel qu'il est écrit sur son étiquette. */
  unitNumber: string
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
