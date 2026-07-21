import { useAuth } from "../auth/AuthContext";
import { GlobalErrorBanner } from "../components/GlobalErrorBanner";
import { GlobalLoading } from "../components/GlobalLoading";

export function SignInPage(): JSX.Element {
  const { isAuthenticated, isLoading, authError, clearError, signIn } = useAuth();

  const handleSignIn = async (): Promise<void> => {
    await signIn();
  };
  if (isAuthenticated) {
    return <GlobalLoading message="Redirecting..." />;
  }

  return (
    <div className="page-container">
      <div className="card">
        <h2>Sign in</h2>
        <p>Use your Microsoft Entra account to access the Petty Cash Settlement app.</p>
        {authError ? <GlobalErrorBanner message={authError} onDismiss={clearError} /> : null}
        <button
          className="button-primary"
          type="button"
          onClick={() => void handleSignIn()}
          disabled={isLoading}
        >
          {!isLoading ? "Sign in with Microsoft" : "Signing in..."}
        </button>
      </div>
    </div>
  );
}

