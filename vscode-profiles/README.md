# 🔧 Gestión de Perfiles VS Code Multiplataforma

Este conjunto de archivos te permite mantener configuraciones consistentes de VS Code entre macOS y Linux Ubuntu, manejando las diferencias específicas de cada sistema operativo.

## 📁 Estructura de Archivos

```
vscode-profiles/
├── README.md                    # Este archivo
├── manage-profiles.sh           # Script automatizado de gestión
├── essential-extensions.md      # Lista de extensiones recomendadas
├── linux-local-settings.json   # Configuración específica para Linux
├── macos-default-settings.json # Configuración específica para macOS
└── backups/                     # Carpeta para backups automáticos
```

## 🎯 Objetivos

1. **Configuración común**: Mantener configuraciones compartidas entre ambos sistemas
2. **Separación de especificidades**: Manejar diferencias de rutas, terminales y herramientas específicas de cada OS
3. **Sincronización sencilla**: Scripts automatizados para aplicar configuraciones
4. **Backup automático**: Proteger configuraciones existentes

## 🚀 Uso Rápido

### Desde macOS (tu situación actual):

```bash
cd /Users/edefrutos/__Proyectos/edf_catalogotablas_net/vscode-profiles
./manage-profiles.sh
```

### Desde Linux Ubuntu:

```bash
# Primero, copia estos archivos a tu sistema Linux
# Luego ejecuta:
cd /ruta/donde/copiaste/los/archivos
./manage-profiles.sh
```

## 📋 Respuestas a tus Preguntas

### ¿Puedes crear el perfil linux-local desde macOS?

**✅ SÍ**, puedes crear y preparar todo desde macOS:

1. Los archivos de configuración son multiplataforma
2. Las extensiones se pueden definir por adelantado
3. VS Code Settings Sync maneja la sincronización automáticamente
4. Solo necesitas aplicar la configuración específica cuando estés en Linux

### ¿Cómo mantener configuraciones comunes?

**📝 Configuraciones Compartidas**:
- Formatters (Prettier, Black, etc.)
- GitHub Copilot settings
- Python linting y debugging
- Git configuración
- Spell checker
- Code quality tools

**🔧 Configuraciones Específicas por OS**:

**macOS**:
- `terminal.integrated.defaultProfile.osx`: "bash"
- Extensiones específicas como `wayou.vscode-icons-mac`
- Azure configuraciones (si desarrollas en cloud)
- Herramientas como Pieces, Tabnine

**Linux**:
- `terminal.integrated.defaultProfile.linux`: "bash"
- Extensiones como `vadimcn.vscode-lldb` (debugger C/C++)
- Configuraciones de paths específicas de Linux

## 🔄 Proceso de Sincronización

### Opción 1: Automática con Settings Sync

1. **Activa Settings Sync** en ambos sistemas
2. **Usa el script** para aplicar configuraciones específicas
3. **Las extensiones** se sincronizan automáticamente

### Opción 2: Manual

1. **Copia los archivos** a ambos sistemas
2. **Ejecuta el script** en cada sistema
3. **Aplica configuraciones** específicas manualmente

## 📦 Extensiones Recomendadas

### Core (Ambos sistemas):
- C# Dev Kit (.NET)
- Python (completo)
- GitHub Copilot
- Git tools (GitLens, Git History)
- Docker tools
- Prettier y formatters

### Específicas por OS:
- **macOS**: Icons específicos de Mac, herramientas de Azure
- **Linux**: LLDB debugger, herramientas específicas de Linux

## 🛠️ Comandos Útiles

```bash
# Ver perfil actual
code --list-extensions

# Crear backup manual
cp ~/.config/Code/User/settings.json ./backup-settings.json  # Linux
cp ~/Library/Application\\ Support/Code/User/settings.json ./backup-settings.json  # macOS

# Instalar extensión específica
code --install-extension publisher.extension-name

# Ver configuración actual
code --list-extensions > current-extensions.txt
```

## 🔍 Troubleshooting

### Problema: Extensiones no compatibles
**Solución**: Usa el archivo `essential-extensions.md` para instalar solo las compatibles

### Problema: Rutas diferentes
**Solución**: Las configuraciones específicas por OS manejan esto automáticamente

### Problema: Terminal por defecto
**Solución**: Configurado automáticamente (`bash` para ambos, pero separado por OS)

## 📝 Personalización

Para personalizar las configuraciones:

1. **Edita los archivos JSON** específicos de cada OS
2. **Mantén las secciones comunes** sincronizadas
3. **Usa comentarios** para identificar secciones específicas
4. **Ejecuta el script** para aplicar cambios

## 🎯 Próximos Pasos

1. ✅ **Ejecuta el script desde macOS** para crear el perfil base
2. 🔄 **Configura Settings Sync** en VS Code
3. 🐧 **Cuando uses Linux**: ejecuta el script para aplicar configuraciones específicas
4. 🔧 **Personaliza según necesites** editando los archivos JSON

¡Ya tienes todo listo para usar el mismo entorno de desarrollo en ambos sistemas sin conflictos!