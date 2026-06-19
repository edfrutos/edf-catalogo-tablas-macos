#!/usr/bin/env bash
# Instala los binarios del navegador para Playwright (Chromium) tras compilar el proyecto E2E.
# Requiere PowerShell Core (`pwsh`), que es lo que usa el script oficial `playwright.ps1` del paquete.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
dotnet build EDFCatalogoTablasNet.E2E/EDFCatalogoTablasNet.E2E.csproj -c Debug --nologo
OUT="$ROOT/EDFCatalogoTablasNet.E2E/bin/Debug/net9.0"
SH="$OUT/playwright.sh"
PS1="$OUT/playwright.ps1"
if [[ -x "$SH" ]]; then
  "$SH" install chromium
elif [[ -f "$PS1" ]]; then
  if ! command -v pwsh >/dev/null 2>&1; then
    echo "Instala PowerShell Core (brew install --cask powershell) o ejecuta manualmente:" >&2
    echo "  pwsh \"$OUT/playwright.ps1\" install chromium" >&2
    exit 1
  fi
  pwsh "$PS1" install chromium
else
  echo "No se encontró playwright.ps1 en $OUT. Ejecuta: dotnet build EDFCatalogoTablasNet.E2E/EDFCatalogoTablasNet.E2E.csproj" >&2
  exit 1
fi
echo "Playwright Chromium listo."
