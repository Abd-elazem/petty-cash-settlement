function requireEnv(key: keyof ImportMetaEnv): string {
  const value = import.meta.env[key];
  if (value === undefined || value.trim() === "") {
    throw new Error(`Missing required environment variable: ${key}`);
  }
  return value;
}

function optionalEnv(key: keyof ImportMetaEnv): string | undefined {
  const value = import.meta.env[key];
  return value === undefined || value.trim() === "" ? undefined : value;
}

// VITE_AUTH_ENABLED defaults to true when absent or set to any value other than "false".
// Set VITE_AUTH_ENABLED=false in .env to bypass MSAL and use the dev auth provider.
const authEnabledRaw = optionalEnv("VITE_AUTH_ENABLED");
const authEnabled = authEnabledRaw !== "false";

export const appEnv = {
  apiBaseUrl: requireEnv("VITE_API_BASE_URL"),
  authEnabled,
  // Entra vars are only required when authentication is enabled. Reading them
  // unconditionally would make VITE_AUTH_ENABLED=false useless because requireEnv()
  // would still throw for placeholder/missing values at module load time.
  entraClientId: authEnabled ? requireEnv("VITE_ENTRA_CLIENT_ID") : "",
  entraTenantId: authEnabled ? requireEnv("VITE_ENTRA_TENANT_ID") : "",
  entraRedirectUri: authEnabled ? requireEnv("VITE_ENTRA_REDIRECT_URI") : "",
  entraPostLogoutRedirectUri: authEnabled ? requireEnv("VITE_ENTRA_POST_LOGOUT_REDIRECT_URI") : "",
  entraScopes: (
    import.meta.env.VITE_ENTRA_SCOPES?.split(",")
      .map((value) => value.trim())
      .filter((value) => value.length > 0) ?? []
  ),
  // Dev-auth-only. Ignored when authEnabled is true.
  // VITE_DEV_USERNAME defaults to "spender.demo" (matches DevelopmentAuthenticationHandler).
  // VITE_DEV_ROLE defaults to "Spender"; set to "Approver" to test manager routes locally.
  devUsername: optionalEnv("VITE_DEV_USERNAME") ?? "spender.demo",
  devRole: optionalEnv("VITE_DEV_ROLE") ?? "Spender"
};

