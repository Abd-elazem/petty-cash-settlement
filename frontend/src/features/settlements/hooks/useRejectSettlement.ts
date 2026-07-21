import { useCallback, useRef, useState } from "react";
import { settlementsClient } from "../../../api/settlementsClient";
import { getApiErrorMessage } from "../../../api/apiErrorMessage";
import type { SettlementDto } from "../../../types/settlements";

type UseRejectSettlementResult = {
  rejectSettlement: (settlementId: string, comment: string) => Promise<SettlementDto | null>;
  isRejecting: boolean;
  apiError: string | null;
  clearApiError: () => void;
};

export function useRejectSettlement(): UseRejectSettlementResult {
  const [isRejecting, setRejecting] = useState(false);
  const [apiError, setApiError] = useState<string | null>(null);
  const rejectingRef = useRef(false);

  const clearApiError = useCallback(() => setApiError(null), []);

  const rejectSettlement = useCallback(async (
    settlementId: string,
    comment: string
  ): Promise<SettlementDto | null> => {
    if (rejectingRef.current) return null;
    rejectingRef.current = true;
    setRejecting(true);
    setApiError(null);
    try {
      return await settlementsClient.reject(settlementId, comment);
    } catch (err) {
      setApiError(getApiErrorMessage(err, "Failed to reject settlement."));
      return null;
    } finally {
      rejectingRef.current = false;
      setRejecting(false);
    }
  }, []);

  return { rejectSettlement, isRejecting, apiError, clearApiError };
}
