import { Link } from "react-router-dom";

export function NotFoundPage(): JSX.Element {
  return (
    <div className="page-container">
      <div className="card">
        <h2>Page not found</h2>
        <p>The route you requested does not exist.</p>
        <Link to="/">Back to dashboard</Link>
      </div>
    </div>
  );
}

