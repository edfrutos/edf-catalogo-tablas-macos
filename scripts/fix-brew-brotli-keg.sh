#!/usr/bin/env bash
# Repara: Error: /opt/homebrew/opt/brotli is not a valid keg
# (Homebrew rechaza el keg aunque el enlace exista: migración, backup, Cellar corrupto).
#
# Evitar el problema: el Brewfile del repo usa "cask dotnet-sdk" (no formula dotnet).
# Este script solo sirve si quieres usar brew install dotnet / brotli de nuevo.
#
# Uso: ./scripts/fix-brew-brotli-keg.sh

set -euo pipefail

BREW="/opt/homebrew/bin/brew"
OPT_BROTLI="/opt/homebrew/opt/brotli"
CELLAR_BROTLI="/opt/homebrew/Cellar/brotli"

if [[ ! -x "$BREW" ]]; then
  echo "No está instalado Homebrew ARM en /opt/homebrew."
  exit 1
fi

echo ">>> 1/4 Desinstalando brotli (forzado, ignora errores si ya está roto)…"
"$BREW" uninstall --force brotli 2>/dev/null || true

echo ">>> 2/4 Eliminando enlace o carpeta en opt/brotli…"
if [[ -L "$OPT_BROTLI" ]] || [[ -f "$OPT_BROTLI" ]]; then
  rm -f "$OPT_BROTLI"
elif [[ -d "$OPT_BROTLI" ]]; then
  rm -rf "$OPT_BROTLI"
fi

echo ">>> 3/4 Eliminando Cellar/brotli si quedó a medias…"
if [[ -d "$CELLAR_BROTLI" ]]; then
  rm -rf "$CELLAR_BROTLI"
fi

echo ">>> 4/4 Instalación limpia de brotli…"
"$BREW" install brotli

"$BREW" list --versions brotli
ls -la "$OPT_BROTLI"

echo ""
echo "Listo. Ejecuta: brew bundle"
