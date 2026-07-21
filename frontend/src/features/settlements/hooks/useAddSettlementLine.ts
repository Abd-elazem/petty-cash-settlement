import { useCallback, useRef, useState } from "react";
import { getApiErrorMessage } from "../../../api/apiErrorMessage";
import { settlementsClient } from "../../../api/settlementsClient";
import type { AddSettlementLineRequest, SettlementDto } from "../../../types/settlements";

type UseAddSettlementLineResult = {
  isSaving: boolean;
  apiError: string | null;
  addLine: (settlementId: string, payload: AddSettlementLineRequest) => Promise<SettlementDto | null>;
  clearApiError: () => void;
};


export function useAddSettlementLine(): UseAddSettlementLineResult {
  const [isSaving, setSaving] = useState(false);
  const [apiError, setApiError] = useState<string | null>(null);
  const savingRef = useRef(false);

  const addLine = useCallback(async (settlementId: string, payload: AddSettlementLineRequest) => {
    if (savingRef.current) {
      return null;
    }

    savingRef.current = true;
    setSaving(true);
    setApiError(null);

    try {
      return await settlementsClient.addLine(settlementId, payload);
    } catch (error) {
      setApiError(getApiErrorMessage(error, "Unable to add settlement line."));
      return null;
    } finally {
      savingRef.current = false;
      setSaving(false);
    }
  }, []);

  const clearApiError = useCallback(() => setApiError(null), []);

  return {
    isSaving,
    apiError,
    addLine,
    clearApiError
  };
}

