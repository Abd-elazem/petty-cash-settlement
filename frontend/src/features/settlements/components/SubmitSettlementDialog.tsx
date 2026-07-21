import { ConfirmationDialog } from "./ConfirmationDialog";
type SubmitSettlementDialogProps = {
  isOpen: boolean;
  isSubmitting: boolean;
  error: string | null;
  onConfirm: () => Promise<void>;
  onCancel: () => void;
};

export function SubmitSettlementDialog({
  isOpen,
  isSubmitting,
  error,
  onConfirm,
  onCancel
}: SubmitSettlementDialogProps): JSX.Element {
  return (
    <ConfirmationDialog
      title="Submit settlement"
      message="Submit this settlement for approval? You will not be able to edit lines after submission."
      isOpen={isOpen}
      isPending={isSubmitting}
      error={error}
      confirmLabel="Submit Settlement"
      pendingLabel="Submitting..."
      onConfirm={onConfirm}
      onCancel={onCancel}
    />
  );
}
