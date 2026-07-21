import type { CategoryMappingDto } from "../../../types/settlements";
import {
  SettlementLineActions
} from "./SettlementLineActions";
import {
  SettlementLineFields,
  type SettlementLineFormErrors,
  type SettlementLineFormValues
} from "./SettlementLineFields";
export type { SettlementLineFormErrors, SettlementLineFormValues } from "./SettlementLineFields";

type SettlementLineFormProps = {
  mode: "add" | "edit";
  values: SettlementLineFormValues;
  errors: SettlementLineFormErrors;
  apiError: string | null;
  isSaving: boolean;
  showMileageFields: boolean;
  categories: CategoryMappingDto[];
  categoriesLoading: boolean;
  onChange: <K extends keyof SettlementLineFormValues>(field: K, value: SettlementLineFormValues[K]) => void;
  onSubmit: () => Promise<void>;
  onCancel: () => void;
};

export function SettlementLineForm({
  mode,
  values,
  errors,
  apiError,
  isSaving,
  showMileageFields,
  categories,
  categoriesLoading,
  onChange,
  onSubmit,
  onCancel
}: SettlementLineFormProps): JSX.Element {
  return (
    <form
      className="settlement-line-form"
      onSubmit={(event) => {
        event.preventDefault();
        void onSubmit();
      }}
      noValidate
    >
      <h4>{mode === "edit" ? "Edit Line" : "Add Line"}</h4>
      {apiError ? <div className="settlement-form-api-error">{apiError}</div> : null}
      <SettlementLineFields
        values={values}
        errors={errors}
        isSaving={isSaving}
        showMileageFields={showMileageFields}
        categories={categories}
        categoriesLoading={categoriesLoading}
        onChange={onChange}
      />
      <SettlementLineActions mode={mode} isSaving={isSaving} onCancel={onCancel} />
    </form>
  );
}

