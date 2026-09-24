import { ref } from 'vue'
import type { SaleDraftDto } from '@/api/types'
import type { SaleLineDraft, SellableLot } from '@/composables/useSales'

/**
 * Brouillon de vente préparé par l'assistant vocal (RF-35, FR-016), en attente d'être ouvert dans le
 * formulaire « Nouvelle vente ». Il ne passe pas par l'URL : il est lu une seule fois, à l'ouverture.
 */
const pendingDraft = ref<SaleDraftDto | null>(null)

export function setAssistantDraft(draft: SaleDraftDto) {
  pendingDraft.value = draft
}

export function takeAssistantDraft(): SaleDraftDto | null {
  const draft = pendingDraft.value
  pendingDraft.value = null
  return draft
}

/**
 * Lignes de panier d'un brouillon, calculées comme dans `SaleLineChooser` : une unité entière vaut son
 * prix, une tranche vaut son poids × le prix au kilo du lot. Le serveur ne donne aucun montant.
 * Une unité disparue entre-temps, ou une tranche sans poids, reste à choisir à la main.
 * `lots` doit être chargé : un brouillon appliqué à une liste vide perdrait toutes ses lignes.
 */
export function draftToCart(
  draft: SaleDraftDto,
  lots: SellableLot[],
): { cart: SaleLineDraft[]; warnings: string[] } {
  const cart: SaleLineDraft[] = []
  const warnings = [...draft.warnings]
  for (const line of draft.lines) {
    const lot = lots.find((l) => l.stockUnitId === line.stockUnitId)
    if (!lot) {
      warnings.push("Une unité proposée n'est plus en stock : choisis-en une autre.")
      continue
    }
    if (line.isFullSale) {
      cart.push({
        stockUnitId: lot.stockUnitId,
        productName: lot.productName,
        label: lot.label,
        isFullSale: true,
        weightKg: lot.weight,
        amount: lot.price,
      })
      continue
    }
    // Le serveur a déjà signalé le poids à saisir.
    if (line.soldWeight == null || lot.pricePerKg == null) continue
    cart.push({
      stockUnitId: lot.stockUnitId,
      productName: lot.productName,
      label: lot.label,
      isFullSale: false,
      weightKg: line.soldWeight,
      amount: Math.round(line.soldWeight * lot.pricePerKg * 100) / 100,
    })
  }
  return { cart, warnings }
}
