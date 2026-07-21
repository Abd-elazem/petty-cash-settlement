import React from "react";
import ReactDOM from "react-dom/client";
import { MsalProvider } from "@azure/msal-react";
import App from "./App";
import { appEnv } from "./config/env";
import { msalInstance } from "./config/msal";
import { AuthProvider } from "./auth/AuthContext";
import { DevAuthProvider } from "./auth/DevAuthContext";
import "./styles.css";

const root = ReactDOM.createRoot(document.getElementById("root") as HTMLElement);

function renderBootError(message: string): void {
  root.render(
    <React.StrictMode>
      <div className="page-container">
        <div className="card">
          <h2>Application startup failed</h2>
          <p>{message}</p>
        </div>
      </div>
    </React.StrictMode>
  );
}

if (!appEnv.authEnabled) {
  // Dev mode: skip MSAL entirely. DevAuthProvider presents a fixed authenticated
  // user that matches the backend's DevelopmentAuthenticationHandler identity.
  root.render(
    <React.StrictMode>
      <DevAuthProvider>
        <App />
      </DevAuthProvider>
    </React.StrictMode>
  );
} else if (msalInstance) {
  // Production / auth-enabled mode: initialize MSAL before mounting.
  void msalInstance
    .initialize()
    .then(() => {
      // Re-assert the non-null check inside the callback for TypeScript.
      const msal = msalInstance!;
      const accounts = msal.getAllAccounts();
      if (accounts.length > 0) {
        msal.setActiveAccount(accounts[0]);
      }
      root.render(
        <React.StrictMode>
          <MsalProvider instance={msal}>
            <AuthProvider>
              <App />
            </AuthProvider>
          </MsalProvider>
        </React.StrictMode>
      );
    })
    .catch((error) => {
      const message =
        error instanceof Error ? error.message : "Unable to initialize authentication.";
      renderBootError(message);
    });
} else {
  // Should not be reachable: authEnabled is true but msalInstance is null.
  renderBootError("Authentication is enabled but MSAL could not be initialized.");
}
