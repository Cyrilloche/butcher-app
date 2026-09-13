import { createRouter, createWebHistory } from 'vue-router'
import StockView from '@/views/StockView.vue'
import { useAuthStore } from '@/stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    /**
     * Écran réservé à l'administrateur (ADR-011). La garde évite d'afficher un écran vide ; le
     * serveur, lui, refuse les appels (403) quoi qu'il arrive.
     */
    requiresAdmin?: boolean
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'stock', component: StockView },
    { path: '/stock/add', name: 'stock-add', component: () => import('@/views/StockAddView.vue') },
    {
      path: '/stock/:code',
      name: 'stock-detail',
      component: () => import('@/views/StockDetailView.vue'),
      props: true,
    },
    { path: '/sales', name: 'sales', component: () => import('@/views/SalesView.vue') },
    { path: '/sales/add', name: 'sales-add', component: () => import('@/views/SaleAddView.vue') },
    {
      path: '/sales/:id',
      name: 'sales-detail',
      component: () => import('@/views/SaleDetailView.vue'),
      props: true,
    },
    { path: '/customers', name: 'customers', component: () => import('@/views/CustomersView.vue') },
    { path: '/customers/add', name: 'customers-add', component: () => import('@/views/CustomerAddView.vue') },
    {
      path: '/customers/:id',
      name: 'customers-detail',
      component: () => import('@/views/CustomerDetailView.vue'),
      props: true,
    },
    { path: '/products', name: 'products', component: () => import('@/views/ProductsView.vue') },
    { path: '/products/add', name: 'products-add', component: () => import('@/views/ProductAddView.vue') },
    {
      path: '/products/:code',
      name: 'products-detail',
      component: () => import('@/views/ProductDetailView.vue'),
      props: true,
    },
    {
      path: '/overview',
      name: 'overview',
      component: () => import('@/views/OverviewView.vue'),
      // Ses chiffres relèvent des rapports, réservés à l'administrateur (FR-027).
      meta: { requiresAdmin: true },
    },
    { path: '/my-account', name: 'my-account', component: () => import('@/views/MyAccountView.vue') },
    {
      path: '/accounts',
      name: 'accounts',
      component: () => import('@/views/AccountsView.vue'),
      meta: { requiresAdmin: true },
    },
    {
      path: '/journal',
      name: 'journal',
      component: () => import('@/views/JournalView.vue'),
      meta: { requiresAdmin: true },
    },
    {
      path: '/reports',
      name: 'reports',
      component: () => import('@/views/ReportsView.vue'),
      meta: { requiresAdmin: true },
    },
    { path: '/login', name: 'login', component: () => import('@/views/LoginView.vue') },
  ],
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  await auth.ensureReady()

  if (to.name !== 'login' && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }
  if (to.name === 'login' && auth.isAuthenticated) {
    return { name: 'stock' }
  }
  if (to.meta.requiresAdmin && !auth.isAdmin) {
    return { name: 'stock' }
  }
})

export default router