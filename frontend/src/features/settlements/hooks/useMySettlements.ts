import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import axios from "axios";
import { settlementsClient } from "../../../api/settlementsClient";
import type { SettlementSummaryDto } from "../../../types/settlements";
import { normalizeStatus, parseSettlementDate, settlementSearchMatch } from "../utils";

type UseMySettlementsOptions = {
  searchQuery: string;
  statusFilter: string;
};

type UseMySettlementsResult = {
  settlements: SettlementSummaryDto[];
  availableStatuses: string[];
  isLoading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
};

function sortNewestFirst(values: SettlementSummaryDto[]): SettlementSummaryDto[] {
  return [...values].sort((left, right) => parseSettlementDate(right.settlementDate) - parseSettlementDate(left.settlementDate));
}

export function useMySettlements({
  searchQuery,
  statusFilter
}: UseMySettlementsOptions): UseMySettlementsResult {
  const [allSettlements, setAllSettlements] = useState<SettlementSummaryDto[]>([]);
  const [isLoading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const isMountedRef = useRef(true);

  const loadSettlements = useCallback(async (signal?: AbortSignal) => {
    setLoading(true);
    setError(null);
    try {
      const response = await settlementsClient.getMine({ signal });
      if (!isMountedRef.current) {
        return;
      }
      setAllSettlements(sortNewestFirst(response));
    } catch (err) {
      if (axios.isAxiosError(err) && err.code === "ERR_CANCELED") {
        return;
      }
      const message = err instanceof Error ? err.message : "Unexpected error while loading settlements.";
      if (!isMountedRef.current) {
        return;
      }
      setError(message);
    } finally {
      if (!isMountedRef.current) {
        return;
      }
      setLoading(false);
    }
  }, []);
  const refresh = useCallback(async () => {
    await loadSettlements();
  }, [loadSettlements]);

  useEffect(() => {
    isMountedRef.current = true;
    const controller = new AbortController();
    void loadSettlements(controller.signal);

    return () => {
      isMountedRef.current = false;
      controller.abort();
    };
  }, [loadSettlements]);

  const availableStatuses = useMemo(() => {
    const unique = new Set(allSettlements.map((item) => item.status));
    return ["all", ...Array.from(unique)];
  }, [allSettlements]);

  const settlements = useMemo(() => {
    const filterValue = normalizeStatus(statusFilter);

    return allSettlements
      .filter((settlement) => settlementSearchMatch(settlement, searchQuery))
      .filter((settlement) => {
        if (filterValue === "all") {
          return true;
        }
        return normalizeStatus(settlement.status) === filterValue;
      });
  }, [allSettlements, searchQuery, statusFilter]);

  return {
    settlements,
    availableStatuses,
    isLoading,
    error,
    refresh
  };
}

