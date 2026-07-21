import { useCallback, useEffect, useRef, useState } from "react";
import axios from "axios";
import { settlementsClient } from "../../../api/settlementsClient";
import type { SettlementDto } from "../../../types/settlements";

type UseSettlementResult = {
  settlement: SettlementDto | null;
  isLoading: boolean;
  isNotFound: boolean;
  error: string | null;
  refresh: () => Promise<void>;
  replaceSettlement: (value: SettlementDto) => void;
};

export function useSettlement(requestId: string | undefined): UseSettlementResult {
  const [settlement, setSettlement] = useState<SettlementDto | null>(null);
  const [isLoading, setLoading] = useState(Boolean(requestId));
  const [isNotFound, setNotFound] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const isMountedRef = useRef(true);

  const loadSettlement = useCallback(async (signal?: AbortSignal) => {
    if (!requestId) {
      setSettlement(null);
      setNotFound(true);
      setError(null);
      setLoading(false);
      return;
    }

    setLoading(true);
    setNotFound(false);
    setError(null);
    setSettlement(null);
    try {
      const dto = await settlementsClient.getById(requestId, { signal });
      if (!isMountedRef.current) {
        return;
      }
      setSettlement(dto);
    } catch (err) {
      if (axios.isAxiosError(err) && err.code === "ERR_CANCELED") {
        return;
      }
      if (!isMountedRef.current) {
        return;
      }
      setSettlement(null);
      if (axios.isAxiosError(err) && err.response?.status === 404) {
        setNotFound(true);
        return;
      }

      const message = err instanceof Error ? err.message : "Unexpected error while loading settlement.";
      setError(message);
    } finally {
      if (!isMountedRef.current) {
        return;
      }
      setLoading(false);
    }
  }, [requestId]);
  const refresh = useCallback(async () => {
    await loadSettlement();
  }, [loadSettlement]);

  useEffect(() => {
    isMountedRef.current = true;
    const controller = new AbortController();
    void loadSettlement(controller.signal);

    return () => {
      isMountedRef.current = false;
      controller.abort();
    };
  }, [loadSettlement]);

  const replaceSettlement = useCallback((value: SettlementDto) => {
    setSettlement(value);
    setNotFound(false);
    setError(null);
  }, []);

  return {
    settlement,
    isLoading,
    isNotFound,
    error,
    refresh,
    replaceSettlement
  };
}

