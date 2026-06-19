#!/usr/bin/env bash
# Homebrew mezclando /usr/local (Intel/Rosetta) con /opt/homebrew (ARM) produce:
#   - Error: /opt/homebrew/opt/brotli is not a valid keg
#   - ffmpeg dependencies not built for x86_64 / brotli was built for arm64
#
# Causa habitual: symlink manual tipo /usr/local/opt/brotli -> ../opt/homebrew/...
# o PATH que mezcla prefijos.
#
# Uso: ./scripts/fix-brew-intel-arm-mixup.sh        # solo diagnóstico
#      ./scripts/fix-brew-intel-arm-mixup.sh --fix  # quita symlink cruzado brotli en /usr/local/opt

set -eu

fix=false
[[ "${1:-}" == "--fix" ]] && fix=true

echo "=== which brew ==="
command -v brew || true
which -a brew 2>/dev/null || true

echo ""
echo "=== Prefijos ==="
[[ -x /usr/local/Homebrew/bin/brew ]] && /usr/local/Homebrew/bin/brew --prefix 2>/dev/null || echo "(sin /usr/local/Homebrew/bin/brew)"
[[ -x /opt/homebrew/bin/brew ]] && /opt/homebrew/bin/brew --prefix 2>/dev/null || echo "(sin /opt/homebrew/bin/brew)"

echo ""
echo "=== /usr/local/opt/brotli (si cruza a ARM, rompe brew Intel) ==="
if [[ -L /usr/local/opt/brotli ]]; then
  ls -la /usr/local/opt/brotli
  python3 -c "import os; print('realpath:', os.path.realpath('/usr/local/opt/brotli'))" 2>/dev/null || true
elif [[ -e /usr/local/opt/brotli ]]; then
  ls -la /usr/local/opt/brotli
else
  echo "(no existe)"
fi

echo ""
echo "=== /opt/homebrew/opt/brotli ==="
ls -la /opt/homebrew/opt/brotli 2>/dev/null || echo "(no existe)"

echo ""
echo "=== uname / arch ==="
uname -m
arch 2>/dev/null || true

echo ""
if $fix; then
  if [[ -L /usr/local/opt/brotli ]]; then
    rp="$(python3 -c "import os; print(os.path.realpath('/usr/local/opt/brotli'))" 2>/dev/null || true)"
    if [[ "$rp" == *opt/homebrew* ]]; then
      echo ">>> Eliminando symlink cruzado /usr/local/opt/brotli -> ARM"
      sudo rm -f /usr/local/opt/brotli
      echo "Hecho. Reinstala brotli SOLO en el prefijo que uses para bundle:"
      echo "  # Solo ARM (recomendado en Mac Studio):"
      echo "  /opt/homebrew/bin/brew reinstall brotli"
      echo "  # O solo Intel (Rosetta), si aún usas /usr/local para todo:"
      echo "  arch -x86_64 /usr/local/Homebrew/bin/brew reinstall brotli"
    else
      echo "El enlace no apunta a /opt/homebrew; no toco nada automáticamente."
    fi
  else
    echo "No hay symlink en /usr/local/opt/brotli; --fix no aplica."
  fi
else
  cat <<'TXT'

--- Qué hacer en Mac Studio (recomendado) ---

1) NO ejecutes "brew bundle" con el brew de /usr/local si quieres stack ARM64 nativo.

2) Terminal sin Rosetta, uname -m → arm64, y:
   export PATH="/opt/homebrew/bin:/opt/homebrew/sbin:$PATH"
   hash -r
   cd <tu-repo>
   brew bundle

   Así todo va a /opt/homebrew/Cellar (arm64) y no se mezcla con /usr/local.

3) Si antes creaste enlace /usr/local/opt/brotli → Homebrew ARM, bórralo:
   ./scripts/fix-brew-intel-arm-mixup.sh --fix

4) Enlaces rotos con Docker.app / AWS CLI / python.org:
   brew link --overwrite awscli docker gh python@3.14   # según falle cada uno

5) Opcional: cuando todo esté en /opt/homebrew, desinstala Homebrew Intel:
   https://docs.brew.sh/FAQ#how-do-i-uninstall-homebrew

TXT
fi
