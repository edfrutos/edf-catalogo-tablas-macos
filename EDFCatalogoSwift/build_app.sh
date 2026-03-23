#!/bin/bash

# ============================================================
# Script para compilar y empaquetar EDFCatalogoSwift (macOS)
# usando credenciales AWS estándar (~/.aws/credentials y config)
# ============================================================

set -Eeuo pipefail
IFS=$'\n\t'

# --- Colores para mensajes ---
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # Sin color

echo -e "${GREEN}=== Compilando EDFCatalogoSwift para macOS ===${NC}"

# --- Rutas y nombres ---
PROJECT_DIR="$(pwd)"
APP_NAME="EDF Catálogo de Tablas"
BUILD_DIR="${PROJECT_DIR}/.build"
APP_DIR="${PROJECT_DIR}/bin/${APP_NAME}.app"
RESOURCES_DIR="${APP_DIR}/Contents/Resources"

# --- Limpiar compilaciones previas ---
echo -e "${YELLOW}Limpiando directorios anteriores...${NC}"
rm -rf "${BUILD_DIR}" "${APP_DIR}" "${PROJECT_DIR}/bin"
mkdir -p "${PROJECT_DIR}/bin"

# --- Compilar en modo release ---
echo -e "${YELLOW}Compilando el proyecto...${NC}"
swift build -c release

# Obtener ruta del binario compilado de forma fiable
BIN_DIR="$(swift build -c release --show-bin-path)"
BIN_PATH="${BIN_DIR}/EDFCatalogoSwift"

# --- Crear estructura básica del .app ---
echo -e "${YELLOW}Creando estructura de la aplicación...${NC}"
mkdir -p "${APP_DIR}/Contents/MacOS"
mkdir -p "${RESOURCES_DIR}/Assets"

# --- Copiar el ejecutable ---
echo -e "${YELLOW}Copiando el ejecutable...${NC}"
cp "${BIN_PATH}" "${APP_DIR}/Contents/MacOS/EDFCatalogoSwift"
chmod +x "${APP_DIR}/Contents/MacOS/EDFCatalogoSwift"

# --- Copiar recursos ---
echo -e "${YELLOW}Copiando recursos...${NC}"
cp "${PROJECT_DIR}/Resources/favicon_chula.jpeg" "${RESOURCES_DIR}/Assets/" 2>/dev/null || true

# --- Crear Info.plist ---
echo -e "${YELLOW}Creando Info.plist...${NC}"
cat > "${APP_DIR}/Contents/Info.plist" << 'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>launcher.sh</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundleIdentifier</key>
    <string>com.edf.catalogotablas</string>
    <key>CFBundleName</key>
    <string>EDF Catálogo de Tablas</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>CFBundleVersion</key>
    <string>1</string>
    <key>LSMinimumSystemVersion</key>
    <string>12.0</string>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
PLIST

# --- Crear launcher.sh actualizado ---
echo -e "${YELLOW}Creando launcher.sh...${NC}"
cat > "${APP_DIR}/Contents/MacOS/launcher.sh" << 'LAUNCHER'
#!/usr/bin/env bash
# launcher.sh — Lanza la app usando credenciales AWS estándar (~/.aws/*)

set -Eeuo pipefail
IFS=$'\n\t'

# Directorio del ejecutable dentro del .app
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# (Opcional) Cargar configuración no sensible desde un .env externo al .app
APP_ENV="${DIR}/../.."/"$(basename "$(dirname "${DIR}")")"
APP_ENV="${APP_ENV%.app}.env"
if [[ -f "$APP_ENV" ]]; then
  set -a
  source "$APP_ENV"
  set +a
fi

# NOTA: Las credenciales AWS se leen automáticamente desde:
#   ~/.aws/credentials
#   ~/.aws/config
# Si quieres usar un perfil diferente al [default], descomenta la siguiente línea:
# export AWS_PROFILE="default"

# Ejecutar la aplicación
exec "${DIR}/EDFCatalogoSwift"
LAUNCHER

chmod +x "${APP_DIR}/Contents/MacOS/launcher.sh"

# --- Crear icono (.icns) ---
echo -e "${YELLOW}Creando icono de la aplicación...${NC}"
mkdir -p "${PROJECT_DIR}/tmp_iconset.iconset"
cp "${PROJECT_DIR}/Resources/favicon_chula.jpeg" "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.jpg" 2>/dev/null || true
sips -s format png "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.jpg" --out "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.png" >/dev/null

# Crear diferentes tamaños
for size in 16 32 64 128 256 512; do
  sips -z $size $size "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.png" --out "${PROJECT_DIR}/tmp_iconset.iconset/icon_${size}x${size}.png" >/dev/null
done
cp "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.png" "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512@2x.png"

# Generar AppIcon.icns
iconutil -c icns "${PROJECT_DIR}/tmp_iconset.iconset" -o "${RESOURCES_DIR}/AppIcon.icns" 2>/dev/null || true
rm -rf "${PROJECT_DIR}/tmp_iconset.iconset"

# --- Firmar la aplicación (firma ad-hoc) ---
echo -e "${YELLOW}Firmando la aplicación...${NC}"
codesign --force --deep --sign - "${APP_DIR}"

# --- Quitar atributos de cuarentena ---
echo -e "${YELLOW}Quitando atributos de cuarentena...${NC}"
xattr -cr "${APP_DIR}"

# --- Final ---
echo -e "${GREEN}=== Compilación completada ===${NC}"
echo -e "${GREEN}Aplicación creada en:${NC} ${APP_DIR}"
echo -e "${YELLOW}Para ejecutar, haz doble clic en la app o usa:${NC}"
echo -e "${YELLOW}open \"${APP_DIR}\"${NC}"
