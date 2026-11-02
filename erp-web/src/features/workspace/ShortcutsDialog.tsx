import { ArrowDown, ArrowUp, X } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { navModules } from '../../layouts/navigation'
import type { NavGroup } from '../../layouts/navigation'
import { resolveShortcuts } from '../../layouts/tabs'
import { MAX_SHORTCUTS } from './useShortcutActions'
import { useSaveShortcutsMutation } from './workspaceApi'

/**
 * Kullanıcının ana sayfa kısayolları: solda görebildiği ekranlar (işaretleyerek seçer), sağda seçtikleri
 * (sıralar). Seçim kullanıcıya özeldir ve sunucuda saklanır.
 */
export function ShortcutsDialog({ groups, paths, keptPaths, onClose }: { groups: NavGroup[]; paths: string[]; keptPaths: string[]; onClose: () => void }) {
  const [save, { isLoading, error }] = useSaveShortcutsMutation()
  // Artık görülemeyen (yetkisi kalkmış / kaldırılmış) ekranlar listeden düşer.
  const [picked, setPicked] = useState(() => resolveShortcuts(groups, paths).map((item) => item.path))

  // Ana sayfanın kendisi kısayol olmaz.
  const modules = navModules(groups)
    .map((module) => ({ ...module, items: module.groups.flatMap((group) => group.items).filter((item) => item.path !== '/') }))
    .filter((module) => module.items.length > 0)
  const pickedItems = resolveShortcuts(groups, picked)
  const full = picked.length + keptPaths.length >= MAX_SHORTCUTS

  const toggle = (path: string) =>
    setPicked((current) => (current.includes(path) ? current.filter((item) => item !== path) : [...current, path]))

  const move = (index: number, offset: number) =>
    setPicked((current) => {
      const next = [...current]
      const [item] = next.splice(index, 1)
      next.splice(index + offset, 0, item)
      return next
    })

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save([...picked, ...keptPaths])
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title="Kısayollarım" onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <p className="muted">
          Ana sayfanızda görmek istediğiniz ekranları işaretleyin; sağdaki listede sıralayın. Kısayollar yalnızca
          sizin hesabınız için geçerlidir.
        </p>

        <div className="shortcut-editor">
          <div className="shortcut-editor__column">
            {modules.map((module) => (
              <div key={module.title}>
                <h3>{module.title}</h3>
                {module.items.map((item) => (
                  <label key={item.path} className="checkbox">
                    <input
                      type="checkbox"
                      checked={picked.includes(item.path)}
                      disabled={full && !picked.includes(item.path)}
                      onChange={() => toggle(item.path)}
                    />
                    {item.label}
                  </label>
                ))}
              </div>
            ))}
          </div>

          <div className="shortcut-editor__column">
            <h3>Seçilenler ({picked.length} / {MAX_SHORTCUTS - keptPaths.length})</h3>
            {pickedItems.length === 0 && <p className="muted">Henüz ekran seçmediniz.</p>}
            <ol className="shortcut-editor__picked">
              {pickedItems.map((item, index) => (
                <li key={item.path}>
                  <span>{item.label}</span>
                  <button type="button" className="button button--tiny" aria-label={`${item.label}: yukarı taşı`} disabled={index === 0} onClick={() => move(index, -1)}>
                    <ArrowUp aria-hidden="true" />
                  </button>
                  <button type="button" className="button button--tiny" aria-label={`${item.label}: aşağı taşı`} disabled={index === pickedItems.length - 1} onClick={() => move(index, 1)}>
                    <ArrowDown aria-hidden="true" />
                  </button>
                  <button type="button" className="button button--tiny" aria-label={`${item.label}: kaldır`} onClick={() => toggle(item.path)}>
                    <X aria-hidden="true" />
                  </button>
                </li>
              ))}
            </ol>
          </div>
        </div>

        <ErrorAlert error={error} />

        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>
            {isLoading ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}
