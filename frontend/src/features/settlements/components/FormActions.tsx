type FormActionsProps = {
  isSaving: boolean;
  onCancel: () => void;
};

export function FormActions({ isSaving, onCancel }: FormActionsProps): JSX.Element {
  return (
    <div className="settlement-form-actions">
      <button type="button" className="button-secondary" onClick={onCancel} disabled={isSaving}>
        Cancel
      </button>
      <button type="submit" className="button-primary" disabled={isSaving}>
        {isSaving ? "Saving..." : "Save"}
      </button>
    </div>
  );
}

