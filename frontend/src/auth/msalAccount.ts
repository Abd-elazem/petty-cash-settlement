import type { AccountInfo, IPublicClientApplication } from "@azure/msal-browser";

export function resolveActiveAccount(
  instance: IPublicClientApplication,
  accounts: AccountInfo[]
): AccountInfo | null {
  return instance.getActiveAccount() ?? accounts[0] ?? null;
}

