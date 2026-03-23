#!/bin/bash

# Script para compilar y empaquetar la aplicación Swift para macOS

set -e  # Salir si hay algún error

# Colores para los mensajes
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== Compilando EDFCatalogoSwift para macOS ===${NC}"

# Directorio del proyecto
PROJECT_DIR="$(pwd)"
APP_NAME="EDF Catálogo de Tablas"
BUILD_DIR="${PROJECT_DIR}/.build"
RELEASE_DIR="${BUILD_DIR}/release/EDFCatalogoSwift"
APP_DIR="${PROJECT_DIR}/bin/${APP_NAME}.app"
RESOURCES_DIR="${APP_DIR}/Contents/Resources"

# Limpiar directorios anteriores
echo -e "${YELLOW}Limpiando directorios anteriores...${NC}"
rm -rf "${BUILD_DIR}" "${APP_DIR}" "${PROJECT_DIR}/bin"
mkdir -p "${PROJECT_DIR}/bin"

# Compilar el proyecto
echo -e "${YELLOW}Compilando el proyecto...${NC}"
swift build -c release

# Crear estructura de la aplicación
echo -e "${YELLOW}Creando estructura de la aplicación...${NC}"
mkdir -p "${APP_DIR}/Contents/MacOS"
mkdir -p "${RESOURCES_DIR}/Assets"

# Copiar el ejecutable
echo -e "${YELLOW}Copiando el ejecutable...${NC}"
cp "${RELEASE_DIR}" "${APP_DIR}/Contents/MacOS/EDFCatalogoSwift"

# Copiar recursos
echo -e "${YELLOW}Copiando recursos...${NC}"
cp "${PROJECT_DIR}/Resources/favicon_chula.jpeg" "${RESOURCES_DIR}/Assets/"

# Crear Info.plist
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
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
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
    <key>NSHumanReadableCopyright</key>
    <string>Copyright © 2023 EDF. All rights reserved.</string>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
PLIST

# Crear script launcher.sh
echo -e "${YELLOW}Creando script launcher.sh...${NC}"
cat > "${APP_DIR}/Contents/MacOS/launcher.sh" << 'LAUNCHER'
#!/bin/bash

# Obtener el directorio del script
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Variables AWS/S3: no incluir credenciales en el repositorio. Opcionalmente,
# coloca un archivo .env (solo en tu máquina) en Contents/Resources/ del .app
# o exporta AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_REGION y S3_BUCKET_NAME.
ENV_FILE="${DIR}/../Resources/.env"
if [[ -r "${ENV_FILE}" ]]; then
  set -a
  # shellcheck source=/dev/null
  source "${ENV_FILE}"
  set +a
fi

# Ejecutar la aplicación
exec "${DIR}/EDFCatalogoSwift"
LAUNCHER

# Dar permisos de ejecución al launcher
chmod +x "${APP_DIR}/Contents/MacOS/launcher.sh"

# Crear icono de la aplicación
echo -e "${YELLOW}Creando icono de la aplicación...${NC}"
mkdir -p "${PROJECT_DIR}/tmp_iconset.iconset"
cp "${PROJECT_DIR}/Resources/favicon_chula.jpeg" "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.jpg"

# Convertir JPEG a PNG
sips -s format png "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.jpg" --out "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.png"

# Crear diferentes tamaños de iconos
sips -z 16 16 "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.png" --out "${PROJECT_DIR}/tmp_iconset.iconset/icon_16x16.png"
sips -z 32 32 "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.png" --out "${PROJECT_DIR}/tmp_iconset.iconset/icon_16x16@2x.png"
sips -z 32 32 "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.png" --out "${PROJECT_DIR}/tmp_iconset.iconset/icon_32x32.png"
sips -z 64 64 "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.png" --out "${PROJECT_DIR}/tmp_iconset.iconset/icon_32x32@2x.png"
sips -z 128 128 "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.png" --out "${PROJECT_DIR}/tmp_iconset.iconset/icon_128x128.png"
sips -z 256 256 "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.png" --out "${PROJECT_DIR}/tmp_iconset.iconset/icon_128x128@2x.png"
sips -z 256 256 "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.png" --out "${PROJECT_DIR}/tmp_iconset.iconset/icon_256x256.png"
sips -z 512 512 "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.png" --out "${PROJECT_DIR}/tmp_iconset.iconset/icon_256x256@2x.png"
cp "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512.png" "${PROJECT_DIR}/tmp_iconset.iconset/icon_512x512@2x.png"

# Crear el archivo .icns
iconutil -c icns "${PROJECT_DIR}/tmp_iconset.iconset" -o "${RESOURCES_DIR}/AppIcon.icns"

# Limpiar archivos temporales
rm -rf "${PROJECT_DIR}/tmp_iconset.iconset"

# Firmar la aplicación (firma ad-hoc)
echo -e "${YELLOW}Firmando la aplicación...${NC}"
codesign --force --deep --sign - "${APP_DIR}"

# Quitar atributos de cuarentena
echo -e "${YELLOW}Quitando atributos de cuarentena...${NC}"
xattr -cr "${APP_DIR}"

echo -e "${GREEN}=== Compilación completada ===${NC}"
echo -e "${GREEN}La aplicación está disponible en: ${APP_DIR}${NC}"
echo -e "${YELLOW}Para ejecutar la aplicación, haga doble clic en ella desde Finder${NC}"
echo -e "${YELLOW}AWS/S3 en el .app: copia tu \`.env\` (solo local) a:${NC}"
echo -e "  ${RESOURCES_DIR}/.env"
