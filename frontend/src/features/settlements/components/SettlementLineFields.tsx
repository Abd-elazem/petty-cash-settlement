import type { CategoryMappingDto } from "../../../types/settlements";
import { FormField } from "./FormField";

export type SettlementLineFormValues = {
  categoryCode: string;
  grossAmount: string;
  isVat: boolean;
  notes: string;
  carPlate: string;
  odometerKm: string;
};

export type SettlementLineFormErrors = Partial<Record<keyof SettlementLineFormValues, string>>;

type SettlementLineFieldsProps = {
  values: SettlementLineFormValues;
  errors: SettlementLineFormErrors;
  isSaving: boolean;
  /** Backend-driven flag: show Car Plate + Odometer fields for the currently selected category. */
  showMileageFields: boolean;
  /** Active category mappings loaded from GET /api/v1/category-mappings. */
  categories: CategoryMappingDto[];
  /** True while categories are still loading from the API (shows a disabled placeholder). */
  categoriesLoading: boolean;
  onChange: <K extends keyof SettlementLineFormValues>(field: K, value: SettlementLineFormValues[K]) => void;
};

export function SettlementLineFields({
  values,
  errors,
  isSaving,
  showMileageFields,
  categories,
  categoriesLoading,
  onChange
}: SettlementLineFieldsProps): JSX.Element {
  return (
    <>
      <FormField htmlFor="line-category" label="Category" error={errors.categoryCode}>
        <select
          id="line-category"
          value={values.categoryCode}
          onChange={(event) => onChange("categoryCode", event.target.value)}
          disabled={isSaving || categoriesLoading}
          aria-busy={categoriesLoading}
        >
          {/* Placeholder option — visible when no category is selected yet */}
          <option value="">
            {categoriesLoading ? "Loading categories…" : "Select a category"}
          </option>
          {categories.map((cat) => (
            <option key={cat.categoryCode} value={cat.categoryCode}>
              {cat.displayName}
            </option>
          ))}
        </select>
      </FormField>

      <FormField htmlFor="line-gross-amount" label="Gross Amount" error={errors.grossAmount}>
        <input
          id="line-gross-amount"
          type="number"
          min="0.01"
          step="0.01"
          value={values.grossAmount}
          onChange={(event) => onChange("grossAmount", event.target.value)}
          disabled={isSaving}
        />
      </FormField>

      <label className="settlement-line-vat">
        <input
          type="checkbox"
          checked={values.isVat}
          onChange={(event) => onChange("isVat", event.target.checked)}
          disabled={isSaving}
        />
        <span>Apply VAT</span>
      </label>

      <FormField htmlFor="line-notes" label="Notes" error={errors.notes}>
        <textarea
          id="line-notes"
          rows={3}
          maxLength={1000}
          value={values.notes}
          onChange={(event) => onChange("notes", event.target.value)}
          disabled={isSaving}
        />
      </FormField>

      {showMileageFields ? (
        <div className="settlement-line-mileage-grid">
          <FormField htmlFor="line-car-plate" label="Car Plate" error={errors.carPlate}>
            <input
              id="line-car-plate"
              value={values.carPlate}
              onChange={(event) => onChange("carPlate", event.target.value)}
              disabled={isSaving}
            />
          </FormField>

          <FormField htmlFor="line-odometer-km" label="Odometer KM" error={errors.odometerKm}>
            <input
              id="line-odometer-km"
              type="number"
              min="0"
              step="1"
              value={values.odometerKm}
              onChange={(event) => onChange("odometerKm", event.target.value)}
              disabled={isSaving}
            />
          </FormField>
        </div>
      ) : null}
    </>
  );
}
