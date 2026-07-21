export type CategoryMappingDto = {
  categoryCode: string;
  displayName: string;
  kmRequired: boolean;
};

export type SettlementLineDto = {
  lineId: string;
  lineNo: number;
  categoryCode: string;
  grossAmount: number;
  isVat: boolean;
  vatAmount: number;
  netAmount: number;
  notes: string | null;
  carPlate: string | null;
  odometerKm: number | null;
};

export type SettlementDto = {
  requestId: string;
  version: number;
  settlementDate: string;
  purpose: string;
  spenderId: string;
  spenderName: string;
  workerId: string;
  approverEmail: string;
  status: string;
  totalAmount: number;
  approvalComment: string | null;
  journalBatchNumber: string | null;
  isEditable: boolean;
  lines: SettlementLineDto[];
};

export type SettlementSummaryDto = {
  requestId: string;
  settlementDate: string;
  purpose: string;
  status: string;
  totalAmount: number;
};

export type CreateSettlementRequest = {
  settlementDate: string;
  purpose: string;
};

export type AddSettlementLineRequest = {
  categoryCode: string;
  grossAmount: number;
  isVat: boolean;
  notes: string | null;
  carPlate: string | null;
  odometerKm: number | null;
};

export type UpdateSettlementLineRequest = {
  categoryCode: string;
  grossAmount: number;
  isVat: boolean;
  notes: string | null;
  carPlate: string | null;
  odometerKm: number | null;
};

