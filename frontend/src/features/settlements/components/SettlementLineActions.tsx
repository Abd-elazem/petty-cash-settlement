type SettlementLineActionsProps = {
  mode: "add" | "edit";
  isSaving: boolean;
  onCancel: () => void;
};

export function SettlementLineActions({ mode, isSaving, onCancel }: SettlementLineActionsProps): JSX.Element {
  return (
    <div className="settlement-line-form-actions">
      <button type="button" className="button-secondary" onClick={onCancel} disabled={isSaving}>
        Cancel
      </button>
      <button type="submit" className="button-primary" disabled={isSaving}>
        {isSaving ? (mode === "edit" ? "Saving..." : "Adding...") : mode === "edit" ? "Save Changes" : "Add Line"}
      </button>
    </div>
  );
}

