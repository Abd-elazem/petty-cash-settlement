type SettlementMetadataProps = {
  approverEmail: string;
  spenderName: string;
  workerId: string;
  approvalComment: string | null;
  journalBatchNumber: string | null;
};

export function SettlementMetadata({
  approverEmail,
  spenderName,
  workerId,
  approvalComment,
  journalBatchNumber
}: SettlementMetadataProps): JSX.Element {
  return (
    <div className="card settlement-detail-metadata">
      <h3>Metadata</h3>
      <div className="settlement-meta-grid">
        <div>
          <span>Spender</span>
          <strong>{spenderName}</strong>
        </div>
        <div>
          <span>Worker ID</span>
          <strong>{workerId}</strong>
        </div>
        <div>
          <span>Approver Email</span>
          <strong>{approverEmail}</strong>
        </div>
        {approvalComment ? (
          <div>
            <span>Approver Comment</span>
            <strong>{approvalComment}</strong>
          </div>
        ) : null}
        {journalBatchNumber ? (
          <div>
            <span>Journal Number</span>
            <strong>{journalBatchNumber}</strong>
          </div>
        ) : null}
      </div>
    </div>
  );
}

