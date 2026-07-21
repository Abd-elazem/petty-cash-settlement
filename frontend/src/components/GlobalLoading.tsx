type GlobalLoadingProps = {
  message?: string;
};

export function GlobalLoading({ message = "Loading..." }: GlobalLoadingProps): JSX.Element {
  return (
    <div className="global-loading" role="status" aria-live="polite">
      <div className="spinner" aria-hidden="true" />
      <span>{message}</span>
    </div>
  );
}

