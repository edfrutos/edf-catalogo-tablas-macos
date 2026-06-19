#!/usr/bin/env bash
# Comprueba que cada brew/cask/tap del Brewfile exista para el Homebrew indicado (p. ej. ARM64).
# No instala nada. Uso:
#   PATH="/opt/homebrew/bin:/opt/homebrew/sbin:$PATH" ./scripts/validate-brewfile.sh
#   ./scripts/validate-brewfile.sh /ruta/al/Brewfile
#
# Nota: con "while read < Brewfile", brew no debe leer stdin del fichero; por eso </dev/null en cada brew.

set -euo pipefail

BREWFILE="${1:-${HOMEBREW_BUNDLE_FILE:-./Brewfile}}"
if [[ ! -f "$BREWFILE" ]]; then
  echo "No existe el fichero: $BREWFILE" >&2
  exit 1
fi

if [[ -n "${BREW:-}" ]]; then
  BREW_CMD="$BREW"
elif command -v brew >/dev/null 2>&1; then
  BREW_CMD="$(command -v brew)"
elif [[ -x /opt/homebrew/bin/brew ]]; then
  BREW_CMD="/opt/homebrew/bin/brew"
else
  echo "No se encontró brew (exporta PATH con /opt/homebrew/bin o define BREW=...)." >&2
  exit 1
fi

# Fórmula en tap estándar user/repo → repo GitHub user/homebrew-repo
tap_formula_on_github() {
  local tap="$1" formula="$2"
  local user="${tap%%/*}"
  local short="${tap#*/}"
  local gh="homebrew-${short}"
  local url_main="https://raw.githubusercontent.com/${user}/${gh}/main/Formula/${formula}.rb"
  local url_master="https://raw.githubusercontent.com/${user}/${gh}/master/Formula/${formula}.rb"
  curl -sf --max-time 8 -o /dev/null "$url_main" || curl -sf --max-time 8 -o /dev/null "$url_master"
}

tap_ok() {
  local name="$1"
  if "$BREW_CMD" tap-info "$name" </dev/null &>/dev/null; then
    return 0
  fi
  local user="${name%%/*}"
  local short="${name#*/}"
  local gh="homebrew-${short}"
  curl -sf --max-time 8 -o /dev/null "https://github.com/${user}/${gh}"
}

echo "Usando: $BREW_CMD"
echo "Brewfile: $BREWFILE"
echo

taps=()
line_no=0
while IFS= read -r line || [[ -n "$line" ]]; do
  ((line_no++)) || true
  trimmed="${line#"${line%%[![:space:]]*}"}"
  [[ "$trimmed" == \#* || -z "$trimmed" ]] && continue
  if [[ "$trimmed" =~ ^tap[[:space:]]+\"([^\"]+)\" ]]; then
    taps+=("${BASH_REMATCH[1]}")
  fi
done < "$BREWFILE"

bad=0
line_no=0
while IFS= read -r line || [[ -n "$line" ]]; do
  ((line_no++)) || true
  trimmed="${line#"${line%%[![:space:]]*}"}"
  [[ "$trimmed" == \#* || -z "$trimmed" ]] && continue

  if [[ "$trimmed" =~ ^brew[[:space:]]+\"([^\"]+)\" ]]; then
    name="${BASH_REMATCH[1]}"
    if "$BREW_CMD" info --formula "$name" </dev/null &>/dev/null; then
      continue
    fi
    found=false
    for t in "${taps[@]}"; do
      if tap_formula_on_github "$t" "$name"; then
        found=true
        break
      fi
    done
    if ! $found; then
      echo "Línea $line_no: formula no disponible: brew \"$name\""
      sug=$("$BREW_CMD" search --formula "$name" </dev/null 2>/dev/null | head -5 || true)
      [[ -n "$sug" ]] && echo "  búsqueda: $sug"
      ((bad++)) || true
    fi
  elif [[ "$trimmed" =~ ^cask[[:space:]]+\"([^\"]+)\" ]]; then
    name="${BASH_REMATCH[1]}"
    if ! "$BREW_CMD" info --cask "$name" </dev/null &>/dev/null; then
      echo "Línea $line_no: cask no disponible: cask \"$name\""
      sug=$("$BREW_CMD" search --cask "$name" </dev/null 2>/dev/null | head -5 || true)
      [[ -n "$sug" ]] && echo "  búsqueda: $sug"
      ((bad++)) || true
    fi
  elif [[ "$trimmed" =~ ^tap[[:space:]]+\"([^\"]+)\" ]]; then
    name="${BASH_REMATCH[1]}"
    if ! tap_ok "$name"; then
      echo "Línea $line_no: tap no encontrado (local ni en github.com/*/homebrew-*): tap \"$name\""
      ((bad++)) || true
    fi
  fi
done < "$BREWFILE"

echo
if (( bad == 0 )); then
  echo "OK: entradas brew/cask/tap reconocidas por este Homebrew (y taps del fichero en GitHub si aplica)."
  exit 0
fi

echo "Fallos: $bad (corrige el Brewfile o añade taps antes de brew bundle)."
exit 1
