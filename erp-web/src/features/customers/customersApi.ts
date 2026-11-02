import { baseApi } from '../../api/baseApi'

export interface CustomerAddress {
  id: number
  title: string
  addressLine: string
  district: string
  city: string
  postalCode: string | null
  countryCode: string
  contactName: string | null
  phone: string | null
  isDefaultInvoice: boolean
  isDefaultDelivery: boolean
}

export interface CustomerAddresses {
  customerId: number
  customerCode: string
  customerName: string
  addresses: CustomerAddress[]
}

/** Yeni adreste `id` boştur; gönderilmeyen adresler silinir. */
export type CustomerAddressRequest = Omit<CustomerAddress, 'id'> & { id: number | null }

export interface CustomerBuyer {
  id: number
  code: string
  name: string
  contactName: string | null
  email: string | null
  phone: string | null
  isActive: boolean
}

export interface CustomerBuyers {
  customerId: number
  customerCode: string
  customerName: string
  buyers: CustomerBuyer[]
}

/** Yeni buyer'da `id` boştur; gönderilmeyen buyer'lar silinir (siparişte kullanılmışsa sunucu reddeder). */
export type CustomerBuyerRequest = Omit<CustomerBuyer, 'id'> & { id: number | null }

export const customersApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getCustomerAddresses: build.query<CustomerAddresses, number>({
      query: (customerId) => `/customers/${customerId}/addresses`,
      providesTags: ['CustomerAddress'],
    }),
    saveCustomerAddresses: build.mutation<CustomerAddresses, { customerId: number; addresses: CustomerAddressRequest[] }>({
      query: ({ customerId, addresses }) => ({ url: `/customers/${customerId}/addresses`, method: 'PUT', body: { addresses } }),
      // Listede adres sayısı ve fatura ili görünür.
      invalidatesTags: ['CustomerAddress', 'MasterData'],
    }),
    getCustomerBuyers: build.query<CustomerBuyers, number>({
      query: (customerId) => `/customers/${customerId}/buyers`,
      providesTags: ['CustomerBuyer'],
    }),
    saveCustomerBuyers: build.mutation<CustomerBuyers, { customerId: number; buyers: CustomerBuyerRequest[] }>({
      query: ({ customerId, buyers }) => ({ url: `/customers/${customerId}/buyers`, method: 'PUT', body: { buyers } }),
      invalidatesTags: ['CustomerBuyer', 'MasterData'],
    }),
  }),
})

export const {
  useGetCustomerAddressesQuery,
  useSaveCustomerAddressesMutation,
  useGetCustomerBuyersQuery,
  useSaveCustomerBuyersMutation,
} = customersApi
