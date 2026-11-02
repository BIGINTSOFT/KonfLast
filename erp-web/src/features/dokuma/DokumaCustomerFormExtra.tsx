import { X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { countries, turkishCities } from '../customers/customerPresentation'
import type { FormExtraProps } from '../masterData/resources'
import {
  useGetDokumaCustomerAddressesQuery,
  useGetDokumaCustomerContactsQuery,
  useSaveDokumaCustomerAddressesMutation,
  useSaveDokumaCustomerContactsMutation,
} from './dokumaCustomersApi'
import type { DokumaCustomerAddressRequest, DokumaCustomerContactRequest } from './dokumaCustomersApi'

type Tab = 'general' | 'edocument' | 'terms' | 'addresses' | 'contacts'

const tabs: { key: Tab; label: string }[] = [
  { key: 'general', label: 'Genel' },
  { key: 'edocument', label: 'e-Belge' },
  { key: 'terms', label: 'Ticari koşullar' },
  { key: 'addresses', label: 'Adresler' },
  { key: 'contacts', label: 'Yetkililer' },
]

/** Sunucu bir alanı reddederse o alanın bulunduğu sekme açılır. */
const eDocumentFields = ['isEInvoiceUser', 'eInvoiceAlias', 'eInvoiceProfile', 'isEDespatchUser', 'eDespatchAlias', 'eArchiveDelivery', 'eArchiveEmail']
const termFields = ['currency', 'vatRate', 'fireTolerancePercent', 'paymentTermDays', 'firstQualityMaxPoints', 'secondQualityMaxPoints', 'secondQualityDiscountPercent', 'documentLanguage']

type AddressDraft = DokumaCustomerAddressRequest & { key: number }
type ContactDraft = DokumaCustomerContactRequest & { key: number }

const emptyAddress = (key: number, first: boolean): AddressDraft => ({
  key,
  id: null,
  title: first ? 'Merkez' : '',
  addressLine: '',
  district: '',
  city: '',
  postalCode: '',
  countryCode: 'TR',
  isDefaultInvoice: first,
  isDefaultDelivery: first,
})

const emptyContact = (key: number): ContactDraft => ({ key, id: null, name: '', title: '', phone: '', email: '' })

/**
 * Dokuma müşteri formu. Adresler ve yetkililer müşteri kaydının alanı değildir; müşteri kaydedildikten sonra
 * (yeni kayıtta Id oluşunca) kendi uçlarına yazılır.
 */
export function DokumaCustomerFormExtra({
  row,
  values,
  fields,
  activeControl,
  disabled,
  errorFields,
  onAfterSaveChange,
}: FormExtraProps) {
  const [tab, setTab] = useState<Tab>('general')
  // Sunucu bir alanı reddedince o alanın sekmesi açılır; hata gizli sekmede kalmaz.
  const errorKey = errorFields.join(',')
  const [seenErrors, setSeenErrors] = useState(errorKey)
  if (errorKey !== seenErrors) {
    setSeenErrors(errorKey)
    if (errorFields.length > 0) {
      const first = errorFields[0]
      setTab(eDocumentFields.includes(first) ? 'edocument' : termFields.includes(first) ? 'terms' : 'general')
    }
  }

  const customerId = row?.id
  const storedAddresses = useGetDokumaCustomerAddressesQuery(customerId ?? 0, { skip: customerId === undefined })
  const storedContacts = useGetDokumaCustomerContactsQuery(customerId ?? 0, { skip: customerId === undefined })
  const [saveAddresses] = useSaveDokumaCustomerAddressesMutation()
  const [saveContacts] = useSaveDokumaCustomerContactsMutation()
  // Düzenleme yapılmadıysa listeler kayıtta yeniden gönderilmez.
  const [addressDraft, setAddressDraft] = useState<AddressDraft[] | null>(null)
  const [contactDraft, setContactDraft] = useState<ContactDraft[] | null>(null)
  const [nextKey, setNextKey] = useState(-1)
  const addresses = addressDraft ?? (storedAddresses.data?.addresses ?? []).map((address) => ({ ...address, key: address.id }))
  const contacts = contactDraft ?? (storedContacts.data?.contacts ?? []).map((contact) => ({ ...contact, key: contact.id }))

  const individual = values.partyType === 'INDIVIDUAL'
  const eInvoice = values.isEInvoiceUser === true
  const eDespatch = values.isEDespatchUser === true

  useEffect(() => {
    onAfterSaveChange(async (id) => {
      if (addressDraft !== null) {
        const result = await saveAddresses({
          customerId: id,
          addresses: addressDraft.map(({ key: _key, ...address }) => ({ ...address, postalCode: address.postalCode || null })),
        })
        if ('error' in result) {
          return result.error
        }
      }
      if (contactDraft !== null) {
        const result = await saveContacts({
          customerId: id,
          contacts: contactDraft.map(({ key: _key, ...contact }) => ({
            ...contact,
            title: contact.title || null,
            phone: contact.phone || null,
            email: contact.email || null,
          })),
        })
        if ('error' in result) {
          return result.error
        }
      }
      return undefined
    })
    return () => onAfterSaveChange(null)
  })

  const updateAddress = (key: number, change: Partial<AddressDraft>) =>
    setAddressDraft(addresses.map((address) => (address.key === key ? { ...address, ...change } : address)))

  // Varsayılan fatura / teslimat adresi tektir: biri seçilince diğerlerinden kalkar.
  const makeDefault = (key: number, flag: 'isDefaultInvoice' | 'isDefaultDelivery') =>
    setAddressDraft(addresses.map((address) => ({ ...address, [flag]: address.key === key })))

  const addAddress = () => {
    setAddressDraft([...addresses, emptyAddress(nextKey, addresses.length === 0)])
    setNextKey(nextKey - 1)
  }

  const updateContact = (key: number, change: Partial<ContactDraft>) =>
    setContactDraft(contacts.map((contact) => (contact.key === key ? { ...contact, ...change } : contact)))

  const addContact = () => {
    setContactDraft([...contacts, emptyContact(nextKey)])
    setNextKey(nextKey - 1)
  }

  return (
    <div className="customer-form">
      <div className="tabs" role="tablist" aria-label="Müşteri bilgileri">
        {tabs.map((item) => (
          <button
            key={item.key}
            type="button"
            role="tab"
            id={`customer-tab-${item.key}`}
            aria-selected={tab === item.key}
            aria-controls={`customer-panel-${item.key}`}
            className={`tabs__tab${tab === item.key ? ' tabs__tab--active' : ''}`}
            onClick={() => setTab(item.key)}
          >
            {item.label}
            {item.key === 'addresses' && addresses.length > 0 && <span className="tabs__count">{addresses.length}</span>}
            {item.key === 'contacts' && contacts.length > 0 && <span className="tabs__count">{contacts.length}</span>}
          </button>
        ))}
      </div>

      {/* Sekmeler gizlenir, kaldırılmaz: diğer sekmedeki girişler korunur. */}
      <div className="tabs__panel" role="tabpanel" id="customer-panel-general" aria-labelledby="customer-tab-general" hidden={tab !== 'general'}>
        {fields.customerCode}
        {fields.partyType}
        {fields.name}
        {individual && (
          <div className="form__pair">
            {fields.firstName}
            {fields.lastName}
          </div>
        )}
        <div className="form__pair">
          {fields.taxNumber}
          {fields.taxOffice}
        </div>
        <div className="form__pair">
          {fields.mersisNo}
          {fields.tradeRegistryNo}
        </div>
        <div className="form__pair">
          {fields.phone}
          {fields.email}
        </div>
        {activeControl}
      </div>

      <div className="tabs__panel" role="tabpanel" id="customer-panel-edocument" aria-labelledby="customer-tab-edocument" hidden={tab !== 'edocument'}>
        <fieldset className="form__section">
          <legend>e-Fatura</legend>
          {fields.isEInvoiceUser}
          {eInvoice && (
            <>
              {fields.eInvoiceAlias}
              {fields.eInvoiceProfile}
            </>
          )}
        </fieldset>
        <fieldset className="form__section">
          <legend>e-İrsaliye</legend>
          {fields.isEDespatchUser}
          {eDespatch && fields.eDespatchAlias}
        </fieldset>
        <fieldset className="form__section">
          <legend>e-Arşiv fatura</legend>
          {eInvoice ? (
            <p className="muted form__note">Müşteri e-Fatura mükellefi; faturaları e-Fatura olarak kesilir, e-Arşiv kullanılmaz.</p>
          ) : (
            <div className="form__pair">
              {fields.eArchiveDelivery}
              {fields.eArchiveEmail}
            </div>
          )}
        </fieldset>
      </div>

      <div className="tabs__panel" role="tabpanel" id="customer-panel-terms" aria-labelledby="customer-tab-terms" hidden={tab !== 'terms'}>
        <div className="form__pair">
          {fields.currency}
          {fields.vatRate}
        </div>
        <div className="form__pair">
          {fields.fireTolerancePercent}
          {fields.paymentTermDays}
        </div>
        <div className="form__pair">
          {fields.firstQualityMaxPoints}
          {fields.secondQualityMaxPoints}
        </div>
        <div className="form__pair">
          {fields.secondQualityDiscountPercent}
          {/* Çeki listesi, irsaliye, fatura, proforma ve top etiketi bu dilde basılır. */}
          {fields.documentLanguage}
        </div>
      </div>

      <div className="tabs__panel" role="tabpanel" id="customer-panel-addresses" aria-labelledby="customer-tab-addresses" hidden={tab !== 'addresses'}>
        {storedAddresses.isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={storedAddresses.error} />
        <datalist id="dokuma-customer-cities">
          {turkishCities.map((city) => <option key={city} value={city} />)}
        </datalist>

        {addresses.length === 0 && !storedAddresses.isLoading && <p className="muted">Kayıtlı adres yok.</p>}
        {addresses.map((address, index) => (
          <fieldset key={address.key} className="address-card" disabled={disabled}>
            <legend>
              {address.title.trim() || `${index + 1}. adres`}
              {address.isDefaultInvoice && <span className="badge badge--info">Fatura</span>}
              {address.isDefaultDelivery && <span className="badge badge--neutral">Teslimat</span>}
            </legend>
            <div className="address-card__grid">
              <label className="field">
                <span>Adres adı</span>
                <input value={address.title} maxLength={100} onChange={(event) => updateAddress(address.key, { title: event.target.value })} />
              </label>
              <label className="field">
                <span>Ülke</span>
                <select value={address.countryCode} onChange={(event) => updateAddress(address.key, { countryCode: event.target.value })}>
                  {countries.map((country) => <option key={country.code} value={country.code}>{country.name}</option>)}
                  {!countries.some((country) => country.code === address.countryCode) && (
                    <option value={address.countryCode}>{address.countryCode}</option>
                  )}
                </select>
              </label>
              <label className="field">
                <span>İl</span>
                <input
                  value={address.city}
                  maxLength={100}
                  list={address.countryCode === 'TR' ? 'dokuma-customer-cities' : undefined}
                  onChange={(event) => updateAddress(address.key, { city: event.target.value })}
                />
              </label>
              <label className="field">
                <span>İlçe</span>
                <input value={address.district} maxLength={100} onChange={(event) => updateAddress(address.key, { district: event.target.value })} />
              </label>
              <label className="field address-card__wide">
                <span>Açık adres</span>
                <textarea
                  rows={2}
                  value={address.addressLine}
                  maxLength={500}
                  placeholder="Mahalle, cadde / sokak, bina ve kapı no"
                  onChange={(event) => updateAddress(address.key, { addressLine: event.target.value })}
                />
              </label>
              <label className="field">
                <span>Posta kodu</span>
                <input
                  value={address.postalCode ?? ''}
                  maxLength={10}
                  inputMode={address.countryCode === 'TR' ? 'numeric' : undefined}
                  onChange={(event) => updateAddress(address.key, { postalCode: event.target.value })}
                />
              </label>
            </div>
            <div className="address-card__footer">
              <label className="checkbox">
                <input type="radio" name="default-invoice" checked={address.isDefaultInvoice} onChange={() => makeDefault(address.key, 'isDefaultInvoice')} />
                Varsayılan fatura adresi
              </label>
              <label className="checkbox">
                <input type="radio" name="default-delivery" checked={address.isDefaultDelivery} onChange={() => makeDefault(address.key, 'isDefaultDelivery')} />
                Varsayılan teslimat adresi
              </label>
              <button type="button" className="button button--danger-ghost" onClick={() => setAddressDraft(addresses.filter((item) => item.key !== address.key))}>
                Kaldır
              </button>
            </div>
          </fieldset>
        ))}
        <div>
          <button type="button" className="button" onClick={addAddress} disabled={disabled}>Adres ekle</button>
        </div>
      </div>

      <div className="tabs__panel" role="tabpanel" id="customer-panel-contacts" aria-labelledby="customer-tab-contacts" hidden={tab !== 'contacts'}>
        {storedContacts.isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={storedContacts.error} />
        <div className="table-wrap recipe-table">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Ad soyad</th>
                <th>Görev</th>
                <th>Telefon</th>
                <th>E-posta</th>
                <th aria-label="Satırı kaldır" />
              </tr>
            </thead>
            <tbody>
              {contacts.map((contact) => (
                <tr key={contact.key}>
                  <td>
                    <input aria-label="Ad soyad" value={contact.name} maxLength={100} disabled={disabled}
                      onChange={(event) => updateContact(contact.key, { name: event.target.value })} />
                  </td>
                  <td>
                    <input aria-label="Görev" value={contact.title ?? ''} maxLength={100} disabled={disabled}
                      onChange={(event) => updateContact(contact.key, { title: event.target.value })} />
                  </td>
                  <td>
                    <input aria-label="Telefon" value={contact.phone ?? ''} maxLength={30} disabled={disabled}
                      onChange={(event) => updateContact(contact.key, { phone: event.target.value })} />
                  </td>
                  <td>
                    <input aria-label="E-posta" type="email" value={contact.email ?? ''} maxLength={256} disabled={disabled}
                      onChange={(event) => updateContact(contact.key, { email: event.target.value })} />
                  </td>
                  <td className="recipe-table__remove-cell">
                    <button type="button" className="icon-button" aria-label="Satırı kaldır" title="Satırı kaldır" disabled={disabled}
                      onClick={() => setContactDraft(contacts.filter((item) => item.key !== contact.key))}>
                      <X aria-hidden="true" />
                    </button>
                  </td>
                </tr>
              ))}
              {contacts.length === 0 && !storedContacts.isLoading && (
                <tr>
                  <td colSpan={5} className="table__empty">Kayıtlı yetkili yok.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
        <div>
          <button type="button" className="button" onClick={addContact} disabled={disabled}>Yetkili ekle</button>
        </div>
      </div>
    </div>
  )
}
