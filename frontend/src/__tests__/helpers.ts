import { vi } from 'vitest'
import { DOMWrapper, flushPromises, mount } from '@vue/test-utils'
import type { Component } from 'vue'
import vuetify from '@/plugins/vuetify'

// jsdom ne connaît pas ResizeObserver, dont Vuetify se sert pour ses fenêtres et ses menus.
vi.stubGlobal(
  'ResizeObserver',
  class {
    observe() {}
    unobserve() {}
    disconnect() {}
  },
)

// Ni visualViewport, dont les fenêtres se servent pour se placer.
vi.stubGlobal('visualViewport', Object.assign(new EventTarget(), { width: 1024, height: 768, scale: 1, offsetLeft: 0, offsetTop: 0 }))

/** Monte un composant avec Vuetify, attaché au document pour que fenêtres et menus s'y ouvrent. */
export function mountWithVuetify<T extends Component>(component: T, options: Parameters<typeof mount<T>>[1] = {}) {
  return mount(component, {
    ...options,
    attachTo: document.body,
    global: { ...options.global, plugins: [vuetify, ...(options.global?.plugins ?? [])] },
  } as Parameters<typeof mount<T>>[1])
}

/** Fenêtres et menus Vuetify sont téléportés hors du composant : on les cherche dans tout le document. */
export function inDocument(selector: string): DOMWrapper<HTMLElement>[] {
  return Array.from(document.body.querySelectorAll<HTMLElement>(selector)).map((el) => new DOMWrapper(el))
}

/** Bouton du document dont le texte contient `text`. */
export function buttonByText(text: string): DOMWrapper<HTMLElement> {
  const found = inDocument('button, .v-list-item').find((b) => b.text().includes(text))
  if (!found) throw new Error(`Aucun bouton « ${text} » dans le document`)
  return found
}

export async function settle() {
  await flushPromises()
  await new Promise((resolve) => setTimeout(resolve, 0))
  await flushPromises()
}
