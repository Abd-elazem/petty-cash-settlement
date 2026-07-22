import { useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { GlobalLoading } from "../components/GlobalLoading";
import { DeleteConfirmationDialog } from "../features/settlements/components/DeleteConfirmationDialog";
import { EmptyState } from "../features/settlements/components/EmptyState";
import { ErrorState } from "../features/settlements/components/ErrorState";
import { SettlementHeader } from "../features/settlements/components/SettlementHeader";
import { SettlementLineCard } from "../features/settlements/components/SettlementLineCard";
import {
  SettlementLineForm,
  type SettlementLineFormErrors,
  type SettlementLineFormValues
} from "../features/settlements/components/SettlementLineForm";
import { SettlementLineTable } from "../features/settlements/components/SettlementLineTable";
import { SettlementMetadata } from "../features/settlements/components/SettlementMetadata";
import { SubmitSettlementDialog } from "../features/settlements/components/SubmitSettlementDialog";
import {
  EditSettlementHeaderForm,
  type EditHeaderFormValues
} from "../features/settlements/components/EditSettlementHeaderForm";
import { useAddSettlementLine } from "../features/settlements/hooks/useAddSettlementLine";
import { useCategoryMappings } from "../features/settlements/hooks/useCategoryMappings";
import { useRemoveSettlementLine } from "../features/settlements/hooks/useRemoveSettlementLine";
import { useSettlement } from "../features/settlements/hooks/useSettlement";
import { useSubmitSettlement } from "../features/settlements/hooks/useSubmitSettlement";
import { useUpdateSettlementHeader } from "../features/settlements/hooks/useUpdateSettlementHeader";
import { useUpdateSettlementLine } from "../features/settlements/hooks/useUpdateSettlementLine";
import type { AddSettlementLineRequest, SettlementLineDto, UpdateSettlementLineRequest } from "../types/settlements";

function validateLine(values: SettlementLineFormValues, showMileageFields: boolean): SettlementLineFormErrors {
  const errors: SettlementLineFormErrors = {};
  if (!values.categoryCode.trim()) {
    errors.categoryCode = "Category is required.";
  }

  const grossAmount = Number(values.grossAmount);
  if (!values.grossAmount.trim() || Number.isNaN(grossAmount)) {
    errors.grossAmount = "Gross amount is required.";
  } else if (grossAmount <= 0) {
    errors.grossAmount = "Gross amount must be greater than zero.";
  }

  if (values.notes.length > 1000) {
    errors.notes = "Notes must be 1000 characters or fewer.";
  }

  if (showMileageFields) {
    if (!values.carPlate.trim()) {
      errors.carPlate = "Car plate is required for this category.";
    }

    if (!values.odometerKm.trim()) {
      errors.odometerKm = "Odometer KM is required for this category.";
    } else {
      const odometer = Number(values.odometerKm);
      if (Number.isNaN(odometer)) {
        errors.odometerKm = "Odometer KM must be a valid number.";
      } else if (odometer < 0) {
        errors.odometerKm = "Odometer KM must be zero or greater.";
      }
    }
  }

  return errors;
}

export function SettlementDetailPage(): JSX.Element {
  const { requestId } = useParams<{ requestId: string }>();
  const navigate = useNavigate();
  const { settlement, isLoading, isNotFound, error, refresh, replaceSettlement } = useSettlement(requestId);
  const {
    categories,
    isLoading: categoriesLoading,
    error: categoriesError,
    retry: retryCategories
  } = useCategoryMappings();
  const { addLine, isSaving: isAddingLine, apiError: addApiError, clearApiError: clearAddApiError } = useAddSettlementLine();
  const {
    updateLine,
    isSaving: isUpdatingLine,
    apiError: updateApiError,
    clearApiError: clearUpdateApiError
  } = useUpdateSettlementLine();
  const {
    updateHeader,
    isSaving: isUpdatingHeader,
    apiError: headerApiError,
    clearApiError: clearHeaderApiError
  } = useUpdateSettlementHeader();
  const {
    removeLine,
    isRemoving,
    apiError: removeApiError,
    clearApiError: clearRemoveApiError
  } = useRemoveSettlementLine();
  const {
    submitSettlement,
    isSubmitting,
    apiError: submitApiError,
    clearApiError: clearSubmitApiError
  } = useSubmitSettlement();
  const [isEditingHeader, setEditingHeader] = useState(false);
  const [lineEditorMode, setLineEditorMode] = useState<"add" | "edit" | null>(null);
  const [editingLineId, setEditingLineId] = useState<string | null>(null);
  const [deleteTargetLine, setDeleteTargetLine] = useState<SettlementLineDto | null>(null);
  const [isSubmitDialogOpen, setSubmitDialogOpen] = useState(false);
  const [submitClientError, setSubmitClientError] = useState<string | null>(null);
  const [lineValues, setLineValues] = useState<SettlementLineFormValues>({
    categoryCode: "",
    grossAmount: "",
    isVat: false,
    notes: "",
    carPlate: "",
    odometerKm: ""
  });
  const [lineErrors, setLineErrors] = useState<SettlementLineFormErrors>({});
  const isDraft = settlement?.status === "Draft";
  const canSubmit = Boolean(isDraft && settlement && settlement.lines.length > 0);
  const lineOperationPending = isAddingLine || isUpdatingLine || isUpdatingHeader || isRemoving || isSubmitting;
  const submitOperationPending = lineOperationPending || isSubmitDialogOpen;
  const activeEditorApiError = lineEditorMode === "edit" ? updateApiError : addApiError;
  const activeSubmitError = submitClientError ?? submitApiError;
  // Derive kmRequired from backend category metadata — no hardcoded category codes.
  // Falls back to false while categories are still loading (mileage fields simply stay
  // hidden until the list arrives; the user can't select a category yet anyway).
  const showMileageFields = useMemo(() => {
    if (!lineValues.categoryCode || categoriesLoading) return false;
    const selected = categories.find((c) => c.categoryCode === lineValues.categoryCode);
    return selected?.kmRequired ?? false;
  }, [lineValues.categoryCode, categories, categoriesLoading]);

  const handleLineChange = <K extends keyof SettlementLineFormValues>(
    field: K,
    value: SettlementLineFormValues[K]
  ): void => {
    setLineValues((previous) => ({ ...previous, [field]: value }));
    if (lineErrors[field]) {
      setLineErrors((previous) => {
        const next = { ...previous };
        delete next[field];
        return next;
      });
    }
    if (lineEditorMode === "edit" && updateApiError) {
      clearUpdateApiError();
    }
    if (lineEditorMode !== "edit" && addApiError) {
      clearAddApiError();
    }
  };

  const openSubmitDialog = (): void => {
    if (!canSubmit || lineOperationPending) {
      return;
    }
    setSubmitClientError(null);
    clearSubmitApiError();
    setSubmitDialogOpen(true);
  };

  const handleSubmitSettlement = async (): Promise<void> => {
    if (!requestId || lineOperationPending) {
      return;
    }
    if (!settlement || settlement.lines.length === 0) {
      setSubmitClientError("Add at least one settlement line before submitting.");
      return;
    }

    setSubmitClientError(null);
    const updated = await submitSettlement(requestId);
    if (!updated) {
      return;
    }

    replaceSettlement(updated);
    setSubmitDialogOpen(false);
    setDeleteTargetLine(null);
    closeLineEditor();
    clearRemoveApiError();
  };

  const closeLineEditor = (): void => {
    setLineEditorMode(null);
    setEditingLineId(null);
    setLineErrors({});
    clearAddApiError();
    clearUpdateApiError();
  };

  const handleSaveHeader = async (values: EditHeaderFormValues): Promise<void> => {
    if (!requestId || !settlement) {
      return;
    }
    const updated = await updateHeader(requestId, {
      settlementDate: values.settlementDate,
      purpose: values.purpose.trim()
    });
    if (updated) {
      replaceSettlement(updated);
      setEditingHeader(false);
      clearHeaderApiError();
    }
  };

  const resetLineForm = (): void => {
    setLineValues({
      categoryCode: "",
      grossAmount: "",
      isVat: false,
      notes: "",
      carPlate: "",
      odometerKm: ""
    });
    setLineErrors({});
  };

  const startAddLine = (): void => {
    if (submitOperationPending) {
      return;
    }
    resetLineForm();
    setEditingLineId(null);
    setLineEditorMode("add");
    clearAddApiError();
    clearUpdateApiError();
  };

  const startEditLine = (lineId: string): void => {
    if (!settlement || submitOperationPending) {
      return;
    }
    const line = settlement.lines.find((item) => item.lineId === lineId);
    if (!line) {
      return;
    }

    setLineValues({
      categoryCode: line.categoryCode,
      grossAmount: String(line.grossAmount),
      isVat: line.isVat,
      notes: line.notes ?? "",
      carPlate: line.carPlate ?? "",
      odometerKm: line.odometerKm === null ? "" : String(line.odometerKm)
    });
    setLineErrors({});
    setEditingLineId(line.lineId);
    setLineEditorMode("edit");
    clearAddApiError();
    clearUpdateApiError();
  };

  const requestDeleteLine = (lineId: string): void => {
    if (!settlement || submitOperationPending) {
      return;
    }
    const line = settlement.lines.find((item) => item.lineId === lineId);
    if (!line) {
      return;
    }
    setDeleteTargetLine(line);
    clearRemoveApiError();
  };

  const handleSaveLine = async (): Promise<void> => {
    if (!settlement || !requestId) {
      return;
    }

    const validationErrors = validateLine(lineValues, showMileageFields);
    setLineErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0 || lineOperationPending) {
      return;
    }

    const payload = {
      categoryCode: lineValues.categoryCode.trim(),
      grossAmount: Number(lineValues.grossAmount),
      isVat: lineValues.isVat,
      notes: lineValues.notes.trim().length > 0 ? lineValues.notes.trim() : null,
      carPlate: showMileageFields ? lineValues.carPlate.trim() : null,
      odometerKm: showMileageFields ? Number(lineValues.odometerKm) : null
    };

    let updated = null;
    if (lineEditorMode === "edit" && editingLineId) {
      updated = await updateLine(requestId, editingLineId, payload satisfies UpdateSettlementLineRequest);
    } else {
      updated = await addLine(requestId, payload satisfies AddSettlementLineRequest);
    }

    if (updated) {
      replaceSettlement(updated);
      resetLineForm();
      closeLineEditor();
    }
  };

  const handleRemoveLine = async (): Promise<void> => {
    if (!requestId || !deleteTargetLine || lineOperationPending) {
      return;
    }

    const updated = await removeLine(requestId, deleteTargetLine.lineId);
    if (updated) {
      replaceSettlement(updated);
      setDeleteTargetLine(null);
      clearRemoveApiError();
    }
  };

  return (
    <div className="page-container">
      <button type="button" className="button-link settlement-back-link" onClick={() => navigate("/my-settlements")}>
        ← Back to My Settlements
      </button>

      {isLoading ? <GlobalLoading message="Loading settlement detail..." /> : null}

      {!isLoading && isNotFound ? (
        <EmptyState
          title="Settlement not found"
          description={`No settlement was found for request ID ${requestId ?? "-"}.`}
        />
      ) : null}

      {!isLoading && error ? <ErrorState message={error} onRetry={() => void refresh()} /> : null}

      {!isLoading && !isNotFound && !error && settlement ? (
        <div className="settlement-detail-layout">
          <SettlementHeader
            requestId={settlement.requestId}
            status={settlement.status}
            purpose={settlement.purpose}
            settlementDate={settlement.settlementDate}
            totalAmount={settlement.totalAmount}
            version={settlement.version}
          />

          {isDraft && !isEditingHeader ? (
            <div className="settlement-header-edit-action">
              <button
                type="button"
                className="button-secondary"
                onClick={() => {
                  clearHeaderApiError();
                  setEditingHeader(true);
                }}
                disabled={lineOperationPending}
              >
                Edit Header
              </button>
            </div>
          ) : null}

          {isDraft && isEditingHeader ? (
            <EditSettlementHeaderForm
              initialValues={{
                settlementDate: settlement.settlementDate,
                purpose: settlement.purpose
              }}
              isSaving={isUpdatingHeader}
              apiError={headerApiError}
              onSave={handleSaveHeader}
              onCancel={() => {
                if (!isUpdatingHeader) {
                  setEditingHeader(false);
                  clearHeaderApiError();
                }
              }}
            />
          ) : null}

          <SettlementMetadata
            approverEmail={settlement.approverEmail}
            spenderName={settlement.spenderName}
            workerId={settlement.workerId}
            approvalComment={settlement.approvalComment}
            journalBatchNumber={settlement.journalBatchNumber}
          />

          <div className="card">
            <div className="settlement-lines-header">
              <h3>Settlement Lines</h3>
              <div className="settlement-lines-actions">
                {isDraft ? (
                  <button
                    type="button"
                    className="button-primary"
                    onClick={() =>
                      lineEditorMode === "add" && !editingLineId ? closeLineEditor() : startAddLine()
                    }
                    disabled={lineOperationPending}
                  >
                    {lineEditorMode === "add" && !editingLineId ? "Close Add Line" : "Add Line"}
                  </button>
                ) : null}
                {canSubmit ? (
                  <button
                    type="button"
                    className="button-primary"
                    onClick={openSubmitDialog}
                    disabled={lineOperationPending}
                  >
                    Submit Settlement
                  </button>
                ) : null}
              </div>
            </div>

            {/* Category load error — shown inline above the line form so the user
                 can retry without leaving the page. The form is kept hidden while
                 categories haven't loaded yet (nothing to select in the dropdown). */}
            {categoriesError ? (
              <div className="settlement-form-api-error">
                Failed to load categories.
                {" "}
                <button type="button" className="button-link" onClick={retryCategories}>
                  Retry
                </button>
              </div>
            ) : null}

            {lineEditorMode && isDraft ? (
              <SettlementLineForm
                mode={lineEditorMode}
                values={lineValues}
                errors={lineErrors}
                apiError={activeEditorApiError}
                isSaving={isAddingLine || isUpdatingLine}
                showMileageFields={showMileageFields}
                categories={categories}
                categoriesLoading={categoriesLoading}
                onChange={handleLineChange}
                onSubmit={handleSaveLine}
                onCancel={closeLineEditor}
              />
            ) : null}

            {settlement.lines.length === 0 ? (
              <p>No lines available for this settlement.</p>
            ) : (
              <>
                <div className="settlement-lines-desktop">
                  <SettlementLineTable
                    lines={settlement.lines}
                    editable={isDraft}
                    isOperationPending={lineOperationPending}
                    onEdit={startEditLine}
                    onDelete={requestDeleteLine}
                  />
                </div>
                <div className="settlement-lines-mobile">
                  {settlement.lines.map((line) => (
                    <SettlementLineCard
                      key={line.lineId}
                      line={line}
                      editable={isDraft}
                      isOperationPending={lineOperationPending}
                      onEdit={startEditLine}
                      onDelete={requestDeleteLine}
                    />
                  ))}
                </div>
              </>
            )}
          </div>
        </div>
      ) : null}

      <DeleteConfirmationDialog
        title="Delete settlement line"
        message={
          deleteTargetLine
            ? `Delete line #${deleteTargetLine.lineNo} (${deleteTargetLine.categoryCode})? This action cannot be undone.`
            : ""
        }
        isOpen={Boolean(deleteTargetLine)}
        isDeleting={isRemoving}
        error={removeApiError}
        onConfirm={handleRemoveLine}
        onCancel={() => {
          if (!isRemoving) {
            setDeleteTargetLine(null);
            clearRemoveApiError();
          }
        }}
      />
      <SubmitSettlementDialog
        isOpen={isSubmitDialogOpen}
        isSubmitting={isSubmitting}
        error={activeSubmitError}
        onConfirm={handleSubmitSettlement}
        onCancel={() => {
          if (!isSubmitting) {
            setSubmitDialogOpen(false);
            setSubmitClientError(null);
            clearSubmitApiError();
          }
        }}
      />
    </div>
  );
}

