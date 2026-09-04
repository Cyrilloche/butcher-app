import { createRouter, createWebHistory } from 'vue-router'
import StockView from '@/views/StockView.vue'
import { useAuthStore } from '@/stores/auth'

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
})

export default router