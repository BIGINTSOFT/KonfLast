import { createContext, useContext } from 'react'
import type { ContextMenuItem } from '../components/ContextMenu'
import type { NavItem } from './navigation'

/** Ekranların çalışma sekmeleriyle yapabildikleri (kabuk sağlar). */
export interface WorkTabsApi {
  /** Adresi yeni bir sekmede açar ve o sekmeye geçer. */
  openInNewTab: (path: string) => void
  /** Menüden ekran açar: aynı adres açık bir sekmede varsa ona geçer, yoksa yeni sekme açar. */
  openScreen: (path: string) => void
  /** Sekme sınırı dolu ve kapatılabilecek sekme yoksa false. */
  canOpenInNewTab: boolean
  /**
   * Sağ tık menüsünün "Kısayol ekle" öğesi: alt menüsünde ana sayfa ve kullanıcının özel sekmeleri listelenir.
   * Tek ekran için seçilen sekmede kısayol varsa kaldırılır, yoksa eklenir; birden fazla ekran topluca eklenir.
   */
  shortcutMenu: (items: NavItem[]) => ContextMenuItem
  /** Durum çubuğunda kısa süreli bilgi iletisi gösterir. */
  notify: (message: string) => void
}

export const WorkTabsContext = createContext<WorkTabsApi>({
  openInNewTab: () => undefined,
  openScreen: () => undefined,
  canOpenInNewTab: false,
  shortcutMenu: () => ({ key: 'shortcut', label: 'Kısayol ekle', disabled: true }),
  notify: () => undefined,
})

export const useWorkTabs = () => useContext(WorkTabsContext)
