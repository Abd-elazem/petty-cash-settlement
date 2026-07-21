import { normalizeStatus } from "../utils";

type StatusBadgeProps = {
  status: string;
};

const statusClassMap: Record<string, string> = {
  draft: "status-badge status-badge--draft",
  submitted: "status-badge status-badge--submitted",
  approved: "status-badge status-badge--approved",
  rejected: "status-badge status-badge--rejected",
  journalled: "status-badge status-badge--journalled",
  posted: "status-badge status-badge--posted"
};

export function StatusBadge({ status }: StatusBadgeProps): JSX.Element {
  const key = normalizeStatus(status);
  const className = statusClassMap[key] ?? "status-badge status-badge--default";
  return <span className={className}>{status}</span>;
}

