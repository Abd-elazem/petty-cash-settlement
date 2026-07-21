import { formatCurrency, formatDate } from "../utils";
import { StatusBadge } from "./StatusBadge";

type SettlementHeaderProps = {
  requestId: string;
  status: string;
  purpose: string;
  settlementDate: string;
  totalAmount: number;
  version: number;
};

export function SettlementHeader({
  requestId,
  status,
  purpose,
  settlementDate,
  totalAmount,
  version
}: SettlementHeaderProps): JSX.Element {
  return (
    <div className="card settlement-detail-header">
      <div className="settlement-detail-header-top">
        <div>
          <h2>Settlement {requestId}</h2>
          <p>{purpose}</p>
        </div>
        <StatusBadge status={status} />
      </div>
      <div className="settlement-detail-summary">
        <div>
          <span>Settlement Date</span>
          <strong>{formatDate(settlementDate)}</strong>
        </div>
        <div>
          <span>Total Amount</span>
          <strong>{formatCurrency(totalAmount)}</strong>
        </div>
        <div>
          <span>Version</span>
          <strong>v{version}</strong>
        </div>
      </div>
    </div>
  );
}

