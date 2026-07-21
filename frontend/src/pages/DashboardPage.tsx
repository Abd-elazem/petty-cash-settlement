import { useNavigate } from "react-router-dom";
export function DashboardPage(): JSX.Element {
  const navigate = useNavigate();
  return (
    <div className="page-container">
      <div className="card">
        <h2>Dashboard</h2>
        <p>
          Frontend Batch 2 placeholder rendered in the authenticated application shell.
          Settlement workflow widgets and summary metrics will be added in the next
          frontend batches.
        </p>
        <button type="button" className="button-primary" onClick={() => navigate("/settlements/new")}>
          New Settlement
        </button>
      </div>
    </div>
  );
}

