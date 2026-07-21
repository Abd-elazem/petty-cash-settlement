type ErrorStateProps = {
  message: string;
  onRetry: () => void;
};

export function ErrorState({ message, onRetry }: ErrorStateProps): JSX.Element {
  return (
    <div className="card settlements-state-card settlements-state-card--error">
      <h3>Could not load settlements</h3>
      <p>{message}</p>
      <button type="button" className="button-primary" onClick={onRetry}>
        Retry
      </button>
    </div>
  );
}

