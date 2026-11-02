import { baseApi } from '../../api/baseApi'

export interface DokumaCustomerAddress {
  id: number
  title: string
  addressLine: string
  district: string
  city: string
  postalCode: string | null
  countryCode: string
  isDefaultInvoice: boolean
  isDefaultDelivery: boolean
}

/** Yeni adreste `id` boştur; gönderilmeyen adresler silinir. */
export type DokumaCustomerAddressRequest = Omit<DokumaCustomerAddress, 'id'> & { id: number | null }

export interface DokumaCustomerContact {
  id: number
  name: string
  title: string | null
  phone: string | null
  email: string | null
}

/** Yeni yetkilide `id` boştur; gönderilmeyen yetkililer silinir. */
export type DokumaCustomerContactRequest = Omit<DokumaCustomerContact, 'id'> & { id: number | null }

export const dokumaCustomersApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getDokumaCustomerAddresses: build.query<{ addresses: DokumaCustomerAddress[] }, number>({
      query: (customerId) => `/dokuma-customers/${customerId}/addresses`,
      providesTags: ['CustomerAddress'],
    }),
    saveDokumaCustomerAddresses: build.mutation<unknown, { customerId: number; addresses: DokumaCustomerAddressRequest[] }>({
      query: ({ customerId, addresses }) => ({ url: `/dokuma-customers/${customerId}/addresses`, method: 'PUT', body: { addresses } }),
      // Listede adres sayısı ve fatura ili görünür.
      invalidatesTags: ['CustomerAddress', 'MasterData'],
    }),
    getDokumaCustomerContacts: build.query<{ contacts: DokumaCustomerContact[] }, number>({
      query: (customerId) => `/dokuma-customers/${customerId}/contacts`,
      providesTags: ['CustomerBuyer'],
    }),
    saveDokumaCustomerContacts: build.mutation<unknown, { customerId: number; contacts: DokumaCustomerContactRequest[] }>({
      query: ({ customerId, contacts }) => ({ url: `/dokuma-customers/${customerId}/contacts`, method: 'PUT', body: { contacts } }),
      invalidatesTags: ['CustomerBuyer', 'MasterData'],
    }),
  }),
})

export const {
  useGetDokumaCustomerAddressesQuery,
  useSaveDokumaCustomerAddressesMutation,
  useGetDokumaCustomerContactsQuery,
  useSaveDokumaCustomerContactsMutation,
} = dokumaCustomersApi
