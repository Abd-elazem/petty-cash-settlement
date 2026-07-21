import type { SettlementLineDto } from "../../../types/settlements";
import { formatCurrency } from "../utils";

type SettlementLineCardProps = {
  line: SettlementLineDto;
  editable: boolean;
  isOperationPending: boolean;
  onEdit: (lineId: string) => void;
  onDelete: (lineId: string) => void;
};

export function SettlementLineCard({
  line,
  editable,
  isOperationPending,
  onEdit,
  onDelete
}: SettlementLineCardProps): JSX.Element {
  return (
    <div className="settlement-line-card">
      <div className="settlement-line-card-row">
        <span className="settlement-label">Line</span>
        <span className="settlement-value">#{line.lineNo}</span>
      </div>
      <div className="settlement-line-card-row">
        <span className="settlement-label">Category</span>
        <span className="settlement-value">{line.categoryCode}</span>
      </div>
      <div className="settlement-line-card-row">
        <span className="settlement-label">Gross</span>
        <span className="settlement-value">{formatCurrency(line.grossAmount)}</span>
      </div>
      <div className="settlement-line-card-row">
        <span className="settlement-label">VAT</span>
        <span className="settlement-value">{formatCurrency(line.vatAmount)}</span>
      </div>
      <div className="settlement-line-card-row">
        <span className="settlement-label">Net</span>
        <span className="settlement-value">{formatCurrency(line.netAmount)}</span>
      </div>
      <div className="settlement-line-card-row">
        <span className="settlement-label">Is VAT</span>
        <span className="settlement-value">{line.isVat ? "Yes" : "No"}</span>
      </div>
      {line.notes ? (
        <div className="settlement-line-card-row">
          <span className="settlement-label">Notes</span>
          <span className="settlement-value">{line.notes}</span>
        </div>
      ) : null}
      {line.carPlate ? (
        <div className="settlement-line-card-row">
          <span className="settlement-label">Car Plate</span>
          <span className="settlement-value">{line.carPlate}</span>
        </div>
      ) : null}
      {line.odometerKm !== null ? (
        <div className="settlement-line-card-row">
          <span className="settlement-label">Odometer (km)</span>
          <span className="settlement-value">{line.odometerKm}</span>
        </div>
      ) : null}
      {editable ? (
        <div className="line-actions-inline">
          <button type="button" className="button-link" onClick={() => onEdit(line.lineId)} disabled={isOperationPending}>
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
      ) : null}
    </div>
  );
}

