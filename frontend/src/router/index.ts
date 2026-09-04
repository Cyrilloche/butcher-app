import { createRouter, createWebHistory } from 'vue-router'
import StockView from '@/views/StockView.vue'

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
    { path: '/products', name: 'products', component: () => import('@/views/ProductsView.vue') },
    { path: '/login', name: 'login', component: () => import('@/views/LoginView.vue') },
  ],
})

export default router