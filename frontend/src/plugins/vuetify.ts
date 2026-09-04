import 'vuetify/styles'
import { createVuetify } from 'vuetify'
import { aliases, mdi } from 'vuetify/iconsets/mdi'
import '@mdi/font/css/materialdesignicons.css'
import { phosphor } from './phosphor-iconset'

// Système de design "Kraft" — voir design/style-guide.html et CLAUDE.md §12.
// Thème clair uniquement pour l'instant (le fichier de référence montre aussi
// une variante sombre, réservée à une itération future).
export default createVuetify({
  theme: {
    defaultTheme: 'light',
    themes: {
      light: {
        colors: {
          primary: '#C4623C', // Terracotta — primaire
          'primary-darken-1': '#A54E2E', // Terracotta — survol
          secondary: '#6E5A45', // Bois — secondaire
          background: '#ECE2D0', // Kraft — fond
          surface: '#FBF7EE', // Surface — carte
          'on-background': '#2B241E', // Texte principal
          'on-surface': '#2B241E',

          // Statuts sémantiques du design Kraft.
          success: '#4E7A4E',
          'success-container': '#DCE9D6',
          warning: '#8A6A16',
          'warning-container': '#F1E4C4',
          error: '#B0362A',
          'error-container': '#F3D9D3',

          // Mapping des statuts métier de stock_unit sur les couleurs
          // sémantiques du design (RG voir data-model.md) :
          //   available -> succès   | opened -> attention (warning)
          //   sold      -> neutre   | personal -> neutre
          //   lost      -> critique (error)
          // "neutre" n'a pas de couleur dédiée dans la maquette : on réutilise
          // le "bois" (secondary), plus sobre que primary/terracotta.
          'status-neutral': '#6E5A45',
          'status-neutral-container': '#EDE6DA',

          // Champs de saisie (AppTextField) : fond blanc distinct du fond
          // "carte" (surface), bordure au repos ton kraft (voir maquettes
          // Ajout Stock).
          'field-surface': '#FFFFFF',
          'field-border': '#DACFBB',
        },
      },
    },
  },
  icons: {
    defaultSet: 'mdi',
    aliases,
    sets: { mdi, phosphor },
  },
})