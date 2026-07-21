import { useCallback, useRef, useState } from "react";
import { getApiErrorMessage } from "../../../api/apiErrorMessage";
import { settlementsClient } from "../../../api/settlementsClient";
import type { SettlementDto } from "../../../types/settlements";

type UseRemoveSettlementLineResult = {
  isRemoving: boolean;
  apiError: string | null;
  removeLine: (settlementId: string, lineId: string) => Promise<SettlementDto | null>;
  clearApiError: () => void;
};


export function useRemoveSettlementLine(): UseRemoveSettlementLineResult {
  const [isRemoving, setRemoving] = useState(false);
  const [apiError, setApiError] = useState<string | null>(null);
  const removingRef = useRef(false);

  const removeLine = useCallback(async (settlementId: string, lineId: string) => {
    if (removingRef.current) {
      return null;
    }

    removingRef.current = true;
    setRemoving(true);
    setApiError(null);
    try {
      return await settlementsClient.removeLine(settlementId, lineId);
    } catch (error) {
      setApiError(getApiErrorMessage(error, "Unable to remove settlement line."));
      return null;
    } finally {
      removingRef.current = false;
      setRemoving(false);
    }
  }, []);

  const clearApiError = useCallback(() => setApiError(null), []);

  return {
    isRemoving,
    apiError,
    removeLine,
    clearApiError
  };
}

