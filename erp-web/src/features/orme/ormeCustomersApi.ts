import { baseApi } from '../../api/baseApi'

export interface OrmeCustomerAddress {
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
export type OrmeCustomerAddressRequest = Omit<OrmeCustomerAddress, 'id'> & { id: number | null }

export interface OrmeCustomerContact {
  id: number
  name: string
  title: string | null
  phone: string | null
  email: string | null
}

/** Yeni yetkilide `id` boştur; gönderilmeyen yetkililer silinir. */
export type OrmeCustomerContactRequest = Omit<OrmeCustomerContact, 'id'> & { id: number | null }

export const ormeCustomersApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getOrmeCustomerAddresses: build.query<{ addresses: OrmeCustomerAddress[] }, number>({
      query: (customerId) => `/orme-customers/${customerId}/addresses`,
      providesTags: ['CustomerAddress'],
    }),
    saveOrmeCustomerAddresses: build.mutation<unknown, { customerId: number; addresses: OrmeCustomerAddressRequest[] }>({
      query: ({ customerId, addresses }) => ({ url: `/orme-customers/${customerId}/addresses`, method: 'PUT', body: { addresses } }),
      // Listede adres sayısı ve fatura ili görünür.
      invalidatesTags: ['CustomerAddress', 'MasterData'],
    }),
    getOrmeCustomerContacts: build.query<{ contacts: OrmeCustomerContact[] }, number>({
      query: (customerId) => `/orme-customers/${customerId}/contacts`,
      providesTags: ['CustomerBuyer'],
    }),
    saveOrmeCustomerContacts: build.mutation<unknown, { customerId: number; contacts: OrmeCustomerContactRequest[] }>({
      query: ({ customerId, contacts }) => ({ url: `/orme-customers/${customerId}/contacts`, method: 'PUT', body: { contacts } }),
      invalidatesTags: ['CustomerBuyer', 'MasterData'],
    }),
  }),
})

export const {
  useGetOrmeCustomerAddressesQuery,
  useSaveOrmeCustomerAddressesMutation,
  useGetOrmeCustomerContactsQuery,
  useSaveOrmeCustomerContactsMutation,
} = ormeCustomersApi
