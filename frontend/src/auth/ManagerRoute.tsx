import { Navigate } from "react-router-dom";
import { useAuth } from "./AuthContext";
import { GlobalLoading } from "../components/GlobalLoading";

type ManagerRouteProps = {
  children: JSX.Element;
};

/**
 * Route guard for manager-only pages. Requires the user to be authenticated AND to
 * carry the "Approver" Entra app role (resolved via AuthContext.user.isManager).
 *
 * - Unauthenticated → /signin (handled by the outer ProtectedRoute, but checked here
 *   defensively in case ManagerRoute is ever used outside the ProtectedRoute tree).
 * - Authenticated but not a manager → /unauthorized.
 * - Auth loading → spinner (avoids a flash-of-unauthorized during session restore).
 *
 * In development/no-auth mode where idTokenClaims.roles is absent, isManager is false,
 * so manager routes redirect to /unauthorized. That is intentional: it keeps the guard
 * honest rather than open. Developers testing locally should use a test account with
 * the Approver role assigned, or temporarily seed a role in the mock token.
 */
export function ManagerRoute({ children }: ManagerRouteProps): JSX.Element {
  const { isAuthenticated, isLoading, user } = useAuth();

  if (isLoading) {
    return (
      <div className="page-container">
        <GlobalLoading message="Restoring session..." />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/signin" replace />;
  }

  if (!user?.isManager) {
    return <Navigate to="/unauthorized" replace />;
  }

  return children;
}
