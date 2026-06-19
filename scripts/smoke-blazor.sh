#!/usr/bin/env bash
# Smoke mínimo: compila Blazor Server, arranca `dotnet run`, comprueba GET /health y detiene el proceso.
# Requiere MongoDB alcanzable (p. ej. `appsettings.local.json` con `MongoDB:ConnectionString` real).
#
# Uso:
#   ./scripts/smoke-blazor.sh
#   SMOKE_TIMEOUT=120 ./scripts/smoke-blazor.sh
#   SMOKE_LOG=/tmp/blazor-smoke.log ./scripts/smoke-blazor.sh   # conservar log del servidor
#   SMOKE_ALWAYS_START=1 ./scripts/smoke-blazor.sh               # no reutilizar :5005 aunque /health responda

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT="$REPO_ROOT/EDFCatalogoTablasNet/EDFCatalogoTablasNet.csproj"
HEALTH_URL="http://127.0.0.1:5005/health"
TIMEOUT_SEC="${SMOKE_TIMEOUT:-90}"

# /health exige ping a MongoDB: status Healthy + mongoDb.ok true (serialización camelCase).
_health_json_ok() {
  local b="${1:-}"
  printf '%s' "$b" | grep -qE '"(status|Status)"[[:space:]]*:[[:space:]]*"Healthy"' || return 1
  printf '%s' "$b" | grep -qE '"mongoDb"[[:space:]]*:[[:space:]]*\{[^}]*"ok"[[:space:]]*:[[:space:]]*true'
}

if [[ ! -f "$PROJECT" ]]; then
  echo "[smoke-blazor] no se encuentra el proyecto: $PROJECT" >&2
  exit 1
fi

# Antes de tocar .NET: si :5005 ya responde, salir (sin avisos de runtime ni build).
if [[ -z "${SMOKE_ALWAYS_START:-}" ]]; then
  body_early="$(curl -sS --connect-timeout 1 --max-time 3 "$HEALTH_URL" 2>/dev/null || true)"
  if _health_json_ok "$body_early"; then
    echo "[smoke-blazor] OK — $HEALTH_URL (servidor ya en ejecución; SMOKE_ALWAYS_START=1 para forzar arranque propio)"
    exit 0
  fi
fi

# El proyecto es net9.0: hace falta runtime Microsoft.NETCore.App 9.x. El formula `dotnet` 10.x
# a veces solo trae 10; el cask Microsoft en /usr/local/share/dotnet suele incluir 9 + 10.
_dotnet_has_net9_runtime() {
  local root="$1"
  [[ -x "$root/dotnet" ]] || return 1
  "$root/dotnet" --list-runtimes 2>/dev/null | grep -qE 'Microsoft\.NETCore\.App 9\.'
}

if _dotnet_has_net9_runtime "/usr/local/share/dotnet"; then
  export DOTNET_ROOT="/usr/local/share/dotnet"
  export PATH="${DOTNET_ROOT}:${PATH}"
elif _dotnet_has_net9_runtime "/opt/homebrew/opt/dotnet/libexec"; then
  export DOTNET_ROOT="/opt/homebrew/opt/dotnet/libexec"
  export PATH="${DOTNET_ROOT}:${PATH}"
elif [[ -f "$SCRIPT_DIR/use-dotnet-arm64.sh" ]]; then
  # shellcheck disable=SC1091
  source "$SCRIPT_DIR/use-dotnet-arm64.sh"
fi
# net9.0 sin runtime 9.x instalado: LatestMajor permite ejecutar con 10.x (p. ej. formula Homebrew).
if ! dotnet --list-runtimes 2>/dev/null | grep -qE 'Microsoft\.NETCore\.App 9\.'; then
  export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-LatestMajor}"
  echo "[smoke-blazor] aviso: sin Microsoft.NETCore.App 9.x; usando DOTNET_ROLL_FORWARD=${DOTNET_ROLL_FORWARD} para dotnet run." >&2
fi

if [[ -n "${SMOKE_LOG:-}" ]]; then
  LOG="$SMOKE_LOG"
  KEEP_LOG=1
else
  LOG="$(mktemp -t smoke-blazor.XXXXXX)"
  KEEP_LOG=0
fi

cleanup() {
  if [[ -n "${SERVER_PID:-}" ]] && kill -0 "$SERVER_PID" 2>/dev/null; then
    kill "$SERVER_PID" 2>/dev/null || true
    wait "$SERVER_PID" 2>/dev/null || true
  fi
  if [[ "$KEEP_LOG" -eq 0 ]] && [[ -f "$LOG" ]]; then
    rm -f "$LOG"
  fi
}
trap cleanup EXIT

echo "[smoke-blazor] compilando…"
dotnet build "$PROJECT" --verbosity quiet || exit 1

echo "[smoke-blazor] arrancando servidor (log: $LOG), esperando /health hasta ${TIMEOUT_SEC}s…"
(cd "$REPO_ROOT" && dotnet run --project "$PROJECT" --no-build) >>"$LOG" 2>&1 &
SERVER_PID=$!

ok=0
for ((i = 1; i <= TIMEOUT_SEC; i++)); do
  if ! kill -0 "$SERVER_PID" 2>/dev/null; then
    echo "[smoke-blazor] dotnet terminó antes de responder. Últimas líneas del log:" >&2
    tail -40 "$LOG" >&2
    if grep -qiE 'address already in use|Address already in use' "$LOG" 2>/dev/null; then
      echo "[smoke-blazor] pista: puerto 5005 ocupado y /health no coincidió antes del arranque. Detén el proceso que lo usa o deja de definir SMOKE_ALWAYS_START si quieres reutilizar un servidor ya sano." >&2
    fi
    exit 1
  fi
  body="$(curl -sS --connect-timeout 2 --max-time 5 "$HEALTH_URL" 2>/dev/null || true)"
  if _health_json_ok "$body"; then
    ok=1
    break
  fi
  sleep 1
done

if [[ "$ok" -ne 1 ]]; then
  echo "[smoke-blazor] FAIL: sin respuesta Healthy en ${TIMEOUT_SEC}s. Últimas líneas:" >&2
  tail -40 "$LOG" >&2
  exit 1
fi

echo "[smoke-blazor] OK — $HEALTH_URL"
exit 0
