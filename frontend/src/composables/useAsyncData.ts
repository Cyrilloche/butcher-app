import { ref, type Ref } from 'vue'
import { ApiError } from '@/api/http'

/** Charge `loader()` immédiatement, expose data/loading/error + un moyen de recharger. */
export function useAsyncData<T>(loader: () => Promise<T>, initial: T) {
  const data = ref(initial) as Ref<T>
  const loading = ref(true)
  const error = ref<string | null>(null)

  async function load() {
    loading.value = true
    error.value = null
    try {
      data.value = await loader()
    } catch (err) {
      error.value = err instanceof ApiError ? err.message : 'Erreur de chargement.'
    } finally {
      loading.value = false
    }
  }

  load()

  return { data, loading, error, reload: load }
}
