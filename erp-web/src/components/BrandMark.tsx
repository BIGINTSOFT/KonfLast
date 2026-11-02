/** Marka işareti: kumaş üstünde zikzak dikiş ve altında dikiş hattı. Süs amaçlıdır; adı yanındaki metin taşır. */
export function BrandMark() {
  return (
    <svg className="brand-mark" viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <rect width="32" height="32" rx="8" fill="#0d5c6e" />
      <path
        d="M7 20 L13 9.5 L19 20 L25 9.5"
        fill="none"
        stroke="#ffffff"
        strokeWidth="2.6"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path d="M6.5 25 H25.5" fill="none" stroke="#c9a24a" strokeWidth="2" strokeLinecap="round" strokeDasharray="3 3.2" />
    </svg>
  )
}
