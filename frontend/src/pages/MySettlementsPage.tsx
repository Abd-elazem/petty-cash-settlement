import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { GlobalLoading } from "../components/GlobalLoading";
import { EmptyState } from "../features/settlements/components/EmptyState";
import { ErrorState } from "../features/settlements/components/ErrorState";
import { SettlementCard } from "../features/settlements/components/SettlementCard";
import { SettlementTable } from "../features/settlements/components/SettlementTable";
import { useMySettlements } from "../features/settlements/hooks/useMySettlements";

export function MySettlementsPage(): JSX.Element {
  const navigate = useNavigate();
  const [searchQuery, setSearchQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const { settlements, availableStatuses, isLoading, error, refresh } = useMySettlements({
    searchQuery,
    statusFilter
  });

  const hasFilters = useMemo(
    () => searchQuery.trim().length > 0 || statusFilter !== "all",
    [searchQuery, statusFilter]
  );

  return (
    <div className="page-container">
      <div className="settlements-page-header">
        <div>
          <h2>My Settlements</h2>
          <p>View your submitted and draft petty cash settlements.</p>
        </div>
        <div className="settlements-header-actions">
          <button type="button" className="button-secondary" onClick={() => navigate("/settlements/new")}>
            New Settlement
          </button>
          <button type="button" className="button-primary" onClick={() => void refresh()} disabled={isLoading}>
            Refresh
          </button>
        </div>
      </div>

      <div className="card settlements-filters">
        <label className="settlements-filter-field">
          <span>Search</span>
          <input
            type="search"
            value={searchQuery}
            placeholder="Search by request ID or purpose"
            onChange={(event) => setSearchQuery(event.target.value)}
          />
        </label>
        <label className="settlements-filter-field">
          <span>Status</span>
          <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
            {availableStatuses.map((status) => (
              <option key={status} value={status}>
                {status === "all" ? "All statuses" : status}
              </option>
            ))}
          </select>
        </label>
      </div>

      {isLoading ? <GlobalLoading message="Loading settlements..." /> : null}

      {!isLoading && error ? <ErrorState message={error} onRetry={() => void refresh()} /> : null}

      {!isLoading && !error && settlements.length === 0 && !hasFilters ? (
        <EmptyState
          title="No settlements yet"
          description="You have not created any petty cash settlements yet."
        />
      ) : null}

      {!isLoading && !error && settlements.length === 0 && hasFilters ? (
        <EmptyState
          title="No matching settlements"
          description="Try changing the search text or status filter."
        />
      ) : null}

      {!isLoading && !error && settlements.length > 0 ? (
        <>
          <div className="settlements-desktop-view">
            <SettlementTable settlements={settlements} />
          </div>
          <div className="settlements-mobile-view">
            {settlements.map((settlement) => (
              <SettlementCard key={settlement.requestId} settlement={settlement} />
            ))}
          </div>
        </>
      ) : null}
    </div>
  );
}

