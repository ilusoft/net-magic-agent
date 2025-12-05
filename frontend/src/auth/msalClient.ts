import { PublicClientApplication } from "@azure/msal-browser";

const clientId = import.meta.env.VITE_MSAL_CLIENT_ID;

if (!clientId) {
  throw new Error(
    [
      "[Auth] Missing VITE_MSAL_CLIENT_ID environment variable.",
      "Add it to your .env.local (or equivalent) to configure Microsoft authentication.",
    ].join(" ")
  );
}

const tenantId = import.meta.env.VITE_MSAL_TENANT_ID?.trim();
const tenantName = import.meta.env.VITE_MSAL_TENANT_NAME?.trim();
const policyId = import.meta.env.VITE_MSAL_POLICY_ID?.trim();
const explicitAuthority = import.meta.env.VITE_MSAL_AUTHORITY?.trim();

const b2cAuthority =
  tenantName && policyId
    ? `https://${tenantName}.b2clogin.com/${tenantName}.onmicrosoft.com/${policyId}`
    : null;

const defaultAuthority = tenantId
  ? `https://login.microsoftonline.com/${tenantId}`
  : "https://login.microsoftonline.com/common";

const resolvedAuthority = explicitAuthority ?? b2cAuthority ?? defaultAuthority;

const knownAuthorities =
  tenantName && b2cAuthority
    ? [`https://${tenantName}.b2clogin.com`]
    : undefined;
const redirectUri =
  import.meta.env.VITE_MSAL_REDIRECT_URI ||
  (typeof window !== "undefined" ? window.location.origin : undefined) ||
  "http://localhost:5173";

const cacheLocation =
  import.meta.env.VITE_MSAL_CACHE_LOCATION === "sessionStorage"
    ? "sessionStorage"
    : "localStorage";

const configuredScopes = import.meta.env.VITE_MSAL_SCOPES?.split(",")
  .map((scope: string) => scope.trim())
  .filter((scope: string) => scope.length > 0);

export const authScopes =
  configuredScopes && configuredScopes.length > 0
    ? configuredScopes
    : [`api://${clientId}/.default`];

export const loginRequest = {
  scopes: authScopes,
  authority: resolvedAuthority,
};

export const msalInstance = new PublicClientApplication({
  auth: {
    clientId,
    authority: resolvedAuthority,
    knownAuthorities,
    redirectUri,
  },
  cache: {
    cacheLocation,
    storeAuthStateInCookie: false,
  },
});
