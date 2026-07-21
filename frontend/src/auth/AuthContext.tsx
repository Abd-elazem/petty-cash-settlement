import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode
} from "react";
import { EventType, InteractionStatus } from "@azure/msal-browser";
import { useMsal } from "@azure/msal-react";
import { loginRequest } from "../config/msal";
import { resolveActiveAccount } from "./msalAccount";

export type AuthUser = {
  accountId: string;
  name: string;
  username: string;
  isManager: boolean;
};

export type AuthContextValue = {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: AuthUser | null;
  authError: string | null;
  signIn: () => Promise<void>;
  signOut: () => Promise<void>;
  clearError: () => void;
};

// Exported so DevAuthProvider (dev-auth-disabled mode) can provide to the same
// context slot — that way all useAuth() consumers work identically regardless of
// which provider is mounted in main.tsx.
export const AuthContext = createContext<AuthContextValue | undefined>(undefined);

type AuthProviderProps = {
  children: ReactNode;
};

export function AuthProvider({ children }: AuthProviderProps): JSX.Element {
  const { instance, accounts, inProgress } = useMsal();
  const [authError, setAuthError] = useState<string | null>(null);

  useEffect(() => {
    const active = resolveActiveAccount(instance, accounts);
    if (active && instance.getActiveAccount() === null) {
      instance.setActiveAccount(active);
    }
  }, [accounts, instance]);

  useEffect(() => {
    const callbackId = instance.addEventCallback((event) => {
      if (
        event.eventType === EventType.LOGIN_SUCCESS ||
        event.eventType === EventType.SSO_SILENT_SUCCESS ||
        event.eventType === EventType.ACQUIRE_TOKEN_SUCCESS
      ) {
        if (event.payload && "account" in event.payload && event.payload.account) {
          instance.setActiveAccount(event.payload.account);
        }
        setAuthError(null);
      }

      if (event.eventType === EventType.LOGIN_FAILURE || event.eventType === EventType.ACQUIRE_TOKEN_FAILURE) {
        const error = event.error;
        setAuthError(error?.message ?? "Authentication failed.");
      }

      if (event.eventType === EventType.LOGOUT_SUCCESS) {
        instance.setActiveAccount(null);
        setAuthError(null);
      }
    });

    return () => {
      if (callbackId) {
        instance.removeEventCallback(callbackId);
      }
    };
  }, [instance]);

  const account = useMemo(() => resolveActiveAccount(instance, accounts), [accounts, instance]);
  const isAuthenticated = account !== null;
  const isLoading = inProgress !== InteractionStatus.None;

  const signIn = useCallback(async () => {
    setAuthError(null);
    try {
      await instance.loginRedirect(loginRequest);
    } catch (error) {
      const message = error instanceof Error ? error.message : "Unable to sign in.";
      setAuthError(message);
    }
  }, [instance]);

  const signOut = useCallback(async () => {
    setAuthError(null);
    try {
      const activeAccount = resolveActiveAccount(instance, accounts);
      instance.setActiveAccount(null);
      await instance.logoutRedirect(
        activeAccount
          ? {
              account: activeAccount
            }
          : undefined
      );
    } catch (error) {
      const message = error instanceof Error ? error.message : "Unable to sign out.";
      setAuthError(message);
    }
  }, [accounts, instance]);

  const clearError = useCallback(() => setAuthError(null), []);

  const user = useMemo<AuthUser | null>(() => {
    if (!account) {
      return null;
    }

    return {
      accountId: account.homeAccountId,
      name: account.name ?? "Authenticated User",
      username: account.username,
      // Entra app-role claim. The backend assigns the "Approver" role to managers;
      // we read it here from the ID token claims MSAL already caches — no extra API
      // call or scope needed. Falls back to false when claims are absent (dev/no-auth
      // environments) so the manager nav is simply hidden rather than crashing.
      isManager: Array.isArray(account.idTokenClaims?.roles)
        ? (account.idTokenClaims.roles as string[]).includes("Approver")
        : false
    };
  }, [account]);

  const value = useMemo<AuthContextValue>(
    () => ({
      isAuthenticated,
      isLoading,
      user,
      authError,
      signIn,
      signOut,
      clearError
    }),
    [authError, clearError, isAuthenticated, isLoading, signIn, signOut, user]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider.");
  }
  return context;
}

