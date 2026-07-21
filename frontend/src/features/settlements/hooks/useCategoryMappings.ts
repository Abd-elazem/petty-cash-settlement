import { useCallback, useEffect, useRef, useState } from "react";
import axios from "axios";
import { categoriesClient } from "../../../api/categoriesClient";
import type { CategoryMappingDto } from "../../../types/settlements";

type UseCategoryMappingsResult = {
  categories: CategoryMappingDto[];
  isLoading: boolean;
  error: string | null;
  retry: () => void;
};

/**
 * Loads the active category mappings from GET /api/v1/category-mappings on mount.
 * Provides loading / error / retry states so the form can show feedback while the
 * list is in flight and allow the user to recover from a transient failure.
 *
 * The hook uses the same abort-signal + isMounted guard pattern as useMySettlements
 * and useSettlement, so navigating away before the request completes does not
 * trigger a state update on an unmounted component.
 */
export function useCategoryMappings(): UseCategoryMappingsResult {
  const [categories, setCategories] = useState<CategoryMappingDto[]>([]);
  const [isLoading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const isMountedRef = useRef(true);
  // Increment to trigger a re-fetch (used by retry).
  const [retryCount, setRetryCount] = useState(0);

  const load = useCallback(async (signal: AbortSignal) => {
    setLoading(true);
    setError(null);
    try {
      const data = await categoriesClient.getAll({ signal });
      if (!isMountedRef.current) return;
      setCategories(data);
    } catch (err) {
      if (axios.isAxiosError(err) && err.code === "ERR_CANCELED") return;
      if (!isMountedRef.current) return;
      const message =
        err instanceof Error ? err.message : "Unexpected error while loading categories.";
      setError(message);
    } finally {
      if (!isMountedRef.current) return;
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    isMountedRef.current = true;
    const controller = new AbortController();
    void load(controller.signal);
    return () => {
      isMountedRef.current = false;
      controller.abort();
    };
    // retryCount is intentionally included: incrementing it re-runs this effect,
    // which is exactly how manual retry is implemented without duplicating the load logic.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [load, retryCount]);

  const retry = useCallback(() => {
    setRetryCount((n) => n + 1);
  }, []);

  return { categories, isLoading, error, retry };
}
