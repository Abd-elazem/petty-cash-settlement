import { GlobalLoading } from "../components/GlobalLoading";
import { EmptyState } from "../features/settlements/components/EmptyState";
import { ErrorState } from "../features/settlements/components/ErrorState";
import { ManagerInboxCard } from "../features/settlements/components/ManagerInboxCard";
import { ManagerInboxTable } from "../features/settlements/components/ManagerInboxTable";
import { useApproverInbox } from "../features/settlements/hooks/useApproverInbox";

export function ManagerInboxPage(): JSX.Element {
  const { settlements, isLoading, error, refresh } = useApproverInbox();

  return (
    <div className="page-container">
      <div className="settlements-page-header">
        <div>
          <h2>Manager Inbox</h2>
          <p>Settlements awaiting your approval decision.</p>
        </div>
        <div className="settlements-header-actions">
          <button
            type="button"
            className="button-primary"
            onClick={() => void refresh()}
            disabled={isLoading}
          >
            Refresh
          </button>
        </div>
      </div>

      {isLoading ? <GlobalLoading message="Loading inbox…" /> : null}

      {!isLoading && error ? (
        <ErrorState message={error} onRetry={() => void refresh()} />
      ) : null}

      {!isLoading && !error && settlements.length === 0 ? (
        <EmptyState
          title="Inbox is empty"
          description="No settlements are currently awaiting your approval."
        />
      ) : null}

      {!isLoading && !error && settlements.length > 0 ? (
        <>
          <div className="settlements-desktop-view">
            <ManagerInboxTable settlements={settlements} />
          </div>
          <div className="settlements-mobile-view">
            {settlements.map((s) => (
              <ManagerInboxCard key={s.requestId} settlement={s} />
            ))}
          </div>
        </>
      ) : null}
    </div>
  );
}
