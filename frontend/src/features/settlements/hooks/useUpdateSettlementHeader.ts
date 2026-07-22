import { useCallback, useRef, useState } from "react";
import { getApiErrorMessage } from "../../../api/apiErrorMessage";
import { settlementsClient } from "../../../api/settlementsClient";
import type { SettlementDto, UpdateSettlementHeaderRequest } from "../../../types/settlements";

type UseUpdateSettlementHeaderResult = {
  isSaving: boolean;
  apiError: string | null;
  updateHeader: (
    settlementId: string,
    payload: UpdateSettlementHeaderRequest
  ) => Promise<SettlementDto | null>;
  clearApiError: () => void;
};

export function useUpdateSettlementHeader(): UseUpdateSettlementHeaderResult {
  const [isSaving, setSaving] = useState(false);
  const [apiError, setApiError] = useState<string | null>(null);
  const savingRef = useRef(false);

  const updateHeader = useCallback(
    async (settlementId: string, payload: UpdateSettlementHeaderRequest) => {
      if (savingRef.current) {
        return null;
      }

      savingRef.current = true;
      setSaving(true);
      setApiError(null);
      try {
        return await settlementsClient.updateHeader(settlementId, payload);
      } catch (error) {
        setApiError(getApiErrorMessage(error, "Unable to update settlement header."));
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
    updateHeader,
    clearApiError
  };
}
