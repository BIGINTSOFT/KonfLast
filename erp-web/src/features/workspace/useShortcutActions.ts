import type { NavItem } from '../../layouts/navigation'
import { useGetShortcutsQuery, useSaveShortcutsMutation } from './workspaceApi'

/** Sunucunun kabul ettiği en fazla kısayol sayısı. */
export const MAX_SHORTCUTS = 24

/**
 * Menüden ana sayfa kısayolu ekleme / kaldırma (sağ tık menüleri için). Sonuç, kullanıcıya gösterilmek üzere
 * `notify` ile bildirilir; kısayollar kullanıcıya özeldir ve sunucuda saklanır.
 */
export function useShortcutActions(notify: (message: string) => void) {
  const { data: paths } = useGetShortcutsQuery()
  const [save] = useSaveShortcutsMutation()

  const add = async (items: NavItem[]) => {
    if (!paths) {
      return
    }

    // Ana sayfanın kendisi kısayol olmaz.
    const fresh = items.filter((item) => item.path !== '/' && !paths.includes(item.path))
    if (fresh.length === 0) {
      notify(items.length === 1 ? `"${items[0].label}" zaten kısayollarınızda.` : 'Bu ekranların hepsi zaten kısayollarınızda.')
      return
    }

    const added = fresh.slice(0, MAX_SHORTCUTS - paths.length)
    if (added.length === 0) {
      notify(`En fazla ${MAX_SHORTCUTS} kısayol eklenebilir; önce ana sayfadan bir kısayolu kaldırın.`)
      return
    }

    const result = await save([...paths, ...added.map((item) => item.path)])
    if ('error' in result) {
      notify('Kısayol eklenemedi.')
    } else if (added.length < fresh.length) {
      notify(`${added.length} ekran kısayollara eklendi; ${fresh.length - added.length} ekran sınır (${MAX_SHORTCUTS}) dolduğu için eklenmedi.`)
    } else {
      notify(added.length === 1 ? `"${added[0].label}" ana sayfa kısayollarına eklendi.` : `${added.length} ekran ana sayfa kısayollarına eklendi.`)
    }
  }

  const remove = async (item: NavItem) => {
    if (!paths) {
      return
    }

    const result = await save(paths.filter((path) => path !== item.path))
    notify('error' in result ? 'Kısayol kaldırılamadı.' : `"${item.label}" kısayollardan kaldırıldı.`)
  }

  return { ready: paths !== undefined, has: (path: string) => paths?.includes(path) ?? false, add, remove }
}
