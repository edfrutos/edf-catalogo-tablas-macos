#!/usr/bin/env bash
# Lista fórmulas y casks en Homebrew Intel (/usr/local) vs ARM (/opt/homebrew).
# Sirve para ver qué falta reinstalar en ARM tras migración iMac → Mac Studio.
#
# Uso: ./scripts/brew-intel-vs-arm-report.sh
#      ./scripts/brew-intel-vs-arm-report.sh --only-intel   # solo lo que sigue en Intel

set -eu

# Rutas habituales (Intel puede estar en /usr/local/Homebrew o solo bin/brew)
intel_brew=""
for candidate in /usr/local/Homebrew/bin/brew /usr/local/bin/brew; do
  if [[ -x "$candidate" ]]; then
    intel_brew="$candidate"
    break
  fi
done

arm_brew=""
[[ -x /opt/homebrew/bin/brew ]] && arm_brew="/opt/homebrew/bin/brew"

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

only_intel=false
[[ "${1:-}" == "--only-intel" ]] && only_intel=true

section() { printf '\n=== %s ===\n' "$1"; }

# Tras comm, quita entradas «solo Intel» que en ARM existen ya con otro nombre (renombres Homebrew / sustitutos).
adjust_intel_only_formulas() {
  : >"$tmp/out.intel-only.formula"
  : >"$tmp/resolved.formula"
  while IFS= read -r name || [[ -n "${name:-}" ]]; do
    [[ -z "$name" ]] && continue
    case "$name" in
      icu4c@75)
        if grep -qE '^(icu4c@77|icu4c@78|icu4c)$' "$tmp/arm.formula" 2>/dev/null; then
          echo "icu4c@75  →  ICU más reciente en ARM (icu4c@77, @78 o icu4c); @75 está deshabilitado en core" >>"$tmp/resolved.formula"
          continue
        fi
        ;;
      claws)
        if grep -qx 'claws-mail' "$tmp/arm.formula" 2>/dev/null; then
          echo "claws  →  claws-mail" >>"$tmp/resolved.formula"
          continue
        fi
        ;;
      dotstate)
        if grep -qx 'dotter' "$tmp/arm.formula" 2>/dev/null; then
          echo "dotstate  →  dotter" >>"$tmp/resolved.formula"
          continue
        fi
        ;;
      pass-cli)
        if grep -qx 'pass' "$tmp/arm.formula" 2>/dev/null; then
          echo "pass-cli  →  pass (password-store)" >>"$tmp/resolved.formula"
          continue
        fi
        ;;
      powershell-preview)
        if grep -qx 'powershell' "$tmp/arm.formula" 2>/dev/null; then
          echo "powershell-preview  →  powershell (core)" >>"$tmp/resolved.formula"
          continue
        fi
        ;;
    esac
    echo "$name" >>"$tmp/out.intel-only.formula"
  done <"$tmp/raw.intel-only.formula"
}

adjust_intel_only_casks() {
  : >"$tmp/out.intel-only.cask"
  while IFS= read -r name || [[ -n "${name:-}" ]]; do
    [[ -z "$name" ]] && continue
    case "$name" in
      google-cloud-sdk)
        if grep -qx 'gcloud-cli' "$tmp/arm.cask" 2>/dev/null; then
          echo "google-cloud-sdk  →  gcloud-cli" >>"$tmp/resolved.cask"
          continue
        fi
        ;;
      qlvideo)
        if grep -qx 'quicklook-video' "$tmp/arm.cask" 2>/dev/null; then
          echo "qlvideo  →  quicklook-video" >>"$tmp/resolved.cask"
          continue
        fi
        ;;
      tigervnc-viewer)
        if grep -qx 'tigervnc' "$tmp/arm.cask" 2>/dev/null; then
          echo "tigervnc-viewer  →  tigervnc" >>"$tmp/resolved.cask"
          continue
        fi
        ;;
    esac
    echo "$name" >>"$tmp/out.intel-only.cask"
  done <"$tmp/raw.intel-only.cask"
}

if [[ -z "$intel_brew" ]]; then
  section "Homebrew Intel"
  echo "No se encontró brew en /usr/local (Homebrew/bin/brew ni bin/brew ejecutable)."
else
  section "Homebrew Intel"
  echo "Ejecutable: $intel_brew"
  file "$intel_brew" 2>/dev/null || true
  arch_intel="$("$intel_brew" config 2>/dev/null | awk -F': ' '/^CPU:/ {print $2; exit}')"
  [[ -n "$arch_intel" ]] && echo "CPU (brew config): $arch_intel"
  { "$intel_brew" list --formula -1 2>/dev/null | sort -u >"$tmp/intel.formula"; } || true
  { "$intel_brew" list --cask -1 2>/dev/null | sort -u >"$tmp/intel.cask"; } || true
  echo "Fórmulas: $(wc -l <"$tmp/intel.formula" | tr -d ' ')"
  echo "Casks:    $(wc -l <"$tmp/intel.cask" | tr -d ' ')"
fi

if [[ -z "$arm_brew" ]]; then
  section "Homebrew ARM"
  echo "No está /opt/homebrew/bin/brew. Instala: https://docs.brew.sh/Installation"
  echo "(La comparación usará listas ARM vacías: todo lo de Intel aparecerá como «solo Intel».)"
  : >"$tmp/arm.formula"
  : >"$tmp/arm.cask"
else
  section "Homebrew ARM"
  echo "Ejecutable: $arm_brew"
  file "$arm_brew" 2>/dev/null || true
  { "$arm_brew" list --formula -1 2>/dev/null | sort -u >"$tmp/arm.formula"; } || true
  { "$arm_brew" list --cask -1 2>/dev/null | sort -u >"$tmp/arm.cask"; } || true
  echo "Fórmulas: $(wc -l <"$tmp/arm.formula" | tr -d ' ')"
  echo "Casks:    $(wc -l <"$tmp/arm.cask" | tr -d ' ')"
fi

if [[ -z "$intel_brew" ]]; then
  section "Comparación"
  echo "Sin instalación Intel detectada; nada que comparar."
  exit 0
fi

comm -23 "$tmp/intel.formula" "$tmp/arm.formula" | sort -u >"$tmp/raw.intel-only.formula"
comm -23 "$tmp/intel.cask" "$tmp/arm.cask" | sort -u >"$tmp/raw.intel-only.cask"
: >"$tmp/resolved.cask"
adjust_intel_only_formulas
adjust_intel_only_casks

section "Ya equivalente en ARM (nombre viejo solo en Intel; migración de ese paquete OK)"
echo "(Renombres/sustitutos que conoce este script; amplía el case en el .sh si te falta alguno.)"
if [[ -s "$tmp/resolved.formula" ]]; then
  echo "Fórmulas:"
  sort -u "$tmp/resolved.formula"
else
  echo "Fórmulas: (ninguna)"
fi
if [[ -s "$tmp/resolved.cask" ]]; then
  echo "Casks:"
  sort -u "$tmp/resolved.cask"
else
  echo "Casks: (ninguno)"
fi

section "Fórmulas solo en Intel (faltan en ARM o sin mapping conocido — instalar o sustituir)"
cat "$tmp/out.intel-only.formula"
intel_only_f=$(wc -l <"$tmp/out.intel-only.formula" | tr -d ' ')
[[ "$intel_only_f" -eq 0 ]] && echo "(ninguna)"

section "Casks solo en Intel (faltan en ARM o sin mapping conocido)"
cat "$tmp/out.intel-only.cask"
intel_only_c=$(wc -l <"$tmp/out.intel-only.cask" | tr -d ' ')
[[ "$intel_only_c" -eq 0 ]] && echo "(ninguna)"

if $only_intel; then
  exit 0
fi

section "Fórmulas solo en ARM"
comm -13 "$tmp/intel.formula" "$tmp/arm.formula" | tee "$tmp/out.arm-only.formula"
arm_only_f=$(wc -l <"$tmp/out.arm-only.formula" | tr -d ' ')
[[ "$arm_only_f" -eq 0 ]] && echo "(ninguna)"

section "Fórmulas en ambos"
comm -12 "$tmp/intel.formula" "$tmp/arm.formula"

section "Casks solo en ARM"
comm -13 "$tmp/intel.cask" "$tmp/arm.cask" | tee "$tmp/out.arm-only.cask"
arm_only_c=$(wc -l <"$tmp/out.arm-only.cask" | tr -d ' ')
[[ "$arm_only_c" -eq 0 ]] && echo "(ninguna)"

section "Casks en ambos"
comm -12 "$tmp/intel.cask" "$tmp/arm.cask"

if ! $only_intel; then
  section "Fragmento Brewfile: solo estaban en Intel (revisa y guarda en un fichero si quieres)"
  if [[ "$intel_only_f" -eq 0 && "$intel_only_c" -eq 0 ]]; then
    echo "No hay entradas que migrar (listas Intel ⊆ ARM)."
  else
    echo "# Generado por brew-intel-vs-arm-report.sh — revisa versiones y opciones (args) antes de brew bundle"
    while IFS= read -r f; do
      [[ -z "$f" ]] && continue
      echo "brew \"$f\""
    done <"$tmp/out.intel-only.formula"
    while IFS= read -r c; do
      [[ -z "$c" ]] && continue
      echo "cask \"$c\""
    done <"$tmp/out.intel-only.cask"
  fi
fi

section "¿Migración de paquetes lista para quitar Intel?"
if [[ "$intel_only_f" -eq 0 && "$intel_only_c" -eq 0 ]]; then
  echo "Sí: no queda nada «solo Intel» sin equivalente conocido en ARM (tras renombres/sustitutos arriba)."
  echo "Revisa igual: PATH, file \"\$(which …)\" por si algo sigue siendo x86_64 desde /usr/local, IDEs y scripts."
else
  echo "No del todo: quedan entradas en «solo Intel» sin mapping automático; instálalas en ARM o sustitúyelas antes de desinstalar /usr/local."
fi

section "Siguientes pasos (resumen)"
cat <<'EOS'
1. Prioriza PATH: export PATH="/opt/homebrew/bin:/opt/homebrew/sbin:$PATH"
2. Ignora avisos por renombre si aparecen en «Ya equivalente en ARM». Lo que siga en «solo Intel»:
   brew install <fórmula>  /  brew install --cask <cask>
   O copia el fragmento Brewfile del final (solo lo no resuelto por renombre).
3. Comprueba cada proyecto (node, python, docker, etc.) con: file "$(which <binario>)"
4. Cuando las secciones «solo Intel» estén vacías y no dependas de /usr/local:
   desinstalación oficial (lee el script antes de ejecutarlo):
   https://docs.brew.sh/FAQ#how-do-i-uninstall-homebrew
   En muchos Macs con resto Intel en /usr/local:
   /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/uninstall.sh)"
   (puede pedir sudo; si solo quieres quitar el prefijo Intel, sigue las indicaciones del uninstall.sh)
EOS
