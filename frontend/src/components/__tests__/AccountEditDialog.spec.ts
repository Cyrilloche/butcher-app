import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { buttonByText, mountWithVuetify, settle } from '@/__tests__/helpers'
import type * as AccountsApi from '@/api/accounts'
import type { AccountDto } from '@/api/types'
import AccountEditDialog from '../domain/AccountEditDialog.vue'

vi.mock('@/api/accounts', () => ({
  createAccount: vi.fn<typeof AccountsApi.createAccount>(),
  updateAccount: vi.fn<typeof AccountsApi.updateAccount>(),
}))

const { createAccount, updateAccount } = vi.mocked(await import('@/api/accounts'))

const gerard: AccountDto = {
  id: 'g',
  email: 'gerard@saloir.local',
  displayName: 'Gérard',
  role: 'user',
  isActive: true,
  assistantEnabled: false,
  lastLoginAt: null,
  createdAt: '2026-09-13T08:00:00Z',
}

async function openDialog(account: AccountDto | null) {
  const wrapper = mountWithVuetify(AccountEditDialog, { props: { modelValue: false, account } })
  await wrapper.setProps({ modelValue: true })
  await settle()
  return wrapper
}

function assistantSwitch() {
  return document.querySelector<HTMLInputElement>('.account-edit__assistant input')
}

beforeEach(() => {
  vi.resetAllMocks()
  updateAccount.mockResolvedValue({} as never)
})

afterEach(() => {
  document.body.innerHTML = ''
})

describe('AccountEditDialog — assistant vocal (RF-36)', () => {
  it('active l’assistant pour le compte modifié', async () => {
    await openDialog(gerard)
    expect(assistantSwitch()!.checked).toBe(false)

    assistantSwitch()!.click()
    await settle()
    await buttonByText('Enregistrer').trigger('click')
    await settle()

    expect(updateAccount).toHaveBeenCalledWith('g', expect.objectContaining({ assistantEnabled: true }))
  })

  it('garde l’assistant d’un compte qui l’a déjà quand on le renomme', async () => {
    await openDialog({ ...gerard, assistantEnabled: true })

    await buttonByText('Enregistrer').trigger('click')
    await settle()

    expect(updateAccount).toHaveBeenCalledWith('g', expect.objectContaining({ assistantEnabled: true }))
  })

  it('ne propose pas l’assistant à la création : il s’active ensuite, compte par compte', async () => {
    await openDialog(null)

    expect(assistantSwitch()).toBeNull()
    expect(createAccount).not.toHaveBeenCalled()
  })
})
