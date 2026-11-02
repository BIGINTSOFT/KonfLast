import { useState } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import type { MasterRow } from '../masterData/masterDataApi'
import { useResetTabletDeviceMutation } from './tabletsApi'

/** Tablet satırındaki cihaz işlemi: eşleştirilmiş cihazın bağını koparma. */
export function TabletRowActions({ row }: { row: MasterRow }) {
  const canManage = usePermission(Permissions.TabletManage)
  const [resetDevice, resetState] = useResetTabletDeviceMutation()
  const [confirming, setConfirming] = useState(false)

  if (!canManage || !row.isEnrolled) {
    return null
  }

  const confirmReset = async () => {
    const result = await resetDevice(row.id)
    if (!('error' in result)) {
      setConfirming(false)
    }
  }

  return (
    <>
      <button type="button" className="button" onClick={() => setConfirming(true)}>
        Cihazı sıfırla
      </button>

      {confirming && (
        <Modal title="Cihazı sıfırla" onClose={() => setConfirming(false)}>
          <div className="form">
            <p>
              <strong>{String(row.tabletCode)}</strong> ile eşleştirilmiş cihazın bağı koparılacak. Cihaz artık sisteme
              bağlanamaz; tekrar kullanmak için tablet uygulamasında tablet koduyla yeniden eşleştirilmesi gerekir.
            </p>
            <ErrorAlert error={resetState.error} />
            <footer className="form__actions">
              <button type="button" className="button" onClick={() => setConfirming(false)}>Vazgeç</button>
              <button type="button" className="button button--danger" disabled={resetState.isLoading} onClick={() => void confirmReset()}>
                Sıfırla
              </button>
            </footer>
          </div>
        </Modal>
      )}
    </>
  )
}
