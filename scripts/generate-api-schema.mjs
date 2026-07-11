#!/usr/bin/env node
// Regenerates src/Steward.Web/src/api/schema.d.ts from the API's live OpenAPI doc.
//
// If nothing is already listening on :5000, this brings up Postgres, runs the API
// bound to :5000 just long enough to fetch its OpenAPI doc, then tears it down —
// so `npm run generate:api` works standalone without a pre-existing dev stack.

import { execSync, spawn } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const webDir = path.join(repoRoot, "src", "Steward.Web");
const apiUrl = "http://localhost:5000";
const openApiUrl = `${apiUrl}/openapi/v1.json`;
const apiReadyTimeoutMs = 90_000;
const pollIntervalMs = 1_000;

function log(message) {
  console.log(`[generate-api-schema] ${message}`);
}

async function isApiUp() {
  try {
    const res = await fetch(openApiUrl);
    return res.ok;
  } catch {
    return false;
  }
}

async function waitForApi(timeoutMs) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    if (await isApiUp()) return true;
    await new Promise((resolve) => setTimeout(resolve, pollIntervalMs));
  }
  return false;
}

function findPidsListeningOnPort(port) {
  try {
    if (process.platform === "win32") {
      const out = execSync(
        `powershell -NoProfile -Command "(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue).OwningProcess"`,
        { encoding: "utf8" },
      );
      return [...new Set(out.split(/\s+/).filter(Boolean))];
    }
    const out = execSync(`lsof -ti tcp:${port}`, { encoding: "utf8" });
    return [...new Set(out.split(/\s+/).filter(Boolean))];
  } catch {
    return [];
  }
}

function stopApi(child) {
  // `dotnet run` launches the real listener (the apphost, e.g. Steward.Api.exe) as a
  // process that killing the `dotnet run` pid/tree doesn't reliably reach on Windows —
  // observed leaving an orphaned listener behind. Kill by what's actually bound to the
  // port instead, which is robust regardless of dotnet's internal process shape.
  const port = new URL(apiUrl).port;
  const pids = new Set(findPidsListeningOnPort(port));
  if (child.pid) pids.add(String(child.pid));

  for (const pid of pids) {
    try {
      if (process.platform === "win32") {
        execSync(`taskkill /pid ${pid} /t /f`, { stdio: "ignore" });
      } else {
        process.kill(-Number(pid), "SIGTERM");
      }
    } catch {
      // already gone
    }
  }
}

function generateSchema() {
  execSync(`npx openapi-typescript ${openApiUrl} -o src/api/schema.d.ts`, {
    cwd: webDir,
    stdio: "inherit",
  });
}

async function main() {
  if (await isApiUp()) {
    log("API already running on :5000 — using it.");
    generateSchema();
    return;
  }

  log("Ensuring Postgres is up...");
  execSync("docker compose up -d --wait postgres", { cwd: repoRoot, stdio: "inherit" });

  log("Starting API on :5000...");
  const apiProcess = spawn("dotnet", ["run", "--project", "src/Steward.Api", "--urls", apiUrl], {
    cwd: repoRoot,
    env: { ...process.env, ASPNETCORE_ENVIRONMENT: "Development" },
    stdio: ["ignore", "pipe", "pipe"],
    detached: process.platform !== "win32",
  });

  let apiOutput = "";
  apiProcess.stdout.on("data", (chunk) => (apiOutput += chunk));
  apiProcess.stderr.on("data", (chunk) => (apiOutput += chunk));

  try {
    const ready = await waitForApi(apiReadyTimeoutMs);
    if (!ready) {
      console.error(apiOutput);
      throw new Error(`API did not become ready on :5000 within ${apiReadyTimeoutMs / 1000}s`);
    }

    log("API ready — generating schema...");
    generateSchema();
  } finally {
    log("Stopping API...");
    stopApi(apiProcess);
  }
}

main().catch((err) => {
  console.error(err.message ?? err);
  process.exit(1);
});
