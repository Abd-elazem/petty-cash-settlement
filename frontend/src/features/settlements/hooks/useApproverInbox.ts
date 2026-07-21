import { useCallback, useEffect, useRef, useState } from "react";
import axios from "axios";
import { settlementsClient } from "../../../api/settlementsClient";
import type { SettlementDto } from "../../../types/settlements";

type UseApproverInboxResult = {
  settlements: SettlementDto[];
  isLoading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
};

export function useApproverInbox(): UseApproverInboxResult {
  const [settlements, setSettlements] = useState<SettlementDto[]>([]);
  const [isLoading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const isMountedRef = useRef(true);

  const load = useCallback(async (signal?: AbortSignal) => {
    setLoading(true);
    setError(null);
    try {
      const data = await settlementsClient.getInbox({ signal });
      if (!isMountedRef.current) return;
      setSettlements(data);
    } catch (err) {
      if (axios.isAxiosError(err) && err.code === "ERR_CANCELED") return;
      if (!isMountedRef.current) return;
      const message = err instanceof Error ? err.message : "Unexpected error loading inbox.";
      setError(message);
    } finally {
      if (isMountedRef.current) setLoading(false);
    }
  }, []);

  const refresh = useCallback(async () => { await load(); }, [load]);

  useEffect(() => {
    isMountedRef.current = true;
    const controller = new AbortController();
    void load(controller.signal);
    return () => {
      isMountedRef.current = false;
      controller.abort();
    };
  }, [load]);

  return { settlements, isLoading, error, refresh };
}
