import { useEffect, useRef } from 'react'
import type { InputHTMLAttributes, ReactNode, Ref } from 'react'
import { isRequestError } from '../api/envelope'

interface TextFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string
  /** API'nin bu alan için döndüğü doğrulama mesajları. */
  errors?: string[]
  hint?: string
  ref?: Ref<HTMLInputElement>
}

export function TextField({ label, errors, hint, ...input }: TextFieldProps) {
  return (
    <label className="field">
      <span>{label}</span>
      <input {...input} aria-invalid={Boolean(errors?.length)} />
      {hint && !errors?.length && <small className="field__hint">{hint}</small>}
      {errors?.map((message) => (
        <small key={message} className="field__error">{message}</small>
      ))}
    </label>
  )
}

/** Alanlara bağlanamayan API hatasını gösterir; alan hataları ilgili alanın altında çıkar. */
export function ErrorAlert({ error, fields = [] }: { error: unknown; fields?: string[] }) {
  if (!isRequestError(error)) {
    return null
  }

  const shownOnFields = Object.keys(error.details ?? {}).some((key) => fields.includes(key))
  if (shownOnFields) {
    return null
  }

  return <ErrorAlertBox message={error.message} details={Object.values(error.details ?? {}).flat()} error={error} />
}

/**
 * Hata kutusu görünür alana kaydırılır: uzun formlarda (ör. çok satırlı sipariş) kutu Kaydet'in hemen üstünde,
 * kaydırma alanının altında kalıp görünmüyordu. Her yeni hatada (aynı mesaj tekrar gelse de) yeniden kaydırılır.
 */
function ErrorAlertBox({ message, details, error }: { message: string; details: string[]; error: unknown }) {
  const ref = useRef<HTMLDivElement>(null)
  useEffect(() => {
    ref.current?.scrollIntoView({ block: 'nearest', behavior: 'smooth' })
  }, [error])

  return (
    <div ref={ref} className="alert alert--error" role="alert">
      {message}
      {details.length > 0 && (
        <ul>
          {details.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      )}
    </div>
  )
}

export function CheckboxList({ legend, children }: { legend: string; children: ReactNode }) {
  return (
    <fieldset className="checkbox-list">
      <legend>{legend}</legend>
      {children}
    </fieldset>
  )
}
