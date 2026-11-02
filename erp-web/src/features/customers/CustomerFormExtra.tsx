import { useEffect, useState } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import type { FormExtraProps } from '../masterData/resources'
import { countries, turkishCities } from './customerPresentation'
import { X } from 'lucide-react'
import {
  useGetCustomerAddressesQuery,
  useGetCustomerBuyersQuery,
  useSaveCustomerAddressesMutation,
  useSaveCustomerBuyersMutation,
} from './customersApi'
import type { CustomerAddress, CustomerAddressRequest, CustomerBuyer, CustomerBuyerRequest } from './customersApi'

type Tab = 'general' | 'edocument' | 'addresses' | 'buyers'

const tabs: { key: Tab; label: string }[] = [
  { key: 'general', label: 'Genel' },
  { key: 'edocument', label: 'e-Belge' },
  { key: 'addresses', label: 'Adresler' },
  { key: 'buyers', label: 'Buyer' },
]

/** e-Belge sekmesindeki alanlar; sunucu bunlardan birini reddederse o sekme açılır. */
const eDocumentFields = ['isEInvoiceUser', 'eInvoiceAlias', 'eInvoiceProfile', 'isEDespatchUser', 'eDespatchAlias', 'eArchiveDelivery', 'eArchiveEmail']

type AddressDraft = CustomerAddressRequest & { key: number }

const emptyAddress = (key: number, first: boolean): AddressDraft => ({
  key,
  id: null,
  title: first ? 'Merkez' : '',
  addressLine: '',
  district: '',
  city: '',
  postalCode: '',
  countryCode: 'TR',
  contactName: '',
  phone: '',
  isDefaultInvoice: first,
  isDefaultDelivery: first,
})

const toDraft = (address: CustomerAddress): AddressDraft => ({ ...address, key: address.id })

type BuyerDraft = CustomerBuyerRequest & { key: number }

const emptyBuyer = (key: number): BuyerDraft => ({
  key, id: null, code: '', name: '', contactName: '', email: '', phone: '', isActive: true,
})

const toBuyerDraft = (buyer: CustomerBuyer): BuyerDraft => ({ ...buyer, key: buyer.id })

/**
 * Müşteri formu: genel bilgiler, e-Fatura / e-İrsaliye / e-Arşiv bilgileri ve adresler. Adresler müşteri kaydının
 * alanı değildir; müşteri kaydedildikten sonra (yeni kayıtta Id oluşunca) kendi ucuna yazılır.
 */
export function CustomerFormExtra({
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
      setTab(errorFields.some((name) => !eDocumentFields.includes(name)) ? 'general' : 'edocument')
    }
  }

  const customerId = row?.id
  const stored = useGetCustomerAddressesQuery(customerId ?? 0, { skip: customerId === undefined })
  const [save] = useSaveCustomerAddressesMutation()
  // Düzenleme yapılmadıysa adres listesi kayıtta yeniden gönderilmez.
  const [draft, setDraft] = useState<AddressDraft[] | null>(null)
  const [nextKey, setNextKey] = useState(-1)
  const addresses = draft ?? (stored.data?.addresses ?? []).map(toDraft)

  const storedBuyers = useGetCustomerBuyersQuery(customerId ?? 0, { skip: customerId === undefined })
  const [saveBuyers] = useSaveCustomerBuyersMutation()
  const [buyerDraft, setBuyerDraft] = useState<BuyerDraft[] | null>(null)
  const buyers = buyerDraft ?? (storedBuyers.data?.buyers ?? []).map(toBuyerDraft)

  const individual = values.partyType === 'INDIVIDUAL'
  const eInvoice = values.isEInvoiceUser === true
  const eDespatch = values.isEDespatchUser === true
  const usesEDocuments = eInvoice || eDespatch || values.eArchiveDelivery === 'ELECTRONIC'

  useEffect(() => {
    onAfterSaveChange(async (id) => {
      if (draft !== null) {
        const result = await save({
          customerId: id,
          addresses: draft.map(({ key: _key, ...address }) => ({
            ...address,
            postalCode: address.postalCode || null,
            contactName: address.contactName || null,
            phone: address.phone || null,
          })),
        })
        if ('error' in result) {
          return result.error
        }
      }
      if (buyerDraft !== null) {
        const result = await saveBuyers({
          customerId: id,
          buyers: buyerDraft.map(({ key: _key, ...buyer }) => ({
            ...buyer,
            contactName: buyer.contactName || null,
            email: buyer.email || null,
            phone: buyer.phone || null,
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

  const update = (key: number, change: Partial<AddressDraft>) =>
    setDraft(addresses.map((address) => (address.key === key ? { ...address, ...change } : address)))

  // Varsayılan fatura / teslimat adresi tektir: biri seçilince diğerlerinden kalkar.
  const makeDefault = (key: number, flag: 'isDefaultInvoice' | 'isDefaultDelivery') =>
    setDraft(addresses.map((address) => ({ ...address, [flag]: address.key === key })))

  const addAddress = () => {
    setDraft([...addresses, emptyAddress(nextKey, addresses.length === 0)])
    setNextKey(nextKey - 1)
  }

  const removeAddress = (key: number) => setDraft(addresses.filter((address) => address.key !== key))

  const updateBuyer = (key: number, change: Partial<BuyerDraft>) =>
    setBuyerDraft(buyers.map((buyer) => (buyer.key === key ? { ...buyer, ...change } : buyer)))

  const addBuyer = () => {
    setBuyerDraft([...buyers, emptyBuyer(nextKey)])
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
            {item.key === 'buyers' && buyers.length > 0 && <span className="tabs__count">{buyers.length}</span>}
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
        <fieldset className="form__section">
          <legend>Sipariş ve fatura</legend>
          <div className="form__pair">
            {fields.currency}
            {fields.paymentTermDays}
          </div>
          {fields.documentLanguage}
          {fields.requireUniqueCustomerOrderNo}
        </fieldset>
        {activeControl}
      </div>

      <div className="tabs__panel" role="tabpanel" id="customer-panel-edocument" aria-labelledby="customer-tab-edocument" hidden={tab !== 'edocument'}>
        <p className="muted form__note">
          Mükellefiyet ve posta kutusu etiketleri GİB kayıtlı kullanıcılar listesinden (entegratörünüzün mükellef
          sorgusu) kontrol edilmelidir. Belgede alıcı bilgisi olarak varsayılan fatura adresi kullanılır.
        </p>

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

        {usesEDocuments && addresses.length === 0 && (
          <div className="alert alert--info">e-Belge düzenleyebilmek için Adresler sekmesinde en az bir fatura adresi girin.</div>
        )}
      </div>

      <div className="tabs__panel" role="tabpanel" id="customer-panel-addresses" aria-labelledby="customer-tab-addresses" hidden={tab !== 'addresses'}>
        {stored.isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={stored.error} />
        <datalist id="customer-cities">
          {turkishCities.map((city) => <option key={city} value={city} />)}
        </datalist>

        {addresses.length === 0 && !stored.isLoading && <p className="muted">Kayıtlı adres yok.</p>}
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
                <input value={address.title} maxLength={100} onChange={(event) => update(address.key, { title: event.target.value })} />
              </label>
              <label className="field">
                <span>Ülke</span>
                <select value={address.countryCode} onChange={(event) => update(address.key, { countryCode: event.target.value })}>
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
                  list={address.countryCode === 'TR' ? 'customer-cities' : undefined}
                  onChange={(event) => update(address.key, { city: event.target.value })}
                />
              </label>
              <label className="field">
                <span>İlçe</span>
                <input value={address.district} maxLength={100} onChange={(event) => update(address.key, { district: event.target.value })} />
              </label>
              <label className="field address-card__wide">
                <span>Açık adres</span>
                <textarea
                  rows={2}
                  value={address.addressLine}
                  maxLength={500}
                  placeholder="Mahalle, cadde / sokak, bina ve kapı no"
                  onChange={(event) => update(address.key, { addressLine: event.target.value })}
                />
              </label>
              <label className="field">
                <span>Posta kodu</span>
                <input
                  value={address.postalCode ?? ''}
                  maxLength={10}
                  inputMode={address.countryCode === 'TR' ? 'numeric' : undefined}
                  onChange={(event) => update(address.key, { postalCode: event.target.value })}
                />
              </label>
              <label className="field">
                <span>Telefon</span>
                <input value={address.phone ?? ''} maxLength={30} onChange={(event) => update(address.key, { phone: event.target.value })} />
              </label>
              <label className="field address-card__wide">
                <span>İlgili kişi</span>
                <input value={address.contactName ?? ''} maxLength={100} onChange={(event) => update(address.key, { contactName: event.target.value })} />
              </label>
            </div>
            <div className="address-card__footer">
              <label className="checkbox">
                <input
                  type="radio"
                  name="default-invoice"
                  checked={address.isDefaultInvoice}
                  onChange={() => makeDefault(address.key, 'isDefaultInvoice')}
                />
                Varsayılan fatura adresi
              </label>
              <label className="checkbox">
                <input
                  type="radio"
                  name="default-delivery"
                  checked={address.isDefaultDelivery}
                  onChange={() => makeDefault(address.key, 'isDefaultDelivery')}
                />
                Varsayılan teslimat adresi
              </label>
              <button type="button" className="button button--danger-ghost" onClick={() => removeAddress(address.key)}>
                Kaldır
              </button>
            </div>
          </fieldset>
        ))}
        <div>
          <button type="button" className="button" onClick={addAddress} disabled={disabled}>Adres ekle</button>
        </div>
      </div>

      <div className="tabs__panel" role="tabpanel" id="customer-panel-buyers" aria-labelledby="customer-tab-buyers" hidden={tab !== 'buyers'}>
        <p className="muted form__note">
          Müşteri adına sipariş veren alıcılar (marka, bölge ya da mağaza alıcısı). Siparişte buyer seçilir; fatura
          müşteri adına kesilir. Siparişte kullanılmış buyer silinemez, pasif yapılır.
        </p>
        {storedBuyers.isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={storedBuyers.error} />
        <div className="table-wrap recipe-table buyer-table">
          <table className="table table--compact">
            <colgroup>
              <col className="buyer-table__code" />
              <col />
              <col className="buyer-table__contact" />
              <col className="buyer-table__contact" />
              <col className="buyer-table__phone" />
              <col className="buyer-table__active" />
              <col className="recipe-table__remove" />
            </colgroup>
            <thead>
              <tr>
                <th>Kod</th>
                <th>Ad</th>
                <th>İlgili kişi</th>
                <th>E-posta</th>
                <th>Telefon</th>
                <th>Aktif</th>
                <th aria-label="Satırı kaldır" />
              </tr>
            </thead>
            <tbody>
              {buyers.map((buyer) => (
                <tr key={buyer.key}>
                  <td>
                    <input aria-label="Buyer kodu" value={buyer.code} maxLength={30} disabled={disabled}
                      onChange={(event) => updateBuyer(buyer.key, { code: event.target.value.toUpperCase() })} />
                  </td>
                  <td>
                    <input aria-label="Buyer adı" value={buyer.name} maxLength={200} disabled={disabled}
                      onChange={(event) => updateBuyer(buyer.key, { name: event.target.value })} />
                  </td>
                  <td>
                    <input aria-label="İlgili kişi" value={buyer.contactName ?? ''} maxLength={100} disabled={disabled}
                      onChange={(event) => updateBuyer(buyer.key, { contactName: event.target.value })} />
                  </td>
                  <td>
                    <input aria-label="E-posta" type="email" value={buyer.email ?? ''} maxLength={256} disabled={disabled}
                      onChange={(event) => updateBuyer(buyer.key, { email: event.target.value })} />
                  </td>
                  <td>
                    <input aria-label="Telefon" value={buyer.phone ?? ''} maxLength={30} disabled={disabled}
                      onChange={(event) => updateBuyer(buyer.key, { phone: event.target.value })} />
                  </td>
                  <td className="buyer-table__check">
                    <input type="checkbox" aria-label="Aktif" checked={buyer.isActive} disabled={disabled}
                      onChange={(event) => updateBuyer(buyer.key, { isActive: event.target.checked })} />
                  </td>
                  <td className="recipe-table__remove-cell">
                    <button type="button" className="icon-button" aria-label="Satırı kaldır" title="Satırı kaldır" disabled={disabled}
                      onClick={() => setBuyerDraft(buyers.filter((item) => item.key !== buyer.key))}>
                      <X aria-hidden="true" />
                    </button>
                  </td>
                </tr>
              ))}
              {buyers.length === 0 && !storedBuyers.isLoading && (
                <tr>
                  <td colSpan={7} className="table__empty">Kayıtlı buyer yok.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
        <div>
          <button type="button" className="button" onClick={addBuyer} disabled={disabled}>Buyer ekle</button>
        </div>
      </div>
    </div>
  )
}
