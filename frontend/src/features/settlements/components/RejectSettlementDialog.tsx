import { useEffect, useRef } from "react";

type RejectSettlementDialogProps = {
  isOpen: boolean;
  isRejecting: boolean;
  comment: string;
  commentError: string | null;
  apiError: string | null;
  onCommentChange: (value: string) => void;
  onConfirm: () => Promise<void>;
  onCancel: () => void;
};

const focusableSelector = [
  "button:not([disabled])",
  "textarea:not([disabled])",
  "[tabindex]:not([tabindex='-1'])"
].join(", ");

/**
 * Reject dialog with a required comment textarea. Reuses the same dialog-overlay /
 * dialog-card CSS already in styles.css, and the same focus-trap / Escape-to-close /
 * focus-return pattern established in ConfirmationDialog. Kept as a dedicated component
 * (not a ConfirmationDialog extension) because the comment textarea is a first-class
 * form element with its own validation state — it is not a cosmetic variation.
 */
export function RejectSettlementDialog({
  isOpen,
  isRejecting,
  comment,
  commentError,
  apiError,
  onCommentChange,
  onConfirm,
  onCancel
}: RejectSettlementDialogProps): JSX.Element | null {
  const cardRef = useRef<HTMLDivElement | null>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    if (!isOpen) return;

    previousFocusRef.current =
      document.activeElement instanceof HTMLElement ? document.activeElement : null;

    const focusFirst = (): void => {
      const container = cardRef.current;
      if (!container) return;
      const focusables = container.querySelectorAll<HTMLElement>(focusableSelector);
      if (focusables.length > 0) focusables[0].focus();
      else container.focus();
    };
    focusFirst();

    const handleKeyDown = (e: KeyboardEvent): void => {
      if (e.key === "Escape" && !isRejecting) {
        e.preventDefault();
        onCancel();
        return;
      }
      if (e.key !== "Tab") return;
      const container = cardRef.current;
      if (!container) return;
      const focusables = Array.from(container.querySelectorAll<HTMLElement>(focusableSelector));
      if (focusables.length === 0) { e.preventDefault(); return; }
      const first = focusables[0];
      const last = focusables[focusables.length - 1];
      const current = document.activeElement;
      if (e.shiftKey) {
        if (current === first || !container.contains(current)) { e.preventDefault(); last.focus(); }
        return;
      }
      if (current === last || !container.contains(current)) { e.preventDefault(); first.focus(); }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("keydown", handleKeyDown);
      previousFocusRef.current?.focus();
    };
  }, [isOpen, isRejecting, onCancel]);

  if (!isOpen) return null;

  return (
    <div className="dialog-overlay" role="dialog" aria-modal="true" aria-label="Reject settlement">
      <div className="dialog-card" ref={cardRef} tabIndex={-1}>
        <h4>Reject settlement</h4>
        <p>Provide a reason for rejection. The employee will see this comment.</p>

        <div className="settlement-form-field">
          <span>Rejection reason <span aria-hidden="true">*</span></span>
          <textarea
            rows={3}
            value={comment}
            disabled={isRejecting}
            placeholder="Enter rejection reason…"
            onChange={(e) => onCommentChange(e.target.value)}
            aria-required="true"
            aria-invalid={Boolean(commentError)}
          />
          {commentError ? (
            <span className="settlement-form-error" role="alert">{commentError}</span>
          ) : null}
        </div>

        {apiError ? (
          <div className="settlement-form-api-error" role="alert">{apiError}</div>
        ) : null}

        <div className="dialog-actions">
          <button
            type="button"
            className="button-secondary"
            onClick={onCancel}
            disabled={isRejecting}
          >
            Cancel
          </button>
          <button
            type="button"
            className="button-primary button-danger"
            onClick={() => void onConfirm()}
            disabled={isRejecting}
          >
            {isRejecting ? "Rejecting…" : "Reject"}
          </button>
        </div>
      </div>
    </div>
  );
}
