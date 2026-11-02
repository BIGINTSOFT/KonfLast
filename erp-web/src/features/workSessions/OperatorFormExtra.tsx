import { useEffect, useState } from 'react'
import type { ChangeEvent } from 'react'
import { isRequestError } from '../../api/envelope'
import { CheckboxList, ErrorAlert, TextField } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import type { FormExtraProps } from '../masterData/resources'
import { useAssignCardMutation, useReleaseCardMutation } from '../rfid/rfidApi'
import { photoDataUri, preparePhoto } from './operatorPhoto'
import type { PhotoUpload } from './operatorPhoto'
import { initialsOf, mergeOptions, sameCardNumber } from './workSessionPresentation'
import {
  useDeleteOperatorPhotoMutation,
  useGetOperatorPhotoQuery,
  useGetOperatorQualificationsQuery,
  useSaveOperatorPhotoMutation,
  useSaveOperatorQualificationsMutation,
} from './workSessionsApi'

const toggle = (ids: number[], id: number) => (ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id])

type Tab = 'general' | 'qualifications'

const tabs: { key: Tab; label: string }[] = [
  { key: 'general', label: 'Genel' },
  { key: 'qualifications', label: 'Yetkinlikler' },
]

/**
 * Operatör formunun ek bölümleri: fotoğraf, personel kartı ve yetkinlikler. Bunlar operatör kaydının
 * alanı değildir; operatör kaydedildikten sonra (yeni kayıtta Id oluşunca) kendi uçlarına yazılır.
 */
export function OperatorFormExtra({
  row,
  values,
  fields,
  activeControl,
  disabled,
  hasFieldErrors,
  onAfterSaveChange,
}: FormExtraProps) {
  const [tab, setTab] = useState<Tab>('general')
  // Yetki kutuları dışındaki alanlar Genel sekmesindedir; hata gizli sekmede kalmasın.
  const [seenFieldErrors, setSeenFieldErrors] = useState(hasFieldErrors)
  if (hasFieldErrors !== seenFieldErrors) {
    setSeenFieldErrors(hasFieldErrors)
    if (hasFieldErrors) {
      setTab('general')
    }
  }

  const operatorCode = String(values.operatorCode ?? '').trim()
  const fullName = `${String(values.firstName ?? '').trim()} ${String(values.lastName ?? '').trim()}`.trim()
  const initials = initialsOf(fullName)
  const operatorId = row?.id
  const skip = operatorId === undefined
  const canAssignCard = usePermission(Permissions.OperatorAssignCard)
  // Kayıtlı fotoğraf kişisel veridir; görme yetkisi olmayan kullanıcı için istenmez.
  const canViewPhoto = usePermission(Permissions.OperatorViewPersonal)
  const currentPhoto = useGetOperatorPhotoQuery(operatorId ?? 0, { skip: skip || !canViewPhoto })
  const currentQualifications = useGetOperatorQualificationsQuery(operatorId ?? 0, { skip })
  const operations = useGetLookupQuery({ resource: 'operations', take: 100 })
  const machines = useGetLookupQuery({ resource: 'machines', take: 100 })

  const [savePhoto] = useSaveOperatorPhotoMutation()
  const [deletePhoto] = useDeleteOperatorPhotoMutation()
  const [saveQualifications] = useSaveOperatorQualificationsMutation()
  const [assignCard] = useAssignCardMutation()
  const [releaseCard] = useReleaseCardMutation()

  const currentCard = (row?.rfidUid as string | null | undefined) ?? ''
  const [card, setCard] = useState(currentCard)
  const [selectedPhoto, setSelectedPhoto] = useState<PhotoUpload | null>(null)
  const [removePhoto, setRemovePhoto] = useState(false)
  const [fileError, setFileError] = useState<string | null>(null)
  // Dokunulmadıkça sunucudaki seçim geçerlidir (null = değiştirilmedi).
  const [operationChoice, setOperationChoice] = useState<number[] | null>(null)
  const [machineChoice, setMachineChoice] = useState<number[] | null>(null)

  // Fotoğrafı olmayan operatör için sunucu NOT_FOUND döner; bu bir hata değil, boş durumdur.
  const hasNoPhoto = isRequestError(currentPhoto.error) && currentPhoto.error.status === 404
  const storedPhoto = !skip && canViewPhoto && !hasNoPhoto ? currentPhoto.data : undefined
  const shownPhoto = selectedPhoto ?? (removePhoto ? null : (storedPhoto ?? null))

  const storedOperations = currentQualifications.data?.operations ?? []
  const storedMachines = currentQualifications.data?.machines ?? []
  const operationIds = operationChoice ?? storedOperations.map((item) => item.id)
  const machineIds = machineChoice ?? storedMachines.map((item) => item.id)

  // Form kaydedilince çalışır; her çizimde güncel seçimleri görür.
  useEffect(() => {
    onAfterSaveChange(async (id) => {
      const failed = (result: object) => ('error' in result ? result.error : undefined)

      if (operationChoice !== null || machineChoice !== null) {
        const error = failed(await saveQualifications({ operatorId: id, operationIds, machineIds }))
        if (error) {
          return error
        }
      }

      if (selectedPhoto) {
        const error = failed(await savePhoto({ operatorId: id, ...selectedPhoto }))
        if (error) {
          return error
        }
      } else if (removePhoto && storedPhoto) {
        const error = failed(await deletePhoto(id))
        if (error) {
          return error
        }
      }

      const wantedCard = card.trim()
      if (canAssignCard && !sameCardNumber(wantedCard, currentCard)) {
        if (currentCard) {
          const error = failed(await releaseCard({ owner: 'operators', id }))
          if (error) {
            return error
          }
        }
        if (wantedCard) {
          return failed(await assignCard({ owner: 'operators', id, uid: wantedCard }))
        }
      }

      return undefined
    })

    return () => {
      onAfterSaveChange(null)
    }
  })

  const handleFile = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) {
      return
    }

    try {
      setSelectedPhoto(await preparePhoto(file))
      setRemovePhoto(false)
      setFileError(null)
    } catch {
      setFileError('Dosya okunamadı. JPEG ya da PNG bir fotoğraf seçin.')
    }
  }

  return (
    <>
      {/* Kişi kartı: fotoğraf en üstte, yanında formdan canlı okunan ad ve sicil. */}
      <div className="person-card">
        <label className={`person-card__photo${disabled ? ' person-card__photo--disabled' : ''}`}>
          {shownPhoto ? (
            <img src={photoDataUri(shownPhoto)} alt="Operatör fotoğrafı" />
          ) : (
            <span className="person-card__initials" aria-hidden="true">{initials || '+'}</span>
          )}
          <span className="person-card__photo-action">{shownPhoto ? 'Değiştir' : 'Fotoğraf ekle'}</span>
          <input
            type="file"
            accept="image/jpeg,image/png"
            className="visually-hidden"
            aria-label="Fotoğraf seç"
            disabled={disabled}
            onChange={(event) => void handleFile(event)}
          />
        </label>
        <div className="person-card__body">
          <strong className="person-card__name">{fullName || 'Yeni operatör'}</strong>
          <span className="muted">{operatorCode ? `Sicil No · ${operatorCode}` : 'Sicil numarası girilmedi'}</span>
          {shownPhoto && (
            <button
              type="button"
              className="button"
              disabled={disabled}
              onClick={() => {
                setSelectedPhoto(null)
                setRemovePhoto(true)
              }}
            >
              Fotoğrafı kaldır
            </button>
          )}
        </div>
        <div className="person-card__status">{activeControl}</div>
      </div>
      {!hasNoPhoto && <ErrorAlert error={currentPhoto.error} />}
      {fileError && <div className="alert alert--error" role="alert">{fileError}</div>}

      <div className="tabs" role="tablist" aria-label="Operatör bilgileri">
        {tabs.map((item) => (
          <button
            key={item.key}
            type="button"
            role="tab"
            id={`operator-tab-${item.key}`}
            aria-selected={tab === item.key}
            aria-controls={`operator-panel-${item.key}`}
            className={`tabs__tab${tab === item.key ? ' tabs__tab--active' : ''}`}
            onClick={() => setTab(item.key)}
          >
            {item.label}
          </button>
        ))}
      </div>

      {/* Sekmeler gizlenir, kaldırılmaz: diğer sekmedeki girişler korunur. */}
      <div
        className="tabs__panel"
        role="tabpanel"
        id="operator-panel-general"
        aria-labelledby="operator-tab-general"
        hidden={tab !== 'general'}
      >
      {fields.operatorCode}
      {fields.firstName}
      {fields.lastName}
      {fields.breakGroupId}
      {fields.role}

      <TextField
        label="Personel kartı numarası"
        value={card}
        maxLength={10}
        disabled={disabled || !canAssignCard}
        hint={canAssignCard ? undefined : 'Personel kartı atama yetkiniz yok.'}
        onChange={(event) => setCard(event.target.value)}
        autoComplete="off"
        // Okuyucu numaradan sonra Enter gönderir; formun yanlışlıkla kaydedilmesini önler.
        onKeyDown={(event) => {
          if (event.key === 'Enter') {
            event.preventDefault()
          }
        }}
      />
      </div>

      <div
        className="tabs__panel"
        role="tabpanel"
        id="operator-panel-qualifications"
        aria-labelledby="operator-tab-qualifications"
        hidden={tab !== 'qualifications'}
      >
      <CheckboxList legend="Kişiye özel ek tablet yetkileri">
        {fields.canPause}
        {fields.canReportBreakdown}
      </CheckboxList>
      {(currentQualifications.isLoading || operations.isLoading || machines.isLoading) && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={currentQualifications.error ?? operations.error ?? machines.error} />
      {operations.data && machines.data && (skip || currentQualifications.data) && (
        <>
          <CheckboxList legend="Yapabildiği operasyonlar">
            {mergeOptions(operations.data, storedOperations).map((item) => (
              <label key={item.id} className="checkbox">
                <input
                  type="checkbox"
                  disabled={disabled}
                  checked={operationIds.includes(item.id)}
                  onChange={() => setOperationChoice(toggle(operationIds, item.id))}
                />
                {item.code} — {item.name}
              </label>
            ))}
          </CheckboxList>
          {operationIds.length === 0 && (
            <p className="alert alert--info">Hiç operasyon seçilmezse operatör hiçbir makinede oturum açamaz.</p>
          )}

          <CheckboxList legend="Çalışabileceği makineler">
            {mergeOptions(machines.data, storedMachines).map((item) => (
              <label key={item.id} className="checkbox">
                <input
                  type="checkbox"
                  disabled={disabled}
                  checked={machineIds.includes(item.id)}
                  onChange={() => setMachineChoice(toggle(machineIds, item.id))}
                />
                {item.code} — {item.name}
              </label>
            ))}
          </CheckboxList>
        </>
      )}
      </div>
    </>
  )
}
