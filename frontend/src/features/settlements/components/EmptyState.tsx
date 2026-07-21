type EmptyStateProps = {
  title: string;
  description: string;
};

export function EmptyState({ title, description }: EmptyStateProps): JSX.Element {
  return (
    <div className="card settlements-state-card">
      <h3>{title}</h3>
      <p>{description}</p>
    </div>
  );
}

