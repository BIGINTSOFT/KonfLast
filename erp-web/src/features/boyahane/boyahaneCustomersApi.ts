import { baseApi } from '../../api/baseApi'

export interface BoyahaneCustomerAddress {
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
export type BoyahaneCustomerAddressRequest = Omit<BoyahaneCustomerAddress, 'id'> & { id: number | null }

export interface BoyahaneCustomerContact {
  id: number
  name: string
  title: string | null
  phone: string | null
  email: string | null
}

/** Yeni yetkilide `id` boştur; gönderilmeyen yetkililer silinir. */
export type BoyahaneCustomerContactRequest = Omit<BoyahaneCustomerContact, 'id'> & { id: number | null }

export const boyahaneCustomersApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getBoyahaneCustomerAddresses: build.query<{ addresses: BoyahaneCustomerAddress[] }, number>({
      query: (customerId) => `/boyahane-customers/${customerId}/addresses`,
      providesTags: ['CustomerAddress'],
    }),
    saveBoyahaneCustomerAddresses: build.mutation<unknown, { customerId: number; addresses: BoyahaneCustomerAddressRequest[] }>({
      query: ({ customerId, addresses }) => ({ url: `/boyahane-customers/${customerId}/addresses`, method: 'PUT', body: { addresses } }),
      // Listede adres sayısı ve fatura ili görünür.
      invalidatesTags: ['CustomerAddress', 'MasterData'],
    }),
    getBoyahaneCustomerContacts: build.query<{ contacts: BoyahaneCustomerContact[] }, number>({
      query: (customerId) => `/boyahane-customers/${customerId}/contacts`,
      providesTags: ['CustomerBuyer'],
    }),
    saveBoyahaneCustomerContacts: build.mutation<unknown, { customerId: number; contacts: BoyahaneCustomerContactRequest[] }>({
      query: ({ customerId, contacts }) => ({ url: `/boyahane-customers/${customerId}/contacts`, method: 'PUT', body: { contacts } }),
      invalidatesTags: ['CustomerBuyer', 'MasterData'],
    }),
  }),
})

export const {
  useGetBoyahaneCustomerAddressesQuery,
  useSaveBoyahaneCustomerAddressesMutation,
  useGetBoyahaneCustomerContactsQuery,
  useSaveBoyahaneCustomerContactsMutation,
} = boyahaneCustomersApi
