import { useCallback, useEffect, useMemo, useState } from "react";
import "@/index.css";
import { useAuthorizedFetch } from "@/hooks/useAuthorizedFetch";
import { useAuth } from "@/auth/AuthProvider";
import type { AgentDefinitionsDocument } from "@/types/agents";
import { AgentDefinitionsView } from "@/views/AgentDefinitionsView";
import { AgentRunnerView } from "@/views/AgentRunnerView";
import type { AgentRunnerState } from "@/views/AgentRunnerView";

type ActiveView = "editor" | "runner";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5134";
const RUNNER_STATE_STORAGE_KEY = "magic-agent-runner-state";

const defaultRunnerState: AgentRunnerState = {
  selectedAgentId: undefined,
  input: "",
  conversationId: null,
  conversation: [],
  isRunning: false,
  runError: null,
  authHeaderName: "Authorization",
  authHeaderValue: "",
  diagnostics: null,
  debugError: null,
  showDebugPanel: false,
};

function loadRunnerState(): AgentRunnerState {
  if (typeof window === "undefined") {
    return defaultRunnerState;
  }

  try {
    const stored = window.sessionStorage.getItem(RUNNER_STATE_STORAGE_KEY);

    if (!stored) {
      return defaultRunnerState;
    }

    const parsed = JSON.parse(stored) as Partial<AgentRunnerState>;
    return {
      ...defaultRunnerState,
      ...parsed,
      conversation: Array.isArray(parsed.conversation)
        ? parsed.conversation
        : defaultRunnerState.conversation,
    };
  } catch {
    return defaultRunnerState;
  }
}

function App() {
  const authorizedFetch = useAuthorizedFetch();
  const {
    account,
    isAuthenticated,
    isLoading: authLoading,
    error: authError,
    login,
    logout,
  } = useAuth();
  const [activeView, setActiveView] = useState<ActiveView>("editor");
  const [definitions, setDefinitions] =
    useState<AgentDefinitionsDocument | null>(null);
  const [loadingDefs, setLoadingDefs] = useState(false);
  const [definitionsError, setDefinitionsError] = useState<string | null>(null);
  const [runnerState, setRunnerState] = useState<AgentRunnerState>(() =>
    loadRunnerState()
  );

  useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    try {
      window.sessionStorage.setItem(
        RUNNER_STATE_STORAGE_KEY,
        JSON.stringify(runnerState)
      );
    } catch {
      // Ignore storage errors (e.g., quota exceeded)
    }
  }, [runnerState]);

  const loadDefinitions = useCallback(async () => {
    setLoadingDefs(true);
    setDefinitionsError(null);

    try {
      const response = await authorizedFetch(
        `${API_BASE_URL}/api/agents/definitions`
      );

      if (!response.ok) {
        throw new Error(
          `Failed to load agent definitions (${response.status})`
        );
      }

      const document = (await response.json()) as AgentDefinitionsDocument;
      setDefinitions(document);
    } catch (error) {
      const message =
        error instanceof Error
          ? error.message
          : "Unable to load agent definitions.";
      setDefinitionsError(message);
    } finally {
      setLoadingDefs(false);
    }
  }, [authorizedFetch]);

  useEffect(() => {
    if (!isAuthenticated) {
      setDefinitions(null);
      setDefinitionsError(null);
      return;
    }

    loadDefinitions();
  }, [isAuthenticated, loadDefinitions]);

  const handleDefinitionsUpdated = (document: AgentDefinitionsDocument) => {
    setDefinitions(document);
  };

  const handleReloadDefinitions = useCallback(async () => {
    if (!isAuthenticated) {
      return;
    }

    await loadDefinitions();
  }, [isAuthenticated, loadDefinitions]);

  const content = useMemo(() => {
    if (authLoading) {
      return (
        <div className="rounded-md border border-dashed border-border bg-card/40 p-8 text-center text-sm text-foreground/70">
          Checking Microsoft sign-in…
        </div>
      );
    }

    if (!isAuthenticated) {
      return (
        <div className="flex flex-col items-center gap-4 rounded-md border border-dashed border-border bg-card/40 p-10 text-center">
          <div>
            <h2 className="text-lg font-semibold">Sign in required</h2>
            <p className="mt-1 text-sm text-foreground/70">
              Connect with your Microsoft account to load workflows and edit
              agent definitions.
            </p>
          </div>
          {authError ? (
            <p className="text-sm text-destructive">{authError}</p>
          ) : null}
          <button
            type="button"
            className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition hover:bg-primary/90"
            onClick={() => login()}
          >
            Sign in with Microsoft
          </button>
        </div>
      );
    }

    if (activeView === "editor") {
      return (
        <AgentDefinitionsView
          definitions={definitions}
          loading={loadingDefs}
          error={definitionsError}
          onReload={handleReloadDefinitions}
          onDefinitionsUpdated={handleDefinitionsUpdated}
          apiBaseUrl={API_BASE_URL}
        />
      );
    }

    return (
      <AgentRunnerView
        definitions={definitions}
        loading={loadingDefs}
        error={definitionsError}
        apiBaseUrl={API_BASE_URL}
        runnerState={runnerState}
        setRunnerState={setRunnerState}
      />
    );
  }, [
    activeView,
    authError,
    authLoading,
    definitions,
    definitionsError,
    handleDefinitionsUpdated,
    handleReloadDefinitions,
    isAuthenticated,
    loadingDefs,
    login,
    runnerState,
  ]);

  const accountLabel =
    account?.name?.trim() ||
    account?.username ||
    account?.homeAccountId ||
    "Microsoft account";
  const authStatus = authLoading
    ? "Checking status…"
    : isAuthenticated
    ? "Signed in"
    : "Not signed in";

  return (
    <div className="min-h-screen bg-background text-foreground">
      <header className="border-b">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <div className="flex items-center gap-2">
            <img className="h-10" src="robot-color.svg" alt="Magic Agent" />
            <span className="text-lg font-semibold">Magic Agent Console</span>
          </div>
          <nav className="flex flex-wrap items-center gap-2">
            <HeaderButton
              active={activeView === "editor"}
              onClick={() => setActiveView("editor")}
            >
              Agent Definitions
            </HeaderButton>

            <HeaderButton
              active={activeView === "runner"}
              onClick={() => setActiveView("runner")}
            >
              Agent Runner
            </HeaderButton>
          </nav>

          <div className="flex flex-col items-end gap-1 text-right">
            <div className="text-sm font-medium">{accountLabel}</div>
            <div className="text-xs text-foreground/60">{authStatus}</div>
            {isAuthenticated ? (
              <button
                type="button"
                className="text-xs text-primary underline decoration-dotted underline-offset-2 hover:text-primary/80"
                onClick={() => logout()}
                disabled={authLoading}
              >
                Sign out
              </button>
            ) : null}
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-6 py-6">{content}</main>
    </div>
  );
}

interface HeaderButtonProps {
  active: boolean;
  children: React.ReactNode;
  onClick: () => void;
}

function HeaderButton({ active, children, onClick }: HeaderButtonProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`rounded-md px-3 py-2 text-sm font-medium transition-colors ${
        active
          ? "bg-primary text-primary-foreground"
          : "text-foreground/70 hover:bg-muted hover:text-foreground"
      }`}
    >
      {children}
    </button>
  );
}
export default App;
