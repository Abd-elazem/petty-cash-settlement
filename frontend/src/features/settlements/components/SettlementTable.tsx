import { Link } from "react-router-dom";
import type { SettlementSummaryDto } from "../../../types/settlements";
import { formatCurrency, formatDate } from "../utils";
import { StatusBadge } from "./StatusBadge";

type SettlementTableProps = {
  settlements: SettlementSummaryDto[];
};

export function SettlementTable({ settlements }: SettlementTableProps): JSX.Element {
  return (
    <div className="settlement-table-wrap">
      <table className="settlement-table">
        <thead>
          <tr>
            <th>Request ID</th>
            <th>Settlement Date</th>
            <th>Purpose</th>
            <th>Total Amount</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {settlements.map((settlement) => (
            <tr key={settlement.requestId}>
              <td>
                <Link to={`/my-settlements/${settlement.requestId}`} className="settlement-id-link">
                  {settlement.requestId}
                </Link>
              </td>
              <td>{formatDate(settlement.settlementDate)}</td>
              <td>{settlement.purpose}</td>
              <td>{formatCurrency(settlement.totalAmount)}</td>
              <td>
                <StatusBadge status={settlement.status} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

