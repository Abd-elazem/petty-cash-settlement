import { useCallback, useRef, useState } from "react";
import { getApiErrorMessage } from "../../../api/apiErrorMessage";
import { settlementsClient } from "../../../api/settlementsClient";
import type { SettlementDto } from "../../../types/settlements";

type UseSubmitSettlementResult = {
  isSubmitting: boolean;
  apiError: string | null;
  submitSettlement: (settlementId: string) => Promise<SettlementDto | null>;
  clearApiError: () => void;
};


export function useSubmitSettlement(): UseSubmitSettlementResult {
  const [isSubmitting, setSubmitting] = useState(false);
  const [apiError, setApiError] = useState<string | null>(null);
  const submittingRef = useRef(false);

  const submitSettlement = useCallback(async (settlementId: string) => {
    if (submittingRef.current) {
      return null;
    }

    submittingRef.current = true;
    setSubmitting(true);
    setApiError(null);
    try {
      return await settlementsClient.submit(settlementId);
    } catch (error) {
      setApiError(getApiErrorMessage(error, "Unable to submit settlement."));
      return null;
    } finally {
      submittingRef.current = false;
      setSubmitting(false);
    }
  }, []);

  const clearApiError = useCallback(() => setApiError(null), []);

  return {
    isSubmitting,
    apiError,
    submitSettlement,
    clearApiError
  };
}
