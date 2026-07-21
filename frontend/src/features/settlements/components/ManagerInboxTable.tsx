import { Link } from "react-router-dom";
import type { SettlementDto } from "../../../types/settlements";
import { formatCurrency, formatDate } from "../utils";
import { StatusBadge } from "./StatusBadge";

type ManagerInboxTableProps = {
  settlements: SettlementDto[];
};

export function ManagerInboxTable({ settlements }: ManagerInboxTableProps): JSX.Element {
  return (
    <div className="settlement-table-wrap">
      <table className="settlement-table">
        <thead>
          <tr>
            <th>Request ID</th>
            <th>Employee</th>
            <th>Settlement Date</th>
            <th>Purpose</th>
            <th>Total Amount</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {settlements.map((s) => (
            <tr key={s.requestId}>
              <td>
                <Link to={`/manager-inbox/${s.requestId}`} className="settlement-id-link">
                  {s.requestId}
                </Link>
              </td>
              <td>{s.spenderName}</td>
              <td>{formatDate(s.settlementDate)}</td>
              <td>{s.purpose}</td>
              <td>{formatCurrency(s.totalAmount)}</td>
              <td>
                <StatusBadge status={s.status} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
