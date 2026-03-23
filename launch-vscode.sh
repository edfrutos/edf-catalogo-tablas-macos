#!/bin/bash

# Script para lanzar VSCode con el SDK de .NET correcto
# Esto asegura que VSCode use el SDK del sistema en lugar del descargado por extensiones

export DOTNET_ROOT="/usr/local/Cellar/dotnet/9.0.5/libexec"
export PATH="/usr/local/bin:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1

echo "🔧 Configurando entorno .NET para VSCode..."
echo "DOTNET_ROOT: $DOTNET_ROOT"
echo "DOTNET Path: $(which dotnet)"
echo "DOTNET Version: $(dotnet --version)"

# Matar procesos de VSCode si están ejecutándose
echo "🛑 Cerrando VSCode si está ejecutándose..."
pkill -f "Visual Studio Code" || true
sleep 2

# Lanzar VSCode con las variables de entorno correctas
echo "🚀 Iniciando VSCode con SDK del sistema..."
code "/Users/edefrutos/__Proyectos/edf_catalogotablas_net"

echo "✅ VSCode lanzado con configuración de .NET SDK del sistema"
