import { formatQuantity } from '../../utils/format'
import type { QuantityMatrix } from './orderPresentation'

/** Renk × beden miktar tablosu; satır ve sütun toplamlarıyla. */
export function QuantityMatrixTable({ matrix }: { matrix: QuantityMatrix }) {
  return (
    <div className="table-wrap">
      <table className="table table--matrix">
        <thead>
          <tr>
            <th>Renk</th>
            {matrix.sizes.map((size) => (
              <th key={size.id} className="table__number">{size.name}</th>
            ))}
            <th className="table__number">Toplam</th>
          </tr>
        </thead>
        <tbody>
          {matrix.colors.map((color) => (
            <tr key={color.id}>
              <th scope="row">{color.name} <span className="muted">({color.code})</span></th>
              {matrix.sizes.map((size) => {
                const quantity = matrix.quantity(color.id, size.id)
                return <td key={size.id} className="table__number">{quantity ? formatQuantity(quantity) : '—'}</td>
              })}
              <td className="table__number"><strong>{formatQuantity(matrix.colorTotal(color.id))}</strong></td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <th scope="row">Toplam</th>
            {matrix.sizes.map((size) => (
              <td key={size.id} className="table__number">{formatQuantity(matrix.sizeTotal(size.id))}</td>
            ))}
            <td className="table__number"><strong>{formatQuantity(matrix.total)}</strong></td>
          </tr>
        </tfoot>
      </table>
    </div>
  )
}
