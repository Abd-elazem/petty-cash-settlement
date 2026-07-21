import axios, { type AxiosError } from "axios";
import { InteractionRequiredAuthError } from "@azure/msal-browser";
import { appEnv } from "../config/env";
import { loginRequest, msalInstance } from "../config/msal";
import { resolveActiveAccount } from "../auth/msalAccount";

function getActiveAccount() {
  if (!msalInstance) return null;
  return resolveActiveAccount(msalInstance, msalInstance.getAllAccounts());
}

export const apiClient = axios.create({
  baseURL: appEnv.apiBaseUrl
});
let isHandlingUnauthorized = false;

apiClient.interceptors.request.use(async (config) => {
  // When auth is disabled (dev mode), skip token acquisition entirely — the backend's
  // DevelopmentAuthenticationHandler accepts requests with no Authorization header.
  if (!appEnv.authEnabled || !msalInstance) {
    return config;
  }

  const account = getActiveAccount();
  if (!account || appEnv.entraScopes.length === 0) {
    return config;
  }
  let accessToken: string;
  try {
    const token = await msalInstance.acquireTokenSilent({
      account,
      scopes: appEnv.entraScopes
    });
    accessToken = token.accessToken;
  } catch (error) {
    if (error instanceof InteractionRequiredAuthError) {
      await msalInstance.acquireTokenRedirect({
        account,
        scopes: appEnv.entraScopes
      });
      return config;
    }
    throw error;
  }

  config.headers = config.headers ?? {};
  config.headers.Authorization = `Bearer ${accessToken}`;
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const status = error.response?.status;
    const account = getActiveAccount();
    if (status === 401 && account && msalInstance && !isHandlingUnauthorized) {
      isHandlingUnauthorized = true;
      msalInstance.setActiveAccount(null);
      try {
        await msalInstance.logoutRedirect({
          account
        });
      } finally {
        isHandlingUnauthorized = false;
      }
    }
    throw error;
  }
);

export const defaultApiScopes = loginRequest.scopes;

