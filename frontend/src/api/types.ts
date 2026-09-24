// Types miroir des DTO C# du backend (Butcher.Api.Application.Dtos).
// Casse : propriétés en camelCase (policy par défaut System.Text.Json),
// valeurs d'enum en snake_case (JsonStringEnumConverter + SnakeCaseLower,
// cf. Program.cs et CLAUDE.md C-11).

export type SaleMode = 'by_weight' | 'by_piece'
export type StockUnitStatus = 'available' | 'opened' | 'sold' | 'personal' | 'lost'
export type MovementType = 'sale' | 'personal' | 'loss'
/** Rôle d'un compte (ADR-011). Affichage : « Administrateur » / « Utilisateur ». */
export type AccountRole = 'admin' | 'user'

// --- Auth --------------------------------------------------------------

export interface LoginRequest {
  email: string
  password: string
}

export interface AuthResponseDto {
  accessToken: string
  expiresAtUtc: string
}

/** Compte connecté, relu en base par le serveur. Sert à adapter l'interface ; le serveur vérifie. */
export interface MeDto {
  id: string
  email: string
  displayName: string
  role: AccountRole
  /** L'assistant vocal est activé pour ce compte : le « + » propose « Dicter » (RF-36). */
  assistantEnabled: boolean
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

// --- Account -------------------------------------------------------------

export interface AccountDto {
  id: string
  /** Identifiant de connexion ; aucun message n'y est envoyé. */
  email: string
  displayName: string
  role: AccountRole
  isActive: boolean
  /** L'assistant vocal est activé pour ce compte (RF-36). */
  assistantEnabled: boolean
  lastLoginAt: string | null
  createdAt: string
}

export interface CreateAccountRequest {
  email: string
  displayName: string
  role: AccountRole
  password: string
}

export interface UpdateAccountRequest {
  displayName: string
  role: AccountRole
  /** Obligatoire pour promouvoir un utilisateur administrateur (32 caractères au moins). */
  newPassword?: string
  /** Active ou désactive l'assistant vocal ; absent, la valeur ne change pas. */
  assistantEnabled?: boolean
}

export interface ResetPasswordRequest {
  newPassword: string
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
  /** Nom du compte qui a enregistré la fabrication ; `null` avant les comptes nominatifs. */
  createdByName: string | null
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
  /** Poids pesé à la fabrication. Kilogrammes, decimal(10,3) côté backend — pas des grammes. */
  weight: number | null
  /**
   * Poids encore vendable : le poids pesé moins la somme des poids déjà vendus sur cette unité
   * (RG-05). Égal au poids pesé sur une unité intacte, `0` sur une unité entièrement vendue,
   * `null` si l'unité n'a pas de poids.
   *
   * Calculé par le serveur à chaque lecture, jamais stocké. Ne jamais le recalculer ici.
   */
  remainingWeight: number | null
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
  /** Nom du compte qui a enregistré la sortie ; `null` avant les comptes nominatifs. */
  createdByName: string | null
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
  /** Nom du compte qui a enregistré la vente ; `null` avant les comptes nominatifs. */
  createdByName: string | null
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
// --- Journal (US4) -------------------------------------------------------------

/** Nature d'une entrée du journal. Affichage : `useJournal.ts` (FR-032). */
export type AuditAction =
  | 'created'
  | 'updated'
  | 'deleted'
  | 'login_succeeded'
  | 'login_failed'
  | 'locked_out'
  | 'password_changed'

/** Type de l'objet concerné. Affichage : `useJournal.ts` (FR-032). */
export type AuditEntityType =
  | 'product'
  | 'production_batch'
  | 'stock_unit'
  | 'sale'
  | 'stock_movement'
  | 'customer'
  | 'account'

export interface AuditEntryDto {
  id: number
  occurredAt: string
  /** `null` : connexion sur une adresse inconnue, ou geste hors application (commande hors ligne). */
  accountId: string | null
  accountName: string | null
  action: AuditAction
  /** `null` pour une connexion refusée sur une adresse qui ne correspond à aucun compte. */
  entityType: AuditEntityType | null
  /** `null` pour un geste groupé (unités pesées ensemble, solde du stock). */
  entityId: string | null
  /** Libellé en français, figé au moment du geste. */
  entityLabel: string | null
  /** Contenu de l'objet supprimé ; seulement pour une suppression (FR-022). */
  deletedContent: Record<string, unknown> | null
}

export interface AuditEntryPageDto {
  items: AuditEntryDto[]
  total: number
  page: number
  pageSize: number
}

export interface AuditEntryQuery {
  accountId?: string
  entityType?: AuditEntityType
  action?: AuditAction
  /** Jour de début inclus, `YYYY-MM-DD`. */
  from?: string
  /** Jour de fin inclus, `YYYY-MM-DD`. */
  to?: string
  page?: number
  pageSize?: number
}

// --- Rapports (US5) -------------------------------------------------------------
// Montants : sommes des montants saisis sur les lignes de vente, jamais recalculés (FR-030).

export interface MonthlySalesDto {
  /** `YYYY-MM`, mois de Paris. */
  month: string
  saleCount: number
  total: number
  paidTotal: number
  pendingTotal: number
}

export interface SalesSummaryDto {
  saleCount: number
  total: number
  paidTotal: number
  pendingTotal: number
  /** Mois portant au moins une vente, du plus ancien au plus récent. */
  months: MonthlySalesDto[]
}

export interface CustomerSalesDto {
  customerId: number
  customerName: string
  saleCount: number
  total: number
  pendingTotal: number
}

export interface ProductSalesDto {
  productId: number
  productCode: string
  productName: string
  saleMode: SaleMode
  /** Unités distinctes vendues ou entamées : un jambon vendu en cinq tranches compte pour une. */
  unitCount: number
  /** Lignes de vente : le même jambon compte ici pour cinq. */
  lineCount: number
  /** Kilogrammes ; `null` pour un produit à la pièce. */
  soldWeight: number | null
  total: number
}

export interface UnpaidSaleDto {
  id: number
  saleNumber: string
  date: string
  total: number
}

export interface CustomerReceivableDto {
  customerId: number
  customerName: string
  pendingTotal: number
  oldestUnpaidDate: string
  /** De la plus ancienne à la plus récente. */
  sales: UnpaidSaleDto[]
}

export interface ReceivablesDto {
  total: number
  customers: CustomerReceivableDto[]
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

// --- Assistant vocal (RF-34, RF-35 ; specs/006-assistant-vocal) --------

export type AssistantReplyKind = 'stock_answer' | 'sale_draft' | 'not_understood'

export interface BatchStockDto {
  productionDate: string
  salePrice: number
  count: number
  /** Kilogrammes encore vendables ; null à la pièce. */
  remainingKg: number | null
}

export interface OpenedUnitStockDto {
  unitNumber: string
  productionDate: string
  remainingKg: number | null
}

export interface ProductStockDto {
  code: string
  name: string
  saleMode: SaleMode
  wholeCount: number
  openedCount: number
  remainingKg: number | null
  oldestDate: string | null
  batches: BatchStockDto[]
  opened: OpenedUnitStockDto[]
}

/** Ligne proposée par l'assistant : l'unité choisie par le serveur, sans montant (le formulaire le calcule). */
export interface DraftLineDto {
  stockUnitId: number
  isFullSale: boolean
  /** Kilogrammes d'une tranche ; null pour une unité entière ou un poids à saisir. */
  soldWeight: number | null
}

export interface SaleDraftDto {
  customerId: number | null
  paid: boolean
  lines: DraftLineDto[]
  warnings: string[]
}

export interface AssistantReplyDto {
  /** Identifiant de la demande journalisée : sert à demander la voix de la réponse (FR-020). */
  requestId: number
  kind: AssistantReplyKind
  /** Phrase à dire à voix haute. */
  speech: string
  /** Ce que l'assistant a entendu. */
  heard: string
  stock: ProductStockDto[] | null
  draft: SaleDraftDto | null
}

// --- Usage de l'assistant vocal (RF-36, FR-025), réservé à l'administrateur ----------------

export type VoiceRequestOutcome = 'stock_answer' | 'sale_draft' | 'not_understood' | 'error' | 'rate_limited'
export type VoiceInputMode = 'voice' | 'text'

export interface AssistantUsageDto {
  accountId: string
  accountName: string
  /** Lundi de la semaine, jour de Paris (`YYYY-MM-DD`). */
  weekStart: string
  requests: number
  stockAnswers: number
  saleDrafts: number
  notUnderstood: number
  errors: number
  rateLimited: number
  /** Durée médiane de traitement par le serveur ; null si aucune demande n'a été traitée. */
  medianDurationMs: number | null
}

export interface AssistantRequestDto {
  id: number
  occurredAt: string
  accountName: string
  inputMode: VoiceInputMode
  heardText: string | null
  outcome: VoiceRequestOutcome
  replySpeech: string | null
  durationMs: number
}
