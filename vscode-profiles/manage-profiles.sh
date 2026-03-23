#!/bin/bash

# 🚀 Script para Gestión Automática de Perfiles VS Code
# Autor: EDF Proyectos
# Fecha: 2025-10-06
# Versión: 2.0 - Mejorado con configuración por workspace

set -e

# Colores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 Gestor de Perfiles VS Code Multiplataforma v2.0${NC}"
echo -e "${BLUE}====================================================${NC}"

# Detectar el sistema operativo
detect_os() {
    if [[ "$OSTYPE" == "darwin"* ]]; then
        echo "macOS"
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        echo "Linux"
    else
        echo "Desconocido"
    fi
}

OS=$(detect_os)
echo "📍 Sistema detectado: $OS"

# Definir rutas según el sistema operativo
if [[ "$OS" == "macOS" ]]; then
    VSCODE_USER_DIR="$HOME/Library/Application Support/Code/User"
    PROFILE_NAME="Default (macOS)"
elif [[ "$OS" == "Linux" ]]; then
    VSCODE_USER_DIR="$HOME/.config/Code/User"
    PROFILE_NAME="linux-local"
else
    echo "❌ Sistema operativo no soportado"
    exit 1
fi

echo "📁 Directorio VS Code: $VSCODE_USER_DIR"

# Función para instalar extensiones esenciales
install_essential_extensions() {
    echo "📦 Instalando extensiones esenciales..."
    
    # Core Development Tools
    code --install-extension ms-dotnettools.csdevkit
    code --install-extension ms-dotnettools.csharp
    code --install-extension ms-dotnettools.vscode-dotnet-pack
    code --install-extension ms-python.python
    code --install-extension ms-python.black-formatter
    code --install-extension ms-python.pylint
    code --install-extension ms-python.flake8
    code --install-extension ms-python.debugpy
    
    # Git & Version Control
    code --install-extension eamodio.gitlens
    code --install-extension donjayamanne.githistory
    code --install-extension github.vscode-pull-request-github
    code --install-extension mhutchie.git-graph
    
    # AI & Productivity
    code --install-extension github.copilot
    code --install-extension github.copilot-chat
    
    # Code Formatting & Linting
    code --install-extension esbenp.prettier-vscode
    code --install-extension ms-python.isort
    code --install-extension charliermarsh.ruff
    code --install-extension editorconfig.editorconfig
    
    # Web Development
    code --install-extension formulahendry.auto-close-tag
    code --install-extension formulahendry.auto-rename-tag
    code --install-extension christian-kohler.path-intellisense
    code --install-extension bradlc.vscode-tailwindcss
    
    # Docker & Containers
    code --install-extension ms-vscode-remote.vscode-remote-extensionpack
    code --install-extension ms-azuretools.vscode-docker
    code --install-extension docker.docker-vscode-extension
    
    # Language Support
    code --install-extension redhat.vscode-yaml
    code --install-extension ms-toolsai.jupyter
    code --install-extension ms-toolsai.jupyter-renderers
    
    # Code Quality
    code --install-extension streetsidesoftware.code-spell-checker
    code --install-extension streetsidesoftware.code-spell-checker-spanish
    code --install-extension usernamehw.errorlens
    
    # File Management & Navigation
    code --install-extension alefragnani.project-manager
    code --install-extension pkief.material-icon-theme
    code --install-extension vscode-icons-team.vscode-icons
    
    # Theme
    code --install-extension github.github-vscode-theme
    
    # Extensiones específicas por OS
    if [[ "$OS" == "macOS" ]]; then
        echo "🍎 Instalando extensiones específicas de macOS..."
        code --install-extension wayou.vscode-icons-mac
    elif [[ "$OS" == "Linux" ]]; then
        echo "🐧 Instalando extensiones específicas de Linux..."
        code --install-extension vadimcn.vscode-lldb
    fi
    
    echo "✅ Extensiones instaladas correctamente"
}

# Función para aplicar configuración específica
apply_settings() {
    echo "⚙️ Aplicando configuraciones específicas para $OS..."
    
    if [[ "$OS" == "macOS" ]]; then
        SETTINGS_FILE="./macos-default-settings.json"
    elif [[ "$OS" == "Linux" ]]; then
        SETTINGS_FILE="./linux-local-settings.json"
    fi
    
    if [[ -f "$SETTINGS_FILE" ]]; then
        echo "📄 Copiando configuración desde $SETTINGS_FILE"
        cp "$SETTINGS_FILE" "$VSCODE_USER_DIR/settings.json"
        echo "✅ Configuración aplicada correctamente"
    else
        echo "⚠️ Archivo de configuración no encontrado: $SETTINGS_FILE"
        echo "💡 Asegúrate de ejecutar este script desde el directorio que contiene los archivos de configuración"
    fi
}

# Función para crear backup
create_backup() {
    echo "💾 Creando backup de la configuración actual..."
    BACKUP_DIR="./backups/$(date +%Y%m%d_%H%M%S)_$OS"
    mkdir -p "$BACKUP_DIR"
    
    if [[ -f "$VSCODE_USER_DIR/settings.json" ]]; then
        cp "$VSCODE_USER_DIR/settings.json" "$BACKUP_DIR/"
    fi
    
    if [[ -f "$VSCODE_USER_DIR/keybindings.json" ]]; then
        cp "$VSCODE_USER_DIR/keybindings.json" "$BACKUP_DIR/"
    fi
    
    echo "✅ Backup creado en: $BACKUP_DIR"
}

# Menú principal
show_menu() {
    echo ""
    echo "Selecciona una opción:"
    echo "1) Crear backup de configuración actual"
    echo "2) Instalar extensiones esenciales"
    echo "3) Aplicar configuraciones específicas del OS"
    echo "4) Hacer todo (backup + extensiones + configuración)"
    echo "5) Salir"
    echo ""
    read -p "Opción: " option
    
    case $option in
        1)
            create_backup
            ;;
        2)
            install_essential_extensions
            ;;
        3)
            apply_settings
            ;;
        4)
            create_backup
            install_essential_extensions
            apply_settings
            echo ""
            echo "🎉 ¡Proceso completado!"
            echo "💡 Reinicia VS Code para aplicar todos los cambios"
            ;;
        5)
            echo "👋 ¡Hasta luego!"
            exit 0
            ;;
        *)
            echo "❌ Opción no válida"
            show_menu
            ;;
    esac
}

# Ejecutar menú
show_menu