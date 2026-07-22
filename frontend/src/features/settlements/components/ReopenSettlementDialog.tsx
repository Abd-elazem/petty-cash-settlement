import { ConfirmationDialog } from "./ConfirmationDialog";

type ReopenSettlementDialogProps = {
  isOpen: boolean;
  isReopening: boolean;
  error: string | null;
  onConfirm: () => Promise<void>;
  onCancel: () => void;
};

export function ReopenSettlementDialog({
  isOpen,
  isReopening,
  error,
  onConfirm,
  onCancel
}: ReopenSettlementDialogProps): JSX.Element {
  return (
    <ConfirmationDialog
      title="Reopen settlement"
      message="Reopen this rejected settlement for editing? You will be able to modify lines and resubmit."
      isOpen={isOpen}
      isPending={isReopening}
      error={error}
      confirmLabel="Reopen Settlement"
      pendingLabel="Reopening..."
      onConfirm={onConfirm}
      onCancel={onCancel}
    />
  );
}
