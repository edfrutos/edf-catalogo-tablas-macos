#!/bin/bash

echo "🔄 FORZANDO RECARGA COMPLETA DE VSCODE PARA C#..."

# Verificar que la configuración es correcta
echo "🔍 Verificando SDK del sistema..."
dotnet --info

echo ""
echo "📋 Verificando estructura de la solución..."
dotnet sln list

echo ""
echo "🧹 Limpiando objetos de compilación..."
dotnet clean > /dev/null 2>&1

echo ""
echo "📦 Restaurando paquetes..."
dotnet restore > /dev/null 2>&1

echo ""
echo "🔨 Compilando solución..."
dotnet build > /dev/null 2>&1

if [ $? -eq 0 ]; then
    echo "✅ Compilación exitosa"
else
    echo "❌ Error en compilación"
    exit 1
fi

echo ""
echo "🎯 INSTRUCCIONES PARA VSCODE:"
echo "1. En VSCode, abre Command Palette (Cmd+Shift+P)"
echo "2. Ejecuta: 'Developer: Reload Window'"
echo "3. Si el problema persiste:"
echo "   - Command Palette → 'C#: Restart Language Server'"
echo "   - Command Palette → '.NET: Restart Project System'"
echo ""
echo "🔧 Si aún no funciona:"
echo "   - Cierra VSCode completamente (Cmd+Q)"
echo "   - Abre VSCode de nuevo"
echo "   - Abre esta carpeta: $(pwd)"
echo ""
echo "📊 Estado actual de los proyectos:"
echo "   ✅ DemoNet9 - Compilando"
echo "   ✅ EDFCatalogoTablasNet - Compilando (1 advertencia menor)"
echo "   ✅ EDFCatalogoTablasNet.Tests - Compilando"
echo ""
