#!/usr/bin/env bash
# Ejecuta pruebas E2E (Playwright + NUnit).
# Por defecto arranca la app en el puerto 5107 (cámbialo con E2E_PORT) para no chocar con :5005.
# Para usar un servidor ya levantado: E2E_USE_EXISTING=1 E2E_BASE_URL=http://127.0.0.1:5005 ./scripts/e2e-test.sh
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

dotnet build edf_catalogotablas_net.sln -v q --nologo

STARTED_PID=""
if [[ "${E2E_USE_EXISTING:-}" == "1" ]]; then
  BASE_URL="${E2E_BASE_URL:-http://127.0.0.1:5005}"
  BASE_URL="${BASE_URL%/}"
  export E2E_BASE_URL="$BASE_URL"
  if ! curl -sf "$BASE_URL/health" >/dev/null 2>&1; then
    echo "[e2e-test] E2E_USE_EXISTING=1 pero $BASE_URL/health no responde." >&2
    exit 1
  fi
  echo "[e2e-test] Usando servidor existente: $BASE_URL"
else
  E2E_PORT="${E2E_PORT:-5107}"
  BASE_URL="http://127.0.0.1:$E2E_PORT"
  export E2E_BASE_URL="$BASE_URL"
  if command -v lsof >/dev/null 2>&1; then
    for pid in $(lsof -ti tcp:"$E2E_PORT" 2>/dev/null || true); do
      echo "[e2e-test] Liberando puerto $E2E_PORT (PID $pid)…"
      kill -9 "$pid" 2>/dev/null || true
    done
  fi
  echo "[e2e-test] Arrancando app en $BASE_URL (evita conflicto con :5005)…"
  export ASPNETCORE_URLS="$BASE_URL"
  dotnet run --project EDFCatalogoTablasNet/EDFCatalogoTablasNet.csproj --no-build --no-launch-profile &
  STARTED_PID=$!
  for _ in $(seq 1 90); do
    if curl -sf "$BASE_URL/health" >/dev/null 2>&1; then
      echo "[e2e-test] Servidor listo."
      break
    fi
    sleep 1
  done
  if ! curl -sf "$BASE_URL/health" >/dev/null 2>&1; then
    echo "[e2e-test] Timeout esperando /health en $BASE_URL" >&2
    kill "$STARTED_PID" 2>/dev/null || true
    exit 1
  fi
fi

dotnet test edf_catalogotablas_net.sln \
  --filter "Category=E2E" \
  --settings EDFCatalogoTablasNet.E2E/e2e.runsettings \
  --no-build \
  --nologo

STATUS=$?
if [[ -n "${STARTED_PID:-}" ]]; then
  kill "$STARTED_PID" 2>/dev/null || true
  wait "$STARTED_PID" 2>/dev/null || true
fi
exit "$STATUS"
