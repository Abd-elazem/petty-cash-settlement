import { useCallback, useRef, useState } from "react";
import { getApiErrorMessage } from "../../../api/apiErrorMessage";
import { settlementsClient } from "../../../api/settlementsClient";
import type { CreateSettlementRequest, SettlementDto } from "../../../types/settlements";

type UseCreateSettlementResult = {
  isSaving: boolean;
  apiError: string | null;
  createSettlement: (payload: CreateSettlementRequest) => Promise<SettlementDto | null>;
  clearApiError: () => void;
};


export function useCreateSettlement(): UseCreateSettlementResult {
  const [isSaving, setSaving] = useState(false);
  const [apiError, setApiError] = useState<string | null>(null);
  const savingRef = useRef(false);

  const createSettlement = useCallback(async (payload: CreateSettlementRequest) => {
    if (savingRef.current) {
      return null;
    }
    savingRef.current = true;

    setSaving(true);
    setApiError(null);
    try {
      return await settlementsClient.createDraft(payload);
    } catch (error) {
      setApiError(getApiErrorMessage(error, "Unable to create settlement."));
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
    createSettlement,
    clearApiError
  };
}

