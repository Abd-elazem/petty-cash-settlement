import {
  BrowserCacheLocation,
  LogLevel,
  PublicClientApplication,
  type Configuration,
  type IPublicClientApplication
} from "@azure/msal-browser";
import { appEnv } from "./env";

// msalInstance is null when authentication is disabled (VITE_AUTH_ENABLED=false).
// main.tsx gates MsalProvider and msalInstance.initialize() on this value, so
// nothing in the MSAL library runs at all in dev-auth-disabled mode.
export let msalInstance: IPublicClientApplication | null = null;

if (appEnv.authEnabled) {
  const msalConfig: Configuration = {
    auth: {
      clientId: appEnv.entraClientId,
      authority: `https://login.microsoftonline.com/${appEnv.entraTenantId}`,
      redirectUri: appEnv.entraRedirectUri,
      postLogoutRedirectUri: appEnv.entraPostLogoutRedirectUri,
      navigateToLoginRequestUrl: true
    },
    cache: {
      cacheLocation: BrowserCacheLocation.SessionStorage
    },
    system: {
      loggerOptions: {
        loggerCallback: (level, message, containsPii) => {
          if (containsPii) {
            return;
          }
          if (level === LogLevel.Error) {
            console.error(message);
          }
        },
        logLevel: LogLevel.Error
      }
    }
  };

  msalInstance = new PublicClientApplication(msalConfig);
}

export const loginRequest = {
  scopes: appEnv.entraScopes
};

