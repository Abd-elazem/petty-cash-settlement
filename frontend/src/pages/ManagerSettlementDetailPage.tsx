import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { GlobalLoading } from "../components/GlobalLoading";
import { ConfirmationDialog } from "../features/settlements/components/ConfirmationDialog";
import { EmptyState } from "../features/settlements/components/EmptyState";
import { ErrorState } from "../features/settlements/components/ErrorState";
import { RejectSettlementDialog } from "../features/settlements/components/RejectSettlementDialog";
import { SettlementHeader } from "../features/settlements/components/SettlementHeader";
import { SettlementLineCard } from "../features/settlements/components/SettlementLineCard";
import { SettlementLineTable } from "../features/settlements/components/SettlementLineTable";
import { SettlementMetadata } from "../features/settlements/components/SettlementMetadata";
import { useApproveSettlement } from "../features/settlements/hooks/useApproveSettlement";
import { useRejectSettlement } from "../features/settlements/hooks/useRejectSettlement";
import { useSettlement } from "../features/settlements/hooks/useSettlement";

export function ManagerSettlementDetailPage(): JSX.Element {
  const { requestId } = useParams<{ requestId: string }>();
  const navigate = useNavigate();

  const { settlement, isLoading, isNotFound, error, refresh, replaceSettlement } = useSettlement(requestId);
  const { approveSettlement, isApproving, apiError: approveError, clearApiError: clearApproveError } =
    useApproveSettlement();
  const { rejectSettlement, isRejecting, apiError: rejectApiError, clearApiError: clearRejectError } =
    useRejectSettlement();

  const [isApproveDialogOpen, setApproveDialogOpen] = useState(false);
  const [isRejectDialogOpen, setRejectDialogOpen] = useState(false);
  const [rejectComment, setRejectComment] = useState("");
  const [rejectCommentError, setRejectCommentError] = useState<string | null>(null);

  const isSubmitted = settlement?.status === "Submitted";
  const anyPending = isApproving || isRejecting;

  const openApproveDialog = (): void => {
    clearApproveError();
    setApproveDialogOpen(true);
  };

  const openRejectDialog = (): void => {
    setRejectComment("");
    setRejectCommentError(null);
    clearRejectError();
    setRejectDialogOpen(true);
  };

  const handleApprove = async (): Promise<void> => {
    if (!requestId) return;
    const updated = await approveSettlement(requestId);
    if (updated) {
      replaceSettlement(updated);
      setApproveDialogOpen(false);
    }
  };

  const handleReject = async (): Promise<void> => {
    if (!requestId) return;

    const trimmed = rejectComment.trim();
    if (!trimmed) {
      setRejectCommentError("Rejection reason is required.");
      return;
    }
    if (trimmed.length > 1000) {
      setRejectCommentError("Rejection reason must be 1000 characters or fewer.");
      return;
    }
    setRejectCommentError(null);

    const updated = await rejectSettlement(requestId, trimmed);
    if (updated) {
      replaceSettlement(updated);
      setRejectDialogOpen(false);
      setRejectComment("");
    }
  };

  const handleCommentChange = (value: string): void => {
    setRejectComment(value);
    if (rejectCommentError) setRejectCommentError(null);
    if (rejectApiError) clearRejectError();
  };

  return (
    <div className="page-container">
      <button
        type="button"
        className="button-link settlement-back-link"
        onClick={() => navigate("/manager-inbox")}
      >
        ← Back to Manager Inbox
      </button>

      {isLoading ? <GlobalLoading message="Loading settlement…" /> : null}

      {!isLoading && isNotFound ? (
        <EmptyState
          title="Settlement not found"
          description={`No settlement was found for request ID ${requestId ?? "-"}.`}
        />
      ) : null}

      {!isLoading && error ? (
        <ErrorState message={error} onRetry={() => void refresh()} />
      ) : null}

      {!isLoading && !isNotFound && !error && settlement ? (
        <div className="settlement-detail-layout">
          <SettlementHeader
            requestId={settlement.requestId}
            status={settlement.status}
            purpose={settlement.purpose}
            settlementDate={settlement.settlementDate}
            totalAmount={settlement.totalAmount}
            version={settlement.version}
          />

          <SettlementMetadata
            approverEmail={settlement.approverEmail}
            spenderName={settlement.spenderName}
            workerId={settlement.workerId}
            approvalComment={settlement.approvalComment}
            journalBatchNumber={settlement.journalBatchNumber}
          />

          {/* Manager action panel — only shown while the settlement is Submitted */}
          {isSubmitted ? (
            <div className="card manager-action-panel">
              <h3>Approval decision</h3>
              <p>Review the lines below, then approve or reject this settlement.</p>
              <div className="manager-action-buttons">
                <button
                  type="button"
                  className="button-primary"
                  onClick={openApproveDialog}
                  disabled={anyPending}
                >
                  Approve
                </button>
                <button
                  type="button"
                  className="button-secondary button-danger"
                  onClick={openRejectDialog}
                  disabled={anyPending}
                >
                  Reject
                </button>
              </div>
            </div>
          ) : null}

          <div className="card">
            <div className="settlement-lines-header">
              <h3>Settlement Lines</h3>
            </div>

            {settlement.lines.length === 0 ? (
              <p>No lines on this settlement.</p>
            ) : (
              <>
                <div className="settlement-lines-desktop">
                  {/* editable=false — manager view is always read-only */}
                  <SettlementLineTable
                    lines={settlement.lines}
                    editable={false}
                    isOperationPending={false}
                    onEdit={() => undefined}
                    onDelete={() => undefined}
                  />
                </div>
                <div className="settlement-lines-mobile">
                  {settlement.lines.map((line) => (
                    <SettlementLineCard
                      key={line.lineId}
                      line={line}
                      editable={false}
                      isOperationPending={false}
                      onEdit={() => undefined}
                      onDelete={() => undefined}
                    />
                  ))}
                </div>
              </>
            )}
          </div>
        </div>
      ) : null}

      <ConfirmationDialog
        title="Approve settlement"
        message={`Approve settlement ${requestId ?? ""}? The spender will be notified and a journal entry will be created.`}
        isOpen={isApproveDialogOpen}
        isPending={isApproving}
        error={approveError}
        confirmLabel="Approve"
        pendingLabel="Approving…"
        onConfirm={handleApprove}
        onCancel={() => {
          if (!isApproving) {
            setApproveDialogOpen(false);
            clearApproveError();
          }
        }}
      />

      <RejectSettlementDialog
        isOpen={isRejectDialogOpen}
        isRejecting={isRejecting}
        comment={rejectComment}
        commentError={rejectCommentError}
        apiError={rejectApiError}
        onCommentChange={handleCommentChange}
        onConfirm={handleReject}
        onCancel={() => {
          if (!isRejecting) {
            setRejectDialogOpen(false);
            setRejectComment("");
            setRejectCommentError(null);
            clearRejectError();
          }
        }}
      />
    </div>
  );
}
