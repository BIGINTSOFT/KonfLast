export type PartyType = 'COMPANY' | 'INDIVIDUAL'
export type EInvoiceProfile = 'BASIC' | 'COMMERCIAL'
export type EArchiveDelivery = 'ELECTRONIC' | 'PAPER'

export const partyTypeLabels: Record<PartyType, string> = {
  COMPANY: 'Tüzel kişi (şirket)',
  INDIVIDUAL: 'Gerçek kişi (şahıs)',
}

export const eInvoiceProfileLabels: Record<EInvoiceProfile, string> = {
  BASIC: 'Temel fatura (TEMELFATURA)',
  COMMERCIAL: 'Ticari fatura (TICARIFATURA)',
}

// Kâğıt önce gelir: yeni müşteride varsayılandır (elektronik teslim e-posta ister).
export const eArchiveDeliveryLabels: Record<EArchiveDelivery, string> = {
  PAPER: 'Kâğıt',
  ELECTRONIC: 'Elektronik (e-posta)',
}

export const optionsOf = (labels: Record<string, string>) =>
  Object.entries(labels).map(([value, label]) => ({ value, label }))

/** Sık kullanılan ülkeler; listede olmayan ülke ISO kodu yazılarak girilir. */
export const countries: { code: string; name: string }[] = [
  { code: 'TR', name: 'Türkiye' },
  { code: 'DE', name: 'Almanya' },
  { code: 'GB', name: 'Birleşik Krallık' },
  { code: 'NL', name: 'Hollanda' },
  { code: 'FR', name: 'Fransa' },
  { code: 'IT', name: 'İtalya' },
  { code: 'ES', name: 'İspanya' },
  { code: 'BE', name: 'Belçika' },
  { code: 'PL', name: 'Polonya' },
  { code: 'RO', name: 'Romanya' },
  { code: 'BG', name: 'Bulgaristan' },
  { code: 'GR', name: 'Yunanistan' },
  { code: 'RU', name: 'Rusya' },
  { code: 'AZ', name: 'Azerbaycan' },
  { code: 'IQ', name: 'Irak' },
  { code: 'AE', name: 'Birleşik Arap Emirlikleri' },
  { code: 'US', name: 'Amerika Birleşik Devletleri' },
]

/** Türkiye'nin 81 ili (il alanında öneri listesi). */
export const turkishCities = [
  'Adana', 'Adıyaman', 'Afyonkarahisar', 'Ağrı', 'Aksaray', 'Amasya', 'Ankara', 'Antalya', 'Ardahan', 'Artvin', 'Aydın',
  'Balıkesir', 'Bartın', 'Batman', 'Bayburt', 'Bilecik', 'Bingöl', 'Bitlis', 'Bolu', 'Burdur', 'Bursa', 'Çanakkale',
  'Çankırı', 'Çorum', 'Denizli', 'Diyarbakır', 'Düzce', 'Edirne', 'Elazığ', 'Erzincan', 'Erzurum', 'Eskişehir',
  'Gaziantep', 'Giresun', 'Gümüşhane', 'Hakkâri', 'Hatay', 'Iğdır', 'Isparta', 'İstanbul', 'İzmir', 'Kahramanmaraş',
  'Karabük', 'Karaman', 'Kars', 'Kastamonu', 'Kayseri', 'Kırıkkale', 'Kırklareli', 'Kırşehir', 'Kilis', 'Kocaeli',
  'Konya', 'Kütahya', 'Malatya', 'Manisa', 'Mardin', 'Mersin', 'Muğla', 'Muş', 'Nevşehir', 'Niğde', 'Ordu', 'Osmaniye',
  'Rize', 'Sakarya', 'Samsun', 'Siirt', 'Sinop', 'Sivas', 'Şanlıurfa', 'Şırnak', 'Tekirdağ', 'Tokat', 'Trabzon',
  'Tunceli', 'Uşak', 'Van', 'Yalova', 'Yozgat', 'Zonguldak',
]
