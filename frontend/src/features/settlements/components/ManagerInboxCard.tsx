import { Link } from "react-router-dom";
import type { SettlementDto } from "../../../types/settlements";
import { formatCurrency, formatDate } from "../utils";
import { StatusBadge } from "./StatusBadge";

type ManagerInboxCardProps = {
  settlement: SettlementDto;
};

export function ManagerInboxCard({ settlement }: ManagerInboxCardProps): JSX.Element {
  return (
    <Link to={`/manager-inbox/${settlement.requestId}`} className="settlement-card">
      <div className="settlement-card-row">
        <span className="settlement-label">Request ID</span>
        <span className="settlement-value settlement-id">{settlement.requestId}</span>
      </div>
      <div className="settlement-card-row">
        <span className="settlement-label">Employee</span>
        <span className="settlement-value">{settlement.spenderName}</span>
      </div>
      <div className="settlement-card-row">
        <span className="settlement-label">Settlement Date</span>
        <span className="settlement-value">{formatDate(settlement.settlementDate)}</span>
      </div>
      <div className="settlement-card-row">
        <span className="settlement-label">Purpose</span>
        <span className="settlement-value">{settlement.purpose}</span>
      </div>
      <div className="settlement-card-row">
        <span className="settlement-label">Total Amount</span>
        <span className="settlement-value">{formatCurrency(settlement.totalAmount)}</span>
      </div>
      <div className="settlement-card-row">
        <span className="settlement-label">Status</span>
        <StatusBadge status={settlement.status} />
      </div>
    </Link>
  );
}
