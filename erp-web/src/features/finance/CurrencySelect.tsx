import { LOCAL_CURRENCY, useCurrencyOptions } from './currencyOptions'

interface CurrencySelectProps {
  label: string
  value: string
  onChange: (value: string) => void
  errors?: string[]
  hint?: string
  disabled?: boolean
}

/** Para birimi seçimi; seçenekler tanımlı para birimlerinden gelir. */
export function CurrencySelect({ label, value, onChange, errors, hint, disabled }: CurrencySelectProps) {
  const options = useCurrencyOptions(value)
  return (
    <label className="field">
      <span>{label}</span>
      <select
        value={value || LOCAL_CURRENCY}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value)}
        aria-invalid={Boolean(errors?.length)}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>{option.label}</option>
        ))}
      </select>
      {hint && !errors?.length && <small className="field__hint">{hint}</small>}
      {errors?.map((message) => (
        <small key={message} className="field__error">{message}</small>
      ))}
    </label>
  )
}
