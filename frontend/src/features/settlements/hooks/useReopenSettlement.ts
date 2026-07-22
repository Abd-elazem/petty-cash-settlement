import { useCallback, useRef, useState } from "react";
import { getApiErrorMessage } from "../../../api/apiErrorMessage";
import { settlementsClient } from "../../../api/settlementsClient";
import type { SettlementDto } from "../../../types/settlements";

type UseReopenSettlementResult = {
  isReopening: boolean;
  apiError: string | null;
  reopenSettlement: (settlementId: string) => Promise<SettlementDto | null>;
  clearApiError: () => void;
};

export function useReopenSettlement(): UseReopenSettlementResult {
  const [isReopening, setReopening] = useState(false);
  const [apiError, setApiError] = useState<string | null>(null);
  const reopeningRef = useRef(false);

  const reopenSettlement = useCallback(async (settlementId: string) => {
    if (reopeningRef.current) {
      return null;
    }

    reopeningRef.current = true;
    setReopening(true);
    setApiError(null);
    try {
      return await settlementsClient.reopen(settlementId);
    } catch (error) {
      setApiError(getApiErrorMessage(error, "Unable to reopen settlement."));
      return null;
    } finally {
      reopeningRef.current = false;
      setReopening(false);
    }
  }, []);

  const clearApiError = useCallback(() => setApiError(null), []);

  return {
    isReopening,
    apiError,
    reopenSettlement,
    clearApiError
  };
}
