import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate } from '../../utils/format'
import { dyeChemicalUnit, useCreateLabRequestMutation, useGetPreviousRecipesQuery, useReuseLabRecipeMutation } from './boyahaneLabApi'
import type { BoyahaneOrderLine } from './boyahaneOrdersApi'

/** Sipariş satırı için lab talebi. Aynı müşteri, kart ve renkte önceki onaylı reçete varsa önerilir; laboratuvar atlanabilir. */
export function OpenLabRequestDialog({ line, onClose }: { line: BoyahaneOrderLine; onClose: () => void }) {
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.BoyahaneLabManage)
  const canDecide = usePermission(Permissions.BoyahaneLabCustomerDecision)
  const { data: recipes, error, isLoading } = useGetPreviousRecipesQuery(line.id)
  const [create, createState] = useCreateLabRequestMutation()
  const [reuse, reuseState] = useReuseLabRecipeMutation()
  const [targetNote, setTargetNote] = useState('')

  const handleCreate = async () => {
    const result = await create({ orderLineId: line.id, targetNote: targetNote.trim() || null })
    if ('data' in result && result.data) {
      navigate(`/boyahane/lab/${result.data.id}`)
    }
  }

  const handleReuse = async (trialId: number) => {
    const result = await reuse({ orderLineId: line.id, trialId })
    if ('data' in result && result.data) {
      navigate(`/boyahane/lab/${result.data.id}`)
    }
  }

  return (
    <Modal title={`Laboratuvar: ${line.colorName ?? ''}`} onClose={onClose} variant="drawer">
      <p className="muted">{line.itemCode} — {line.itemName}{line.colorCode && ` · ${line.colorCode}`}</p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? createState.error ?? reuseState.error} />

      {recipes && recipes.length > 0 && (
        <fieldset className="form__section">
          <legend>Önceki onaylı reçeteler</legend>
          <p className="muted form__note">Aynı müşteri, kart ve renk için daha önce onaylandı. Kullanırsanız laboratuvar atlanır, talep onaylı açılır.</p>
          {recipes.map((recipe) => (
            <div key={recipe.trialId}>
              <strong>{recipe.requestNo}/{recipe.trialCode}</strong>
              <span className="muted"> · {recipe.orderNo}{recipe.decisionOn && ` · ${formatDate(recipe.decisionOn)}`}</span>
              <p className="muted">{recipe.items.map((item) => `${item.code} ${item.amount} ${dyeChemicalUnit[item.kind]}`).join(' · ')}</p>
              {canDecide && <button type="button" className="button" disabled={reuseState.isLoading} onClick={() => handleReuse(recipe.trialId)}>Bu reçeteyi kullan</button>}
            </div>
          ))}
        </fieldset>
      )}

      {canManage && (
        <fieldset className="form__section">
          <legend>Yeni laboratuvar talebi</legend>
          <label className="field">
            <span>Hedef renk notu</span>
            <input maxLength={500} placeholder="Müşteri numunesi, ışık kaynağı …" value={targetNote} onChange={(event) => setTargetNote(event.target.value)} />
          </label>
          <footer className="form__actions">
            <button type="button" className="button" onClick={onClose}>Vazgeç</button>
            <button type="button" className="button button--primary" disabled={createState.isLoading} onClick={handleCreate}>Talebi aç</button>
          </footer>
        </fieldset>
      )}
    </Modal>
  )
}
