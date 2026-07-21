import type { SettlementSummaryDto } from "../../types/settlements";

export function parseSettlementDate(value: string): number {
  const date = new Date(value);
  const timestamp = date.getTime();
  return Number.isNaN(timestamp) ? 0 : timestamp;
}

export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "EGP",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(amount);
}

export function formatDate(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric"
  }).format(date);
}

export function normalizeStatus(status: string): string {
  return status.trim().toLowerCase();
}

export function settlementSearchMatch(settlement: SettlementSummaryDto, query: string): boolean {
  const normalized = query.trim().toLowerCase();
  if (normalized.length === 0) {
    return true;
  }

  return (
    settlement.purpose.toLowerCase().includes(normalized) ||
    settlement.requestId.toLowerCase().includes(normalized)
  );
}

