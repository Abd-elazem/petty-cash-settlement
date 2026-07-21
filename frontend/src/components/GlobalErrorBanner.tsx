type GlobalErrorBannerProps = {
  message: string;
  onDismiss?: () => void;
};

export function GlobalErrorBanner({ message, onDismiss }: GlobalErrorBannerProps): JSX.Element {
  return (
    <div className="global-error-banner" role="alert" aria-live="assertive">
      <span>{message}</span>
      {onDismiss ? (
        <button type="button" className="button-link" onClick={onDismiss}>
          Dismiss
        </button>
      ) : null}
    </div>
  );
}

