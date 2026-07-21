import { useCallback, useRef, useState } from "react";
import { settlementsClient } from "../../../api/settlementsClient";
import { getApiErrorMessage } from "../../../api/apiErrorMessage";
import type { SettlementDto } from "../../../types/settlements";

type UseApproveSettlementResult = {
  approveSettlement: (settlementId: string) => Promise<SettlementDto | null>;
  isApproving: boolean;
  apiError: string | null;
  clearApiError: () => void;
};

export function useApproveSettlement(): UseApproveSettlementResult {
  const [isApproving, setApproving] = useState(false);
  const [apiError, setApiError] = useState<string | null>(null);
  const approvingRef = useRef(false);

  const clearApiError = useCallback(() => setApiError(null), []);

  const approveSettlement = useCallback(async (settlementId: string): Promise<SettlementDto | null> => {
    if (approvingRef.current) return null;
    approvingRef.current = true;
    setApproving(true);
    setApiError(null);
    try {
      return await settlementsClient.approve(settlementId);
    } catch (err) {
      setApiError(getApiErrorMessage(err, "Failed to approve settlement."));
      return null;
    } finally {
      approvingRef.current = false;
      setApproving(false);
    }
  }, []);

  return { approveSettlement, isApproving, apiError, clearApiError };
}
