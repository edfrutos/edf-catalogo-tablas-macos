#!/usr/bin/env bash
# Desinstala Homebrew instalado bajo /usr/local (típico de Intel / Rosetta).
# NO toca /opt/homebrew (Apple Silicon).
#
# Usa el script oficial: https://github.com/Homebrew/install
#
# Antes:
#   - Terminal nativa arm64: uname -m  → arm64
#   - PATH: export PATH="/opt/homebrew/bin:/opt/homebrew/sbin:$PATH"
#   - which brew → /opt/homebrew/bin/brew
#   - Opcional: ./scripts/brew-intel-vs-arm-report.sh (que «solo Intel» quede vacío)
#
# La caché ~/Library/Caches/Homebrew la comparten ambos Homebrew; por eso usamos
# --skip-cache-and-logs (puedes quitarlo si quieres limpiar caché por completo).
#
# Uso:
#   ./scripts/uninstall-homebrew-usrlocal.sh           # solo simulación (dry-run), como tu usuario
#   ./scripts/uninstall-homebrew-usrlocal.sh --run     # desinstala con sudo bash (igual que el comando manual)
#   NONINTERACTIVE=1 ./scripts/uninstall-homebrew-usrlocal.sh --run   # sin preguntas (implica --force)
#
# Es el mismo uninstall.sh oficial que
#   sudo bash /tmp/uninstall-homebrew.sh --path=/usr/local --skip-cache-and-logs
# Aquí solo añadimos: preflight, symlink /usr/local/bin/brew si falta, y dry-run antes de --run.
#
# IMPORTANTE: el uninstall.sh de Homebrew usa «sudo» incluso en --dry-run (comprueba permisos y
# ejecuta find bajo /usr/local). Pedir contraseña en la simulación es normal. Si falla con
# «Need sudo access», tu usuario debe ser administrador (Ajustes → Usuarios) o la contraseña fue incorrecta.
#
# Si ves «Failed to locate Homebrew!» con --path=/usr/local: falta /usr/local/bin/brew aunque exista
# /usr/local/Homebrew/bin/brew. Este script recrea el symlink automáticamente tras sudo -v.

set -euo pipefail

UNINSTALL_URL="https://raw.githubusercontent.com/Homebrew/install/HEAD/uninstall.sh"
PATH_LOCAL="/usr/local"

OFFICIAL_TMP="$(mktemp)"
trap 'rm -f "$OFFICIAL_TMP"' EXIT

fetch_official() {
  curl -fsSL "$UNINSTALL_URL" -o "$OFFICIAL_TMP"
}

# Dry-run: como tu usuario (lista coherente con $HOME; sudo lo pide el propio uninstall.sh donde haga falta).
run_uninstall_dryrun() {
  fetch_official
  bash "$OFFICIAL_TMP" --path="$PATH_LOCAL" --skip-cache-and-logs --dry-run "$@"
}

# Desinstalación real: sudo bash evita Permission denied en /etc/paths.d y similares (ver FAQ Homebrew).
run_uninstall_real() {
  fetch_official
  sudo bash "$OFFICIAL_TMP" --path="$PATH_LOCAL" --skip-cache-and-logs "$@"
}

echo "=== Preflight ==="
echo "uname -m: $(uname -m)"
echo "which brew: $(command -v brew 2>/dev/null || echo '(no en PATH)')"
if [[ -x /opt/homebrew/bin/brew ]]; then
  echo "brew ARM: /opt/homebrew/bin/brew"
fi
if [[ -x /usr/local/Homebrew/bin/brew ]] || [[ -x /usr/local/bin/brew ]]; then
  echo "brew Intel (/usr/local): detectado"
else
  echo "Aviso: no veo brew ejecutable bajo /usr/local; el dry-run igual puede listar restos."
fi
echo ""

echo "=== Simulación (--dry-run) ==="
echo "El script oficial pide administrador (sudo) también en simulación. Si aparece Password:, introdúcela."
echo "Autenticando sudo una vez (cachea ticket para lo que sigue)…"
if ! sudo -v; then
  echo "No se pudo usar sudo. ¿Tu usuario es administrador del Mac? (Ajustes del sistema → Usuarios y grupos)"
  exit 1
fi

# uninstall.sh con --path=/usr/local exige /usr/local/bin/brew ejecutable (o .git en el prefijo).
# Tras un intento a medias a veces desaparece el symlink; sin él sale: Failed to locate Homebrew!
if [[ ! -x /usr/local/bin/brew ]] && [[ -x /usr/local/Homebrew/bin/brew ]]; then
  echo ">>> Recreando /usr/local/bin/brew → /usr/local/Homebrew/bin/brew (necesario para el desinstalador oficial)"
  sudo mkdir -p /usr/local/bin
  sudo ln -sf /usr/local/Homebrew/bin/brew /usr/local/bin/brew
  ls -la /usr/local/bin/brew
fi

run_uninstall_dryrun
echo ""

if [[ "${1:-}" != "--run" ]]; then
  echo "Solo fue simulación. Para desinstalar de verdad:"
  echo "  ./scripts/uninstall-homebrew-usrlocal.sh --run"
  echo "Equivalente manual:"
  echo "  curl -fsSL $UNINSTALL_URL -o /tmp/uninstall-homebrew.sh"
  echo "  sudo bash /tmp/uninstall-homebrew.sh --path=$PATH_LOCAL --skip-cache-and-logs"
  exit 0
fi

if [[ -n "${NONINTERACTIVE:-}" ]]; then
  echo "=== Desinstalación (NONINTERACTIVE: --force, con sudo bash) ==="
  run_uninstall_real --force
else
  echo "=== Desinstalación (interactiva; sudo bash; confirma en pantalla) ==="
  run_uninstall_real
fi

echo ""
echo "Hecho. Comprueba: test -x /usr/local/Homebrew/bin/brew && echo 'aún existe' || echo 'brew Intel ausente'"
echo "Revisa /etc/paths.d/homebrew si el PATH del sistema dejaba de resolver 'brew'."
