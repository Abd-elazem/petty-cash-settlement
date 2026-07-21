import { apiClient } from "./httpClient";
import type { CategoryMappingDto } from "../types/settlements";

const BASE_PATH = "/api/v1/category-mappings";

type RequestOptions = {
  signal?: AbortSignal;
};

export const categoriesClient = {
  /**
   * Returns all active category mappings ordered alphabetically by display name.
   * Used to populate the Category dropdown in the settlement line form.
   * The backend exposes only categoryCode, displayName, and kmRequired — no
   * internal accounting fields are included in the response.
   */
  async getAll(options?: RequestOptions): Promise<CategoryMappingDto[]> {
    const response = await apiClient.get<CategoryMappingDto[]>(BASE_PATH, {
      signal: options?.signal
    });
    return response.data;
  }
};
