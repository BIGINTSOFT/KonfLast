import { useState } from 'react'
import type { ChangeEvent } from 'react'
import { isRequestError } from '../../api/envelope'
import { Modal } from '../../components/Modal'
import { useAppDispatch } from '../../store/hooks'
import { convertCell, decodeCsv, mapColumns, parseBoolean, parseCsv, templateCsv } from './csv'
import type { ColumnMapping } from './csv'
import { masterDataApi, useSaveMasterRecordMutation } from './masterDataApi'
import { emptyValue, toRequestBody } from './masterDataForm'
import type { FormValues } from './masterDataForm'
import type { ResourceConfig } from './resources'

const MAX_ROWS = 1000
const PREVIEW_ROWS = 5

interface Parsed {
  fileName: string
  headers: string[]
  rows: string[][]
  mapping: ColumnMapping
}

interface RowError {
  /** Dosyadaki satır numarası (başlık satırı 1). */
  line: number
  message: string
}

function errorMessage(error: unknown): string {
  if (!isRequestError(error)) {
    return 'Beklenmeyen bir hata oluştu.'
  }
  const details = Object.values(error.details ?? {}).flat()
  return details.length > 0 ? details.join(' ') : error.message
}

/**
 * CSV'den toplu kayıt ekleme. Her satır, formdan tek tek eklenmiş gibi sunucunun doğrulamasından geçer;
 * hatalı satırlar atlanır ve nedenleriyle listelenir. Başka kayıtlara referanslar (ör. model, makine tipi)
 * o kaydın koduyla verilir.
 */
export function ImportDialog({ config, onClose }: { config: ResourceConfig; onClose: () => void }) {
  const dispatch = useAppDispatch()
  const [save] = useSaveMasterRecordMutation()
  const [parsed, setParsed] = useState<Parsed | null>(null)
  const [fileError, setFileError] = useState<string | null>(null)
  const [progress, setProgress] = useState<{ done: number; total: number } | null>(null)
  const [result, setResult] = useState<{ created: number; errors: RowError[] } | null>(null)

  const downloadTemplate = () => {
    const url = URL.createObjectURL(new Blob([templateCsv(config.fields)], { type: 'text/csv;charset=utf-8' }))
    const link = document.createElement('a')
    link.href = url
    link.download = `${config.key}-sablon.csv`
    link.click()
    URL.revokeObjectURL(url)
  }

  const handleFile = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    setParsed(null)
    setResult(null)
    setFileError(null)
    if (!file) {
      return
    }

    const [headers, ...rows] = parseCsv(decodeCsv(await file.arrayBuffer()))
    if (!headers || rows.length === 0) {
      setFileError('Dosyada başlık satırı ve en az bir kayıt satırı olmalıdır.')
      return
    }
    if (rows.length > MAX_ROWS) {
      setFileError(`Tek seferde en fazla ${MAX_ROWS} satır içe aktarılabilir (dosyada ${rows.length} satır var).`)
      return
    }

    setParsed({ fileName: file.name, headers, rows, mapping: mapColumns(headers, config.fields) })
  }

  /** Referans verilen kaydın kodunu Id'ye çevirir; sonuçlar bu içe aktarma boyunca hatırlanır. */
  const resolveLookup = async (
    lookupCache: Map<string, number | null>, resource: string, code: string,
  ): Promise<number | null> => {
    const key = `${resource}:${code.toLocaleUpperCase('tr-TR')}`
    if (!lookupCache.has(key)) {
      const request = dispatch(masterDataApi.endpoints.getLookup.initiate({ resource, search: code, take: 100 }))
      const { data } = await request
      request.unsubscribe()
      const match = data?.find((item) => item.code.toLocaleUpperCase('tr-TR') === code.toLocaleUpperCase('tr-TR'))
      lookupCache.set(key, match?.id ?? null)
    }
    return lookupCache.get(key) ?? null
  }

  const handleImport = async () => {
    if (!parsed) {
      return
    }

    const errors: RowError[] = []
    const lookupCache = new Map<string, number | null>()
    let created = 0
    setProgress({ done: 0, total: parsed.rows.length })

    for (const [index, cells] of parsed.rows.entries()) {
      const line = index + 2
      const values: FormValues = {}
      let isActive = true
      const problems: string[] = []

      for (const [column, field] of parsed.mapping.fields.entries()) {
        const text = cells[column] ?? ''
        if (field === null) {
          continue
        }
        if (field === 'isActive') {
          const active = text.trim() === '' ? true : parseBoolean(text)
          if (active === null) {
            problems.push(`Aktif: "${text}" Evet / Hayır olmalı.`)
          } else {
            isActive = active
          }
          continue
        }

        const cell = convertCell(field, text)
        if (cell.error) {
          problems.push(cell.error)
        } else if (cell.lookupCode !== undefined) {
          const id = await resolveLookup(lookupCache, field.lookup!, cell.lookupCode)
          if (id === null) {
            problems.push(`${field.label}: "${cell.lookupCode}" kodlu kayıt bulunamadı.`)
          } else {
            values[field.name] = id
          }
        } else {
          values[field.name] = cell.value ?? null
        }
      }

      if (problems.length > 0) {
        errors.push({ line, message: problems.join(' ') })
      } else {
        // Dosyada sütunu olmayan alanlar formdaki boş değerleriyle gönderilir.
        const complete: FormValues = Object.fromEntries(
          config.fields.map((field) => [field.name, field.name in values ? values[field.name] : emptyValue(field)]),
        )
        const response = await save({ resource: config.key, body: toRequestBody(config.fields, complete, isActive) })
        if ('error' in response) {
          errors.push({ line, message: errorMessage(response.error) })
        } else {
          created += 1
        }
      }

      setProgress({ done: index + 1, total: parsed.rows.length })
    }

    setProgress(null)
    setResult({ created, errors })
  }

  const mapping = parsed?.mapping
  const canImport = parsed !== null && mapping!.missingRequired.length === 0 && progress === null && result === null

  return (
    <Modal title={`${config.title}: CSV içe aktar`} onClose={onClose} variant="drawer">
      <div className="form">
        <p className="muted">
          Şablonu indirip Excel'de doldurun ve CSV olarak kaydedin. İlk satır başlıktır; her satır bir kayıttır. Başka
          kayıtlara bağlanan sütunlara (ör. model, makine tipi) o kaydın <strong>kodu</strong> yazılır. Her satır
          formdaki kurallarla doğrulanır; hatalı satırlar atlanır.
        </p>
        <p>
          <button type="button" className="button" onClick={downloadTemplate}>Şablonu indir</button>
        </p>

        <label className="field">
          <span>CSV dosyası</span>
          <input type="file" accept=".csv,text/csv" onChange={(event) => void handleFile(event)} disabled={progress !== null} />
        </label>
        {fileError && <p className="alert alert--error" role="alert">{fileError}</p>}

        {parsed && mapping && (
          <>
            <p>
              <strong>{parsed.fileName}</strong>: {parsed.rows.length} kayıt satırı.
            </p>
            {mapping.missingRequired.length > 0 && (
              <p className="alert alert--error" role="alert">
                Zorunlu sütunlar eksik: {mapping.missingRequired.join(', ')}.
              </p>
            )}
            {mapping.unknownColumns.length > 0 && (
              <p className="alert alert--info">
                Tanınmayan sütunlar yok sayılacak: {mapping.unknownColumns.join(', ')}.
              </p>
            )}
            {!result && (
              <div className="table-wrap">
                <table className="table table--compact">
                  <thead>
                    <tr>
                      {parsed.headers.map((header, index) => (
                        <th key={`${header}:${index}`} className={mapping.fields[index] === null ? 'muted' : undefined}>
                          {header}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {parsed.rows.slice(0, PREVIEW_ROWS).map((cells, rowIndex) => (
                      <tr key={rowIndex}>
                        {parsed.headers.map((header, index) => (
                          <td key={`${header}:${index}`}>{cells[index] ?? ''}</td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
            {!result && parsed.rows.length > PREVIEW_ROWS && (
              <p className="muted">İlk {PREVIEW_ROWS} satır gösteriliyor.</p>
            )}
          </>
        )}

        {progress && (
          <>
            <p role="status">İçe aktarılıyor: {progress.done} / {progress.total}</p>
            <div className="progress"><progress max={progress.total} value={progress.done} /></div>
          </>
        )}

        {result && (
          <>
            <p className="alert alert--info">
              <strong>{result.created}</strong> kayıt eklendi
              {result.errors.length > 0 ? `, ${result.errors.length} satır atlandı.` : '.'}
            </p>
            {result.errors.length > 0 && (
              <div className="table-wrap">
                <table className="table table--compact">
                  <thead>
                    <tr>
                      <th className="table__number">Satır</th>
                      <th>Neden</th>
                    </tr>
                  </thead>
                  <tbody>
                    {result.errors.map((error) => (
                      <tr key={error.line}>
                        <td className="table__number">{error.line}</td>
                        <td>{error.message}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        )}

        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose} disabled={progress !== null}>
            {result ? 'Kapat' : 'Vazgeç'}
          </button>
          {!result && (
            <button type="button" className="button button--primary" disabled={!canImport} onClick={() => void handleImport()}>
              {progress ? 'İçe aktarılıyor…' : `İçe aktar${parsed ? ` (${parsed.rows.length})` : ''}`}
            </button>
          )}
        </footer>
      </div>
    </Modal>
  )
}
