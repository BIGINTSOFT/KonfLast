import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { LookupSelect } from '../masterData/LookupSelect'
import { useOpenFinishedGoodCardMutation } from './stockApi'

/**
 * Sistemde üretilmemiş mamul (açılış stoğu, iade, dışarıdan alınan mamul) için mamul kartı açar. Mamul kartlarını
 * normalde üretimden teslim alma açar; kart açıldıktan sonra miktar "Stok girişi" ile girilir.
 */
export function FinishedGoodCardDialog({ onClose }: { onClose: () => void }) {
  const [openCard, { isLoading, error, data }] = useOpenFinishedGoodCardMutation()
  const [productId, setProductId] = useState<number | null>(null)
  const [colorId, setColorId] = useState<number | null>(null)
  const [sizeId, setSizeId] = useState<number | null>(null)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    await openCard({ productId: productId ?? 0, colorId: colorId ?? 0, sizeId: sizeId ?? 0 })
  }

  return (
    <Modal title="Mamul kartı aç" onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <p className="muted">
          Üretimden teslim alınan mamullerin kartı kendiliğinden açılır. Bu pencere, sistemde üretilmemiş mamulü
          (açılış stoğu, iade) stoğa girebilmek içindir; kartı açtıktan sonra miktarı Stok girişi ile girin.
        </p>
        <LookupSelect resource="products" label="Ürün" value={productId} onChange={setProductId} required errors={fieldErrors(error, 'productId')} />
        <div className="form__pair">
          <LookupSelect resource="colors" label="Renk" value={colorId} onChange={setColorId} required errors={fieldErrors(error, 'colorId')} />
          <LookupSelect resource="sizes" label="Beden" value={sizeId} onChange={setSizeId} required errors={fieldErrors(error, 'sizeId')} />
        </div>
        <ErrorAlert error={error} fields={['productId', 'colorId', 'sizeId']} />
        {data && (
          <p className="alert alert--info" role="status">
            {data.isNew ? 'Mamul kartı açıldı' : 'Bu mamulün kartı zaten var'}: <strong>{data.code}</strong> — {data.name}
          </p>
        )}
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Kapat</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>
            {isLoading ? 'Açılıyor…' : 'Kartı aç'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}
