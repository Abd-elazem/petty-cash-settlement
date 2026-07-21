import type { SettlementLineDto } from "../../../types/settlements";
import { formatCurrency } from "../utils";

type SettlementLineTableProps = {
  lines: SettlementLineDto[];
  editable: boolean;
  isOperationPending: boolean;
  onEdit: (lineId: string) => void;
  onDelete: (lineId: string) => void;
};

export function SettlementLineTable({
  lines,
  editable,
  isOperationPending,
  onEdit,
  onDelete
}: SettlementLineTableProps): JSX.Element {
  return (
    <div className="settlement-table-wrap">
      <table className="settlement-table">
        <thead>
          <tr>
            <th>Line</th>
            <th>Category</th>
            <th>Gross</th>
            <th>VAT</th>
            <th>Net</th>
            <th>Is VAT</th>
            <th>Notes</th>
            <th>Car Plate</th>
            <th>Odometer (km)</th>
            {editable ? <th>Actions</th> : null}
          </tr>
        </thead>
        <tbody>
          {lines.map((line) => (
            <tr key={line.lineId}>
              <td>{line.lineNo}</td>
              <td>{line.categoryCode}</td>
              <td>{formatCurrency(line.grossAmount)}</td>
              <td>{formatCurrency(line.vatAmount)}</td>
              <td>{formatCurrency(line.netAmount)}</td>
              <td>{line.isVat ? "Yes" : "No"}</td>
              <td>{line.notes ?? "—"}</td>
              <td>{line.carPlate ?? "—"}</td>
              <td>{line.odometerKm ?? "—"}</td>
              {editable ? (
                <td>
                  <div className="line-actions-inline">
                    <button
                      type="button"
                      className="button-link"
                      onClick={() => onEdit(line.lineId)}
                      disabled={isOperationPending}
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      className="button-link button-link-danger"
                      onClick={() => onDelete(line.lineId)}
                      disabled={isOperationPending}
                    >
                      Delete
                    </button>
                  </div>
                </td>
              ) : null}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

