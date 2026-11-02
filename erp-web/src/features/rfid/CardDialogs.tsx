import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { useAssignCardMutation, useReleaseCardMutation } from './rfidApi'
import type { CardOwner } from './rfidApi'

interface CardDialogProps {
  owner: CardOwner
  id: number
  /** Kartın atanacağı kaydın adı, örn. "Demet 000154". */
  subject: string
  onClose: () => void
}

/**
 * Kart atama. Masaüstü USB okuyucular klavye gibi davranır: imleç alandayken kart okutulunca kart numarası
 * yazılır ve Enter gönderilir; numara elle de girilebilir.
 */
export function AssignCardDialog({ owner, id, subject, onClose }: CardDialogProps) {
  const [assign, { isLoading, error }] = useAssignCardMutation()
  const [uid, setUid] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await assign({ owner, id, uid: uid.trim() })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Kart ata: ${subject}`} onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <TextField
          label="Kart numarası"
          value={uid}
          maxLength={10}
          onChange={(event) => setUid(event.target.value)}
          errors={fieldErrors(error, 'uid')}
          hint="Kartı okuyucuya okutun ya da kart numarasını yazın (örn. 0012629368)."
          autoFocus
          autoComplete="off"
        />
        <ErrorAlert error={error} fields={['uid']} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={!uid.trim() || isLoading}>
            {isLoading ? 'Atanıyor…' : 'Kartı ata'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}

export function ReleaseCardDialog({ owner, id, subject, uid, onClose }: CardDialogProps & { uid: string }) {
  const [release, { isLoading, error }] = useReleaseCardMutation()

  const handleRelease = async () => {
    const result = await release({ owner, id })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Kartı bırak: ${subject}`} onClose={onClose}>
      <div className="form">
        <p>
          <code>{uid}</code> kartının {subject} ile bağı kaldırılacak. Kart başka bir kayda atanabilir; geçmiş
          atama kaydı silinmez.
        </p>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="button" className="button button--danger" disabled={isLoading} onClick={() => void handleRelease()}>
            {isLoading ? 'Bırakılıyor…' : 'Kartı bırak'}
          </button>
        </footer>
      </div>
    </Modal>
  )
}
