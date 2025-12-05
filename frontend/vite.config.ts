/// <reference types="vitest" />
/// <reference types="vite/client" />

import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import { fileURLToPath } from "node:url";

const srcDir = fileURLToPath(new URL("./src", import.meta.url));

export default defineConfig({
  plugins: [react() as []],
  resolve: {
    alias: {
      "@": srcDir,
    },
  },
  test: {
    globals: true,
    environment: "jsdom",
    setupFiles: "./vitest.setup.ts",
    coverage: {
      provider: "istanbul",
    },
  },
});
