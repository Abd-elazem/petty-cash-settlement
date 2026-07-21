import { ConfirmationDialog } from "./ConfirmationDialog";
type DeleteConfirmationDialogProps = {
  title: string;
  message: string;
  isOpen: boolean;
  isDeleting: boolean;
  error: string | null;
  onConfirm: () => Promise<void>;
  onCancel: () => void;
};

export function DeleteConfirmationDialog({
  title,
  message,
  isOpen,
  isDeleting,
  error,
  onConfirm,
  onCancel
}: DeleteConfirmationDialogProps): JSX.Element {
  return (
    <ConfirmationDialog
      title={title}
      message={message}
      isOpen={isOpen}
      isPending={isDeleting}
      error={error}
      confirmLabel="Delete"
      pendingLabel="Deleting..."
      confirmButtonClassName="button-primary button-danger"
      onConfirm={onConfirm}
      onCancel={onCancel}
    />
  );
}

