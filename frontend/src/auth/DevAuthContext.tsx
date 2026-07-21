/**
 * Development-only authentication provider.
 *
 * Used when VITE_AUTH_ENABLED=false. Provides to the same AuthContext slot as the
 * MSAL-backed AuthProvider so all useAuth() consumers work identically without any
 * import change.
 *
 * The dev user identity is driven by environment variables so you can switch between
 * Spender and Approver without touching source code:
 *
 *   VITE_DEV_USERNAME  — username sent to the backend (default: "spender.demo").
 *                        Must match an AppUserProfile row in the backend seed data.
 *   VITE_DEV_ROLE      — "Spender" or "Approver" (default: "Spender").
 *                        Set to "Approver" to test manager-only routes locally.
 *
 * This file is imported only by main.tsx. All other modules import useAuth from
 * AuthContext.tsx as normal.
 */
import { useCallback, useMemo, type ReactNode } from "react";
import { appEnv } from "../config/env";
import { AuthContext, type AuthContextValue, type AuthUser } from "./AuthContext";

const DEV_USER: AuthUser = {
  accountId: "dev-local-user",
  name: `Dev ${appEnv.devRole} (local)`,
  username: appEnv.devUsername,
  isManager: appEnv.devRole === "Approver"
};

type DevAuthProviderProps = {
  children: ReactNode;
};

/**
 * Drop-in replacement for AuthProvider when VITE_AUTH_ENABLED=false.
 * Provides to the shared AuthContext so useAuth() in all consumers works unchanged.
 */
export function DevAuthProvider({ children }: DevAuthProviderProps): JSX.Element {
  const signIn = useCallback(async (): Promise<void> => {
    // No-op: dev mode user is always authenticated.
  }, []);

  const signOut = useCallback(async (): Promise<void> => {
    // No-op: no real session to destroy in dev mode.
  }, []);

  const clearError = useCallback((): void => {
    // No-op: dev mode produces no auth errors.
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      isAuthenticated: true,
      isLoading: false,
      user: DEV_USER,
      authError: null,
      signIn,
      signOut,
      clearError
    }),
    [clearError, signIn, signOut]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
