import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

import "@fontsource-variable/inter/index.css";
import "@/index.css";

import { AuthProvider } from "@/auth/AuthProvider";
import App from "@/App.tsx";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <AuthProvider>
      <App />
    </AuthProvider>
  </StrictMode>
);
