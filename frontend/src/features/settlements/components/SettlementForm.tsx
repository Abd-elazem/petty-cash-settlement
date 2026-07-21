import { FormActions } from "./FormActions";
import { FormField } from "./FormField";

type SettlementFormValues = {
  settlementDate: string;
  purpose: string;
};

type SettlementFormErrors = Partial<Record<keyof SettlementFormValues, string>>;

type SettlementFormProps = {
  values: SettlementFormValues;
  errors: SettlementFormErrors;
  apiError: string | null;
  isSaving: boolean;
  onChange: (field: keyof SettlementFormValues, value: string) => void;
  onSubmit: () => Promise<void>;
  onCancel: () => void;
};

export function SettlementForm({
  values,
  errors,
  apiError,
  isSaving,
  onChange,
  onSubmit,
  onCancel
}: SettlementFormProps): JSX.Element {
  return (
    <form
      className="card settlement-form"
      onSubmit={(event) => {
        event.preventDefault();
        void onSubmit();
      }}
      noValidate
    >
      <h2>New Settlement</h2>
      <p>Create a new draft settlement to start adding expense lines.</p>

      {apiError ? <div className="settlement-form-api-error">{apiError}</div> : null}

      <FormField htmlFor="settlement-date" label="Settlement Date" error={errors.settlementDate}>
        <input
          id="settlement-date"
          type="date"
          value={values.settlementDate}
          onChange={(event) => onChange("settlementDate", event.target.value)}
          disabled={isSaving}
        />
      </FormField>

      <FormField htmlFor="settlement-purpose" label="Purpose" error={errors.purpose}>
        <textarea
          id="settlement-purpose"
          value={values.purpose}
          onChange={(event) => onChange("purpose", event.target.value)}
          maxLength={500}
          rows={4}
          disabled={isSaving}
        />
      </FormField>

      <FormActions isSaving={isSaving} onCancel={onCancel} />
    </form>
  );
}

