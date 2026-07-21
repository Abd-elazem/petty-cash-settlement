/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string;
  readonly VITE_AUTH_ENABLED?: string;
  readonly VITE_ENTRA_CLIENT_ID?: string;
  readonly VITE_ENTRA_TENANT_ID?: string;
  readonly VITE_ENTRA_REDIRECT_URI?: string;
  readonly VITE_ENTRA_POST_LOGOUT_REDIRECT_URI?: string;
  readonly VITE_ENTRA_SCOPES?: string;
  // Dev-auth-only (VITE_AUTH_ENABLED=false). Ignored when auth is enabled.
  readonly VITE_DEV_USERNAME?: string;
  readonly VITE_DEV_ROLE?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

