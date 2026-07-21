import { useEffect } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "./AuthContext";
import { GlobalLoading } from "../components/GlobalLoading";

type ProtectedRouteProps = {
  children: JSX.Element;
};

export function ProtectedRoute({ children }: ProtectedRouteProps): JSX.Element {
  const { isAuthenticated, isLoading, signIn } = useAuth();
  const location = useLocation();

  useEffect(() => {
    if (!isAuthenticated && !isLoading) {
      void signIn();
    }
  }, [isAuthenticated, isLoading, location.pathname, location.search, signIn]);

  if (isAuthenticated) {
    return children;
  }

  if (isLoading) {
    return (
      <div className="page-container">
        <GlobalLoading message="Restoring session..." />
      </div>
    );
  }

  return <Navigate to="/signin" replace />;
}

