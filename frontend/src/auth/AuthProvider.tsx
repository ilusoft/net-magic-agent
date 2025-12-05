import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import type { AccountInfo, AuthenticationResult } from "@azure/msal-browser";
import { InteractionRequiredAuthError } from "@azure/msal-browser";
import { loginRequest, msalInstance } from "@/auth/msalClient";

interface AuthContextValue {
  account: AccountInfo | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  login: () => Promise<void>;
  logout: () => Promise<void>;
  acquireToken: () => Promise<string | null>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [account, setAccount] = useState<AccountInfo | null>(null);
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function bootstrapAuth() {
      try {
        await msalInstance.initialize();
        const redirectResult = await msalInstance.handleRedirectPromise();

        let activeAccount =
          redirectResult?.account ??
          msalInstance.getActiveAccount() ??
          msalInstance.getAllAccounts()[0] ??
          null;

        if (activeAccount) {
          msalInstance.setActiveAccount(activeAccount);
          const tokenResult = await msalInstance.acquireTokenSilent({
            ...loginRequest,
            account: activeAccount,
          });

          if (cancelled) {
            return;
          }

          setAccount(activeAccount);
          setAccessToken(tokenResult.idToken);
          setIsAuthenticated(true);
          setError(null);
        }
      } catch (bootstrapError) {
        if (cancelled) {
          return;
        }

        const message =
          bootstrapError instanceof Error
            ? bootstrapError.message
            : "Unable to initialize authentication.";
        setAccount(null);
        setAccessToken(null);
        setIsAuthenticated(false);
        setError(message);
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    void bootstrapAuth();

    return () => {
      cancelled = true;
    };
  }, []);

  const acquireToken = useCallback(async () => {
    let activeAccount = msalInstance.getActiveAccount();

    if (!activeAccount) {
      const knownAccounts = msalInstance.getAllAccounts();

      if (knownAccounts.length === 0) {
        setAccount(null);
        setAccessToken(null);
        setIsAuthenticated(false);
        return null;
      }

      activeAccount = knownAccounts[0] ?? null;

      if (!activeAccount) {
        return null;
      }

      msalInstance.setActiveAccount(activeAccount);
    }

    try {
      const tokenResult: AuthenticationResult =
        await msalInstance.acquireTokenSilent({
          ...loginRequest,
          account: activeAccount,
        });

      setAccount(activeAccount);
      setAccessToken(tokenResult.idToken);
      setIsAuthenticated(true);
      setError(null);
      return tokenResult.idToken;
    } catch (tokenError) {
      if (tokenError instanceof InteractionRequiredAuthError) {
        await msalInstance.acquireTokenRedirect(loginRequest);
        return null;
      }

      const message =
        tokenError instanceof Error
          ? tokenError.message
          : "Unable to acquire access token.";
      setError(message);
      setIsAuthenticated(false);
      setAccessToken(null);
      return null;
    }
  }, []);

  const login = useCallback(async () => {
    setError(null);
    setIsLoading(true);

    try {
      await msalInstance.loginRedirect(loginRequest);
    } catch (loginError) {
      const message =
        loginError instanceof Error
          ? loginError.message
          : "Unable to start login flow.";
      setError(message);
      setIsLoading(false);
    }
  }, []);

  const logout = useCallback(async () => {
    setIsLoading(true);

    try {
      await msalInstance.logoutRedirect();
    } finally {
      setAccount(null);
      setAccessToken(null);
      setIsAuthenticated(false);
      setError(null);
      setIsLoading(false);
    }
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      account,
      accessToken,
      isAuthenticated,
      isLoading,
      error,
      login,
      logout,
      acquireToken,
    }),
    [
      account,
      accessToken,
      isAuthenticated,
      isLoading,
      error,
      login,
      logout,
      acquireToken,
    ]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider.");
  }

  return context;
}
