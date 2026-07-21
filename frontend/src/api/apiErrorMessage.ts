import axios from "axios";

export function getApiErrorMessage(error: unknown, fallbackMessage: string): string {
  if (!axios.isAxiosError(error)) {
    return error instanceof Error ? error.message : fallbackMessage;
  }

  const data = error.response?.data;
  if (data && typeof data === "object") {
    const record = data as Record<string, unknown>;
    const errors = record.errors;

    if (errors && typeof errors === "object") {
      const flattened = Object.values(errors)
        .flatMap((value) => (Array.isArray(value) ? value : [value]))
        .filter((value): value is string => typeof value === "string" && value.trim().length > 0);
      if (flattened.length > 0) {
        return flattened.join(" ");
      }
    }

    if (Array.isArray(errors)) {
      const messages = errors.filter((value): value is string => typeof value === "string");
      if (messages.length > 0) {
        return messages.join(" ");
      }
    }

    if (typeof record.detail === "string" && record.detail.trim().length > 0) {
      return record.detail;
    }
    if (typeof record.title === "string" && record.title.trim().length > 0) {
      return record.title;
    }
  }

  return error.message || fallbackMessage;
}
