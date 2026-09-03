import { h } from 'vue'
import type { IconSet, IconProps } from 'vuetify'
import { PhPackage, PhCashRegister, PhUsers, PhCookingPot } from '@phosphor-icons/vue'

/**
 * Registry of Phosphor icons available through the `phosphor:` iconset.
 * Add entries here as more icons get adopted across the app — icons are
 * imported explicitly (not `import *`) so unused ones are tree-shaken out.
 */
const phosphorIcons: Record<string, unknown> = {
  package: PhPackage,
  'cash-register': PhCashRegister,
  users: PhUsers,
  'cooking-pot': PhCookingPot,
}

export const phosphor: IconSet = {
  component: (props: IconProps) => {
    const name = typeof props.icon === 'string' ? props.icon : ''
    const IconComponent = phosphorIcons[name]
    if (!IconComponent) {
      if (import.meta.env.DEV) console.warn(`[phosphor iconset] unknown icon "${name}"`)
      return h('span')
    }
    return h(IconComponent as never, { weight: 'regular' })
  },
}
