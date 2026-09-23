import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mountWithVuetify } from '@/__tests__/helpers'
import ActionFab from '../domain/ActionFab.vue'

const push = vi.fn<(to: string) => void>()
vi.mock('vue-router', () => ({ useRouter: () => ({ push }) }))
const start = vi.fn<() => Promise<void>>()
vi.mock('@/composables/useAssistant', () => ({ useAssistant: () => ({ start }) }))
const auth = { assistantEnabled: true }
vi.mock('@/stores/auth', () => ({ useAuthStore: () => auth }))

beforeEach(() => {
  vi.resetAllMocks()
  auth.assistantEnabled = true
})
afterEach(() => {
  document.body.innerHTML = ''
})

function choice(wrapper: ReturnType<typeof mountWithVuetify>, text: string) {
  return wrapper.findAll('.action-fab__choice').find((b) => b.text().includes(text))!
}

describe('ActionFab', () => {
  it('sans assistant, déclenche l’action de l’écran en un appui, comme avant', async () => {
    auth.assistantEnabled = false
    const wrapper = mountWithVuetify(ActionFab, { props: { label: 'Nouvelle vente', to: '/sales/add' } })

    await wrapper.find('.action-fab__main').trigger('click')

    expect(push).toHaveBeenCalledWith('/sales/add')
    expect(wrapper.find('.action-fab__choices').exists()).toBe(false)
    expect(wrapper.find('.action-fab__main').attributes('aria-label')).toBe('Nouvelle vente')
  })

  it('sans assistant, ouvre la fenêtre sur écran large en un appui', async () => {
    auth.assistantEnabled = false
    const wrapper = mountWithVuetify(ActionFab, { props: { label: 'Nouvelle vente' } })

    await wrapper.find('.action-fab__main').trigger('click')

    expect(wrapper.emitted('click')).toHaveLength(1)
    expect(start).not.toHaveBeenCalled()
  })

  it('déplie l’action de l’écran et « Dicter » au premier appui', async () => {
    const wrapper = mountWithVuetify(ActionFab, { props: { label: 'Nouvelle vente', to: '/sales/add' } })
    expect(wrapper.find('.action-fab__choices').exists()).toBe(false)

    await wrapper.find('.action-fab__main').trigger('click')

    expect(wrapper.findAll('.action-fab__choice').map((b) => b.text())).toEqual(['Dicter', 'Nouvelle vente'])
  })

  it('mène à l’action de l’écran', async () => {
    const wrapper = mountWithVuetify(ActionFab, { props: { label: 'Nouvelle vente', to: '/sales/add' } })
    await wrapper.find('.action-fab__main').trigger('click')

    await choice(wrapper, 'Nouvelle vente').trigger('click')

    expect(push).toHaveBeenCalledWith('/sales/add')
    expect(wrapper.find('.action-fab__choices').exists()).toBe(false)
  })

  it('ouvre la fenêtre sur écran large, quand aucune route n’est donnée', async () => {
    const wrapper = mountWithVuetify(ActionFab, { props: { label: 'Nouvelle vente' } })
    await wrapper.find('.action-fab__main').trigger('click')

    await choice(wrapper, 'Nouvelle vente').trigger('click')

    expect(wrapper.emitted('click')).toHaveLength(1)
    expect(push).not.toHaveBeenCalled()
  })

  it('lance l’écoute avec « Dicter »', async () => {
    const wrapper = mountWithVuetify(ActionFab, { props: { label: 'Nouvelle vente', to: '/sales/add' } })
    await wrapper.find('.action-fab__main').trigger('click')

    await choice(wrapper, 'Dicter').trigger('click')

    expect(start).toHaveBeenCalledOnce()
  })
})
