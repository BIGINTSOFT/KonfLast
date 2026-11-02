import { useState } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import type { MasterRow } from '../masterData/masterDataApi'
import { useResetBoyahaneTabletDeviceMutation } from './boyahaneBatchesApi'

/** Boyahane tabletinin cihaz işlemi: eşleştirilmiş cihazın bağını koparma. */
export function BoyahaneTabletRowActions({ row }: { row: MasterRow }) {
  const canManage = usePermission(Permissions.BoyahaneFloorManage)
  const [resetDevice, resetState] = useResetBoyahaneTabletDeviceMutation()
  const [confirming, setConfirming] = useState(false)

  if (!canManage || !row.isEnrolled) {
    return null
  }

  const confirmReset = async () => {
    if (!('error' in await resetDevice(row.id))) {
      setConfirming(false)
    }
  }

  return (
    <>
      <button type="button" className="button" onClick={() => setConfirming(true)}>Cihazı sıfırla</button>
      {confirming && (
        <Modal title="Cihazı sıfırla" onClose={() => setConfirming(false)}>
          <div className="form">
            <p><strong>{String(row.tabletCode)}</strong> ile eşleştirilmiş cihazın bağı koparılacak; tablet yeniden eşleştirilmelidir.</p>
            <ErrorAlert error={resetState.error} />
            <footer className="form__actions">
              <button type="button" className="button" onClick={() => setConfirming(false)}>Vazgeç</button>
              <button type="button" className="button button--danger" disabled={resetState.isLoading} onClick={() => void confirmReset()}>Sıfırla</button>
            </footer>
          </div>
        </Modal>
      )}
    </>
  )
}
