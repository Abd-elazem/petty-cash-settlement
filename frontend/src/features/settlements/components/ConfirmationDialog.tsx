import { useEffect, useRef } from "react";

type ConfirmationDialogProps = {
  title: string;
  message: string;
  isOpen: boolean;
  isPending: boolean;
  error: string | null;
  confirmLabel: string;
  pendingLabel: string;
  confirmButtonClassName?: string;
  onConfirm: () => Promise<void>;
  onCancel: () => void;
};

const focusableSelector = [
  "button:not([disabled])",
  "[href]",
  "input:not([disabled])",
  "select:not([disabled])",
  "textarea:not([disabled])",
  "[tabindex]:not([tabindex='-1'])"
].join(", ");

export function ConfirmationDialog({
  title,
  message,
  isOpen,
  isPending,
  error,
  confirmLabel,
  pendingLabel,
  confirmButtonClassName = "button-primary",
  onConfirm,
  onCancel
}: ConfirmationDialogProps): JSX.Element | null {
  const dialogCardRef = useRef<HTMLDivElement | null>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    previousFocusRef.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;

    const focusFirst = (): void => {
      const container = dialogCardRef.current;
      if (!container) {
        return;
      }
      const focusables = container.querySelectorAll<HTMLElement>(focusableSelector);
      if (focusables.length > 0) {
        focusables[0].focus();
      } else {
        container.focus();
      }
    };

    focusFirst();

    const handleKeyDown = (event: KeyboardEvent): void => {
      if (!isOpen) {
        return;
      }

      if (event.key === "Escape" && !isPending) {
        event.preventDefault();
        onCancel();
        return;
      }

      if (event.key !== "Tab") {
        return;
      }

      const container = dialogCardRef.current;
      if (!container) {
        return;
      }
      const focusables = Array.from(container.querySelectorAll<HTMLElement>(focusableSelector));
      if (focusables.length === 0) {
        event.preventDefault();
        return;
      }

      const first = focusables[0];
      const last = focusables[focusables.length - 1];
      const current = document.activeElement;

      if (event.shiftKey) {
        if (current === first || !container.contains(current)) {
          event.preventDefault();
          last.focus();
        }
        return;
      }

      if (current === last || !container.contains(current)) {
        event.preventDefault();
        first.focus();
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("keydown", handleKeyDown);
      previousFocusRef.current?.focus();
    };
  }, [isOpen, isPending, onCancel]);

  if (!isOpen) {
    return null;
  }

  return (
    <div className="dialog-overlay" role="dialog" aria-modal="true" aria-label={title}>
      <div className="dialog-card" ref={dialogCardRef} tabIndex={-1}>
        <h4>{title}</h4>
        <p>{message}</p>
        {error ? <div className="settlement-form-api-error">{error}</div> : null}
        <div className="dialog-actions">
          <button type="button" className="button-secondary" onClick={onCancel} disabled={isPending}>
            Cancel
          </button>
          <button
            type="button"
            className={confirmButtonClassName}
            onClick={() => void onConfirm()}
            disabled={isPending}
          >
            {isPending ? pendingLabel : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
