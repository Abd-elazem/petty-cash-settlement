import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { SettlementForm } from "../features/settlements/components/SettlementForm";
import { useCreateSettlement } from "../features/settlements/hooks/useCreateSettlement";

type SettlementFormValues = {
  settlementDate: string;
  purpose: string;
};

type SettlementFormErrors = Partial<Record<keyof SettlementFormValues, string>>;

function validateCreateDraft(values: SettlementFormValues): SettlementFormErrors {
  const errors: SettlementFormErrors = {};

  if (!values.settlementDate) {
    errors.settlementDate = "Settlement date is required.";
  }

  const purpose = values.purpose.trim();
  if (purpose.length === 0) {
    errors.purpose = "Purpose is required.";
  } else if (purpose.length > 500) {
    errors.purpose = "Purpose must be 500 characters or fewer.";
  }

  return errors;
}

export function NewSettlementPage(): JSX.Element {
  const navigate = useNavigate();
  const { isSaving, apiError, createSettlement, clearApiError } = useCreateSettlement();

  const [values, setValues] = useState<SettlementFormValues>({
    settlementDate: "",
    purpose: ""
  });
  const [errors, setErrors] = useState<SettlementFormErrors>({});

  const hasValidationErrors = useMemo(() => Object.keys(errors).length > 0, [errors]);

  const handleChange = (field: keyof SettlementFormValues, value: string): void => {
    setValues((previous) => ({ ...previous, [field]: value }));
    if (errors[field]) {
      setErrors((previous) => {
        const next = { ...previous };
        delete next[field];
        return next;
      });
    }
    if (apiError) {
      clearApiError();
    }
  };

  const handleSubmit = async (): Promise<void> => {
    const validation = validateCreateDraft(values);
    setErrors(validation);
    if (Object.keys(validation).length > 0 || isSaving) {
      return;
    }

    const created = await createSettlement({
      settlementDate: values.settlementDate,
      purpose: values.purpose.trim()
    });

    if (created) {
      navigate(`/my-settlements/${created.requestId}`);
    }
  };

  return (
    <div className="page-container">
      <button type="button" className="button-link settlement-back-link" onClick={() => navigate("/my-settlements")}>
        ← Back to My Settlements
      </button>
      <SettlementForm
        values={values}
        errors={errors}
        apiError={apiError}
        isSaving={isSaving}
        onChange={handleChange}
        onSubmit={handleSubmit}
        onCancel={() => navigate("/my-settlements")}
      />
      {hasValidationErrors ? (
        <div className="settlement-form-hint">Please fix the highlighted fields before saving.</div>
      ) : null}
    </div>
  );
}

