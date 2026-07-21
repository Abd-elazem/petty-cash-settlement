import { useCallback, useRef, useState } from "react";
import { getApiErrorMessage } from "../../../api/apiErrorMessage";
import { settlementsClient } from "../../../api/settlementsClient";
import type { SettlementDto, UpdateSettlementLineRequest } from "../../../types/settlements";

type UseUpdateSettlementLineResult = {
  isSaving: boolean;
  apiError: string | null;
  updateLine: (
    settlementId: string,
    lineId: string,
    payload: UpdateSettlementLineRequest
  ) => Promise<SettlementDto | null>;
  clearApiError: () => void;
};


export function useUpdateSettlementLine(): UseUpdateSettlementLineResult {
  const [isSaving, setSaving] = useState(false);
  const [apiError, setApiError] = useState<string | null>(null);
  const savingRef = useRef(false);

  const updateLine = useCallback(
    async (settlementId: string, lineId: string, payload: UpdateSettlementLineRequest) => {
      if (savingRef.current) {
        return null;
      }

      savingRef.current = true;
      setSaving(true);
      setApiError(null);
      try {
        return await settlementsClient.updateLine(settlementId, lineId, payload);
      } catch (error) {
        setApiError(getApiErrorMessage(error, "Unable to update settlement line."));
        return null;
      } finally {
        savingRef.current = false;
        setSaving(false);
      }
    },
    []
  );

  const clearApiError = useCallback(() => setApiError(null), []);

  return {
    isSaving,
    apiError,
    updateLine,
    clearApiError
  };
}

