import { Link } from "react-router-dom";

export function UnauthorizedPage(): JSX.Element {
  return (
    <div className="page-container">
      <div className="card">
        <h2>Access denied</h2>
        <p>You are authenticated but do not have access to this page.</p>
        <Link to="/">Back to dashboard</Link>
      </div>
    </div>
  );
}

