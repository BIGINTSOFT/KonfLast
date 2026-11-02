import { useEffect, useState } from 'react'
import type { ChangeEvent } from 'react'
import { isRequestError } from '../../api/envelope'
import { CheckboxList, ErrorAlert } from '../../components/FormControls'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import type { FormExtraProps } from '../masterData/resources'
import { photoDataUri, preparePhoto } from '../workSessions/operatorPhoto'
import type { PhotoUpload } from '../workSessions/operatorPhoto'
import { initialsOf, mergeOptions } from '../workSessions/workSessionPresentation'
import {
  useDeleteBoyahaneOperatorPhotoMutation,
  useGetBoyahaneOperatorPhotoQuery,
  useGetBoyahaneOperatorQualificationsQuery,
  useSaveBoyahaneOperatorPhotoMutation,
  useSaveBoyahaneOperatorQualificationsMutation,
} from './boyahaneOperatorApi'

const toggle = (ids: number[], id: number) => (ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id])

type Tab = 'general' | 'qualifications'

const tabs: { key: Tab; label: string }[] = [
  { key: 'general', label: 'Genel' },
  { key: 'qualifications', label: 'Yetkinlikler' },
]

/**
 * Boyahane operatör formunun ek bölümleri (Konfeksiyon operatör kartıyla aynı): fotoğraf ve yetkinlikler. Bunlar
 * kaydın alanı değildir; operatör kaydedildikten sonra kendi uçlarına yazılır.
 */
export function BoyahaneOperatorFormExtra({ row, values, fields, activeControl, disabled, hasFieldErrors, onAfterSaveChange }: FormExtraProps) {
  const [tab, setTab] = useState<Tab>('general')
  // Yetki kutuları dışındaki alanlar Genel sekmesindedir; hata gizli sekmede kalmasın.
  const [seenFieldErrors, setSeenFieldErrors] = useState(hasFieldErrors)
  if (hasFieldErrors !== seenFieldErrors) {
    setSeenFieldErrors(hasFieldErrors)
    if (hasFieldErrors) {
      setTab('general')
    }
  }

  const code = String(values.code ?? '').trim()
  const fullName = `${String(values.firstName ?? '').trim()} ${String(values.lastName ?? '').trim()}`.trim()
  const initials = initialsOf(fullName)
  const operatorId = row?.id
  const skip = operatorId === undefined
  const currentPhoto = useGetBoyahaneOperatorPhotoQuery(operatorId ?? 0, { skip })
  const currentQualifications = useGetBoyahaneOperatorQualificationsQuery(operatorId ?? 0, { skip })
  const operations = useGetLookupQuery({ resource: 'boyahane-operations', take: 100 })
  const machines = useGetLookupQuery({ resource: 'boyahane-machines', take: 100 })

  const [savePhoto] = useSaveBoyahaneOperatorPhotoMutation()
  const [deletePhoto] = useDeleteBoyahaneOperatorPhotoMutation()
  const [saveQualifications] = useSaveBoyahaneOperatorQualificationsMutation()

  const [selectedPhoto, setSelectedPhoto] = useState<PhotoUpload | null>(null)
  const [removePhoto, setRemovePhoto] = useState(false)
  const [fileError, setFileError] = useState<string | null>(null)
  // Dokunulmadıkça sunucudaki seçim geçerlidir (null = değiştirilmedi).
  const [operationChoice, setOperationChoice] = useState<number[] | null>(null)
  const [machineChoice, setMachineChoice] = useState<number[] | null>(null)

  // Fotoğrafı olmayan operatör için sunucu NOT_FOUND döner; bu bir hata değil, boş durumdur.
  const hasNoPhoto = isRequestError(currentPhoto.error) && currentPhoto.error.status === 404
  const storedPhoto = !skip && !hasNoPhoto ? currentPhoto.data : undefined
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
        return failed(await savePhoto({ operatorId: id, ...selectedPhoto }))
      }

      if (removePhoto && storedPhoto) {
        return failed(await deletePhoto(id))
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
          <span className="muted">{code ? `Sicil No · ${code}` : 'Sicil numarası girilmedi'}</span>
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
            id={`boyahane-operator-tab-${item.key}`}
            aria-selected={tab === item.key}
            aria-controls={`boyahane-operator-panel-${item.key}`}
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
        id="boyahane-operator-panel-general"
        aria-labelledby="boyahane-operator-tab-general"
        hidden={tab !== 'general'}
      >
        {fields.code}
        {fields.firstName}
        {fields.lastName}
        {fields.role}
        {fields.cardUid}
        {fields.breakGroupId}
      </div>

      <div
        className="tabs__panel"
        role="tabpanel"
        id="boyahane-operator-panel-qualifications"
        aria-labelledby="boyahane-operator-tab-qualifications"
        hidden={tab !== 'qualifications'}
      >
        <CheckboxList legend="Kişiye özel ek tablet yetkileri">
          {fields.canPause}
          {fields.canReportBreakdown}
        </CheckboxList>
        <p className="muted">İşaretliyse operatör bu işlemleri kendi kartıyla, yalnızca oturumunun açık olduğu tablette yapar.</p>
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
              <p className="alert alert--info">Hiç operasyon seçilmezse operatör hiçbir adımı başlatamaz.</p>
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
