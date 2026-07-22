import { useState } from "react";

export type EditHeaderFormValues = {
  settlementDate: string;
  purpose: string;
};

export type EditHeaderFormErrors = {
  settlementDate?: string;
  purpose?: string;
};

type Props = {
  initialValues: EditHeaderFormValues;
  isSaving: boolean;
  apiError: string | null;
  onSave: (values: EditHeaderFormValues) => void;
  onCancel: () => void;
};

function validate(values: EditHeaderFormValues): EditHeaderFormErrors {
  const errors: EditHeaderFormErrors = {};
  if (!values.settlementDate) {
    errors.settlementDate = "Settlement date is required.";
  }
  if (!values.purpose.trim()) {
    errors.purpose = "Purpose is required.";
  } else if (values.purpose.trim().length > 500) {
    errors.purpose = "Purpose must be 500 characters or fewer.";
  }
  return errors;
}

/**
 * Inline edit form for the settlement header (date + purpose).
 * Rendered inside SettlementDetailPage when the user clicks "Edit Header".
 * Only shown for Draft settlements (enforced by the parent — this component
 * does not re-check IsEditable itself to avoid duplicated policy logic).
 */
export function EditSettlementHeaderForm({
  initialValues,
  isSaving,
  apiError,
  onSave,
  onCancel,
}: Props): JSX.Element {
  const [values, setValues] = useState<EditHeaderFormValues>(initialValues);
  const [errors, setErrors] = useState<EditHeaderFormErrors>({});

  const handleChange = (field: keyof EditHeaderFormValues, value: string): void => {
    setValues((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const next = { ...prev };
        delete next[field];
        return next;
      });
    }
  };

  const handleSubmit = (): void => {
    const validationErrors = validate(values);
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0 || isSaving) {
      return;
    }
    onSave(values);
  };

  return (
    <div className="card settlement-header-edit-form">
      <h3>Edit Settlement Header</h3>

      <div className="form-field">
        <label htmlFor="edit-settlement-date">Settlement Date</label>
        <input
          id="edit-settlement-date"
          type="date"
          value={values.settlementDate}
          disabled={isSaving}
          onChange={(e) => handleChange("settlementDate", e.target.value)}
          aria-describedby={errors.settlementDate ? "edit-date-error" : undefined}
          aria-invalid={Boolean(errors.settlementDate)}
        />
        {errors.settlementDate ? (
          <span id="edit-date-error" className="form-field-error">
            {errors.settlementDate}
          </span>
        ) : null}
      </div>

      <div className="form-field">
        <label htmlFor="edit-purpose">Purpose</label>
        <input
          id="edit-purpose"
          type="text"
          value={values.purpose}
          maxLength={500}
          disabled={isSaving}
          onChange={(e) => handleChange("purpose", e.target.value)}
          aria-describedby={errors.purpose ? "edit-purpose-error" : undefined}
          aria-invalid={Boolean(errors.purpose)}
        />
        {errors.purpose ? (
          <span id="edit-purpose-error" className="form-field-error">
            {errors.purpose}
          </span>
        ) : null}
      </div>

      {apiError ? (
        <div className="settlement-form-api-error" role="alert">
          {apiError}
        </div>
      ) : null}

      <div className="form-actions">
        <button
          type="button"
          className="button-primary"
          onClick={handleSubmit}
          disabled={isSaving}
        >
          {isSaving ? "Saving…" : "Save Changes"}
        </button>
        <button
          type="button"
          className="button-secondary"
          onClick={onCancel}
          disabled={isSaving}
        >
          Cancel
        </button>
      </div>
    </div>
  );
}
