#!/bin/bash

# Script de limpieza completa de extensiones .NET problemáticas
# Fecha: $(date)

echo "🧹 LIMPIEZA COMPLETA DE EXTENSIONES .NET PROBLEMÁTICAS"
echo "======================================================"

# Función para limpiar extensiones
cleanup_extensions() {
    local base_path="$1"
    local name="$2"

    if [ -d "$base_path" ]; then
        echo "🔍 Limpiando $name..."

        # Desinstalar extensión problemática
        if [ -d "$base_path/extensions/ms-dotnettools.vscode-dotnet-runtime-"* ]; then
            echo "  🗑️ Eliminando ms-dotnettools.vscode-dotnet-runtime..."
            rm -rf "$base_path/extensions/ms-dotnettools.vscode-dotnet-runtime-"*
        fi

        # Limpiar caché
        if [ -d "$base_path/User/workspaceStorage" ]; then
            echo "  🧽 Limpiando caché de workspace..."
            find "$base_path/User/workspaceStorage" -name "*dotnet*" -type d -exec rm -rf {} + 2>/dev/null || true
        fi

        # Limpiar logs
        if [ -d "$base_path/logs" ]; then
            echo "  📝 Limpiando logs..."
            find "$base_path/logs" -name "*dotnet*" -type f -delete 2>/dev/null || true
        fi

        echo "  ✅ $name limpiado"
    else
        echo "  ⏭️ $name no encontrado"
    fi
}

# Limpiar VSCode estándar
cleanup_extensions "$HOME/.vscode" "VSCode estándar"

# Limpiar VSCode Insiders (donde está la extensión problemática)
cleanup_extensions "$HOME/.vscode-insiders" "VSCode Insiders"

# Verificar estado después de limpieza
echo ""
echo "🔍 VERIFICACIÓN POST-LIMPIEZA:"
echo "==============================="

if find ~/.vscode* -name "*dotnet-runtime*" 2>/dev/null | grep -q "vscode-dotnet-runtime"; then
    echo "⚠️ Aún quedan referencias a dotnet-runtime"
    find ~/.vscode* -name "*dotnet-runtime*" 2>/dev/null
else
    echo "✅ Todas las referencias a dotnet-runtime eliminadas"
fi

echo ""
echo "🎯 PRÓXIMOS PASOS:"
echo "=================="
echo "1. Reinicia VSCode completamente"
echo "2. Abre el proyecto con: code ."
echo "3. Verifica que no aparezcan errores de extensiones"
echo ""
echo "✅ Limpieza completada. El sistema ahora usará únicamente el SDK del sistema."
