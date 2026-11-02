import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'

export interface PriceListListItem {
  id: number
  code: string
  name: string
  customerId: number | null
  customerName: string | null
  currency: string
  validFrom: string
  validTo: string | null
  isActive: boolean
  itemCount: number
  isCurrent: boolean
}

export interface PriceListItem {
  id: number
  productId: number
  productCode: string
  productName: string
  colorId: number | null
  colorName: string | null
  sizeId: number | null
  sizeName: string | null
  unitPrice: number
}

export interface PriceList {
  id: number
  code: string
  name: string
  customerId: number | null
  customerCode: string | null
  customerName: string | null
  currency: string
  validFrom: string
  validTo: string | null
  notes: string | null
  isActive: boolean
  createdAt: string
  createdBy: string | null
  updatedAt: string | null
  updatedBy: string | null
  items: PriceListItem[]
}

export interface PriceListRequest {
  code: string
  name: string
  customerId: number | null
  currency: string
  validFrom: string
  validTo: string | null
  notes: string | null
  isActive: boolean
  items: { productId: number; colorId: number | null; sizeId: number | null; unitPrice: number }[]
}

export interface CustomerDiscount {
  id: number
  customerId: number
  customerCode: string
  customerName: string
  productId: number | null
  productCode: string | null
  productName: string | null
  discountPercent: number
  validFrom: string
  validTo: string | null
  notes: string | null
  isCurrent: boolean
  updatedAt: string | null
  updatedBy: string | null
}

export interface CustomerDiscountRequest {
  customerId: number
  productId: number | null
  discountPercent: number
  validFrom: string
  validTo: string | null
  notes: string | null
}

export type PriceSource = 'CUSTOMER_PRICE_LIST' | 'GENERAL_PRICE_LIST' | 'PRODUCT_LIST_PRICE'

export interface PriceQuote {
  productId: number
  colorId: number
  sizeId: number
  listPrice: number | null
  discountPercent: number | null
  unitPrice: number | null
  source: PriceSource | null
  sourceName: string | null
  sourcePrice: number | null
  sourceCurrency: string | null
  exchangeRate: number | null
  problem: string | null
}

export interface PriceQuoteRequest {
  customerId: number
  currency: string
  date: string
  items: { productId: number; colorId: number; sizeId: number }[]
}

interface ListParams {
  page: number
  pageSize: number
  search?: string
  customerId?: number
  currentOnly?: boolean
}

export const pricingApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getPriceLists: build.query<PagedResult<PriceListListItem>, ListParams & GridParams>({
      query: (params) => ({ url: '/price-lists', params }),
      providesTags: ['PriceList'],
    }),
    getPriceList: build.query<PriceList, number>({
      query: (id) => `/price-lists/${id}`,
      providesTags: ['PriceList'],
    }),
    savePriceList: build.mutation<PriceList, { id?: number; body: PriceListRequest }>({
      query: ({ id, body }) => ({ url: id ? `/price-lists/${id}` : '/price-lists', method: id ? 'PUT' : 'POST', body }),
      invalidatesTags: ['PriceList'],
    }),
    deletePriceList: build.mutation<void, number>({
      query: (id) => ({ url: `/price-lists/${id}`, method: 'DELETE' }),
      invalidatesTags: ['PriceList'],
    }),
    getCustomerDiscounts: build.query<PagedResult<CustomerDiscount>, ListParams & GridParams>({
      query: (params) => ({ url: '/customer-discounts', params }),
      providesTags: ['CustomerDiscount'],
    }),
    saveCustomerDiscount: build.mutation<CustomerDiscount, { id?: number; body: CustomerDiscountRequest }>({
      query: ({ id, body }) => ({ url: id ? `/customer-discounts/${id}` : '/customer-discounts', method: id ? 'PUT' : 'POST', body }),
      invalidatesTags: ['CustomerDiscount'],
    }),
    deleteCustomerDiscount: build.mutation<void, number>({
      query: (id) => ({ url: `/customer-discounts/${id}`, method: 'DELETE' }),
      invalidatesTags: ['CustomerDiscount'],
    }),
    quotePrices: build.mutation<PriceQuote[], PriceQuoteRequest>({
      query: (body) => ({ url: '/pricing/quote', method: 'POST', body }),
    }),
  }),
})

export const {
  useGetPriceListsQuery,
  useGetPriceListQuery,
  useSavePriceListMutation,
  useDeletePriceListMutation,
  useGetCustomerDiscountsQuery,
  useSaveCustomerDiscountMutation,
  useDeleteCustomerDiscountMutation,
  useQuotePricesMutation,
} = pricingApi
