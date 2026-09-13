import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useDisplay } from 'vuetify'

/** Paramètre d'URL qui ouvre la fenêtre d'ajout d'une liste, posé par AppFormShell sur écran large. */
export const ADD_DIALOG_QUERY = 'ajout'

/**
 * Fenêtre d'ajout d'une liste (specs/005-backoffice, FR-015a). Sur écran large, le bouton « + » ouvre le
 * formulaire dans une fenêtre, au-dessus de la liste, comme la création d'un compte ; sur téléphone, il
 * reste une page à part entière. Arriver sur la liste avec `?ajout=1` ouvre la fenêtre d'emblée.
 */
export function useAddDialog() {
  const { mdAndUp } = useDisplay()
  const route = useRoute()
  const router = useRouter()

  const open = ref(mdAndUp.value && route.query[ADD_DIALOG_QUERY] === '1')

  watch(open, (isOpen) => {
    if (isOpen || route.query[ADD_DIALOG_QUERY] === undefined) return
    const query = { ...route.query }
    delete query[ADD_DIALOG_QUERY]
    router.replace({ query })
  })

  return { mdAndUp, open }
}
