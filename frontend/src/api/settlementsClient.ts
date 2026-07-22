import { apiClient } from "./httpClient";
import type {
  AddSettlementLineRequest,
  CreateSettlementRequest,
  SettlementDto,
  SettlementSummaryDto,
  UpdateSettlementHeaderRequest,
  UpdateSettlementLineRequest
} from "../types/settlements";

const BASE_PATH = "/api/v1/settlements";
type RequestOptions = {
  signal?: AbortSignal;
};

export const settlementsClient = {
  async createDraft(payload: CreateSettlementRequest): Promise<SettlementDto> {
    const response = await apiClient.post<SettlementDto>(BASE_PATH, payload);
    return response.data;
  },

  async updateHeader(
    settlementId: string,
    payload: UpdateSettlementHeaderRequest
  ): Promise<SettlementDto> {
    const response = await apiClient.put<SettlementDto>(
      `${BASE_PATH}/${settlementId}`,
      payload
    );
    return response.data;
  },

  async addLine(settlementId: string, payload: AddSettlementLineRequest): Promise<SettlementDto> {
    const response = await apiClient.post<SettlementDto>(
      `${BASE_PATH}/${settlementId}/lines`,
      payload
    );
    return response.data;
  },

  async updateLine(
    settlementId: string,
    lineId: string,
    payload: UpdateSettlementLineRequest
  ): Promise<SettlementDto> {
    const response = await apiClient.put<SettlementDto>(
      `${BASE_PATH}/${settlementId}/lines/${lineId}`,
      payload
    );
    return response.data;
  },

  async removeLine(settlementId: string, lineId: string): Promise<SettlementDto> {
    const response = await apiClient.delete<SettlementDto>(
      `${BASE_PATH}/${settlementId}/lines/${lineId}`
    );
    return response.data;
  },

  async submit(settlementId: string): Promise<SettlementDto> {
    const response = await apiClient.post<SettlementDto>(`${BASE_PATH}/${settlementId}/submit`);
    return response.data;
  },

  async getMine(options?: RequestOptions): Promise<SettlementSummaryDto[]> {
    const response = await apiClient.get<SettlementSummaryDto[]>(`${BASE_PATH}/mine`, {
      signal: options?.signal
    });
    return response.data;
  },
  async getById(settlementId: string, options?: RequestOptions): Promise<SettlementDto> {
    const response = await apiClient.get<SettlementDto>(`${BASE_PATH}/${settlementId}`, {
      signal: options?.signal
    });
    return response.data;
  },

  async getInbox(options?: RequestOptions): Promise<SettlementDto[]> {
    const response = await apiClient.get<SettlementDto[]>(`${BASE_PATH}/inbox`, {
      signal: options?.signal
    });
    return response.data;
  },

  async approve(settlementId: string): Promise<SettlementDto> {
    const response = await apiClient.post<SettlementDto>(`${BASE_PATH}/${settlementId}/approve`);
    return response.data;
  },

  async reject(settlementId: string, comment: string): Promise<SettlementDto> {
    const response = await apiClient.post<SettlementDto>(`${BASE_PATH}/${settlementId}/reject`, {
      comment
    });
    return response.data;
  },

  async reopen(settlementId: string): Promise<SettlementDto> {
    const response = await apiClient.post<SettlementDto>(`${BASE_PATH}/${settlementId}/reopen`);
    return response.data;
  },

  async recordJournal(settlementId: string, journalBatchNumber: string): Promise<SettlementDto> {
    const response = await apiClient.post<SettlementDto>(`${BASE_PATH}/${settlementId}/journal`, {
      journalBatchNumber
    });
    return response.data;
  }
};

