/**
 * Tablet teması: web yönetim ekranıyla aynı kurumsal renkler (petrol mavisi ana renk, koyu lacivert panel).
 * Ölçüler 10 inçlik tablete göre ayarlıdır.
 */
export const colors = {
  background: '#f3f5f8',
  surface: '#ffffff',
  surfaceMuted: '#f8f9fb',
  /** Koyu sol panel. */
  header: '#0e1724',
  headerText: '#ffffff',
  headerMuted: '#aab6c7',
  /** Koyu panel üzerindeki kartlar ve ayırıcılar. */
  headerSurface: '#152133',
  headerBorder: '#26354a',
  text: '#111a27',
  textSecondary: '#364152',
  muted: '#5f6b7c',
  border: '#e1e5eb',
  borderStrong: '#c9d0da',
  primary: '#0d5c6e',
  primarySoft: '#e2f0f2',
  /** Marka vurgusu (logodaki ince çizgi). */
  brandAccent: '#c9a24a',
  success: '#136c45',
  successSoft: '#e1f3e9',
  danger: '#b42318',
  dangerSoft: '#fdeceb',
  warning: '#8a5700',
  warningSoft: '#fcefd2',
  neutral: '#364152',
  neutralSoft: '#eef1f5',
  info: '#0d5c6e',
};

/** Ana işlemlerin dokunma yüksekliği. */
export const TOUCH_HEIGHT = 52;

/** İkincil işlemler (üst çubuk, panel içi, pencere düğmeleri) için dokunma yüksekliği. */
export const TOUCH_HEIGHT_COMPACT = 42;

export const radius = { card: 10, control: 6, pill: 999 };

/** İçeriğin üzerinde açılan öğeler (pencere, ileti, yükleniyor göstergesi) için belirgin gölge. */
export const overlayShadow = {
  elevation: 12,
  shadowColor: '#101828',
  shadowOpacity: 0.22,
  shadowRadius: 24,
  shadowOffset: { width: 0, height: 12 },
};

