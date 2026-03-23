# EDF Catálogo de Tablas para macOS

Aplicación nativa para macOS que permite gestionar catálogos de tablas conectándose a MongoDB Atlas.

## Versiones disponibles

### Versión Avalonia UI (Actual)

La versión actual está desarrollada con Avalonia UI, un framework multiplataforma para .NET que permite crear aplicaciones de escritorio nativas. Esta versión se encuentra en el directorio `EDFCatalogoAvalonia/`.

### Versión MAUI (Descontinuada)

Se intentó desarrollar una versión con .NET MAUI, pero debido a problemas persistentes con las cargas de trabajo para macOS, se optó por Avalonia UI como alternativa.

## Características

- Interfaz de usuario nativa para macOS
- Conexión a MongoDB Atlas para almacenamiento de datos
- Sistema de autenticación de usuarios
- Visualización y gestión de catálogos
- Edición y creación de catálogos
- Visualización de archivos multimedia

## Requisitos previos

Para compilar y ejecutar esta aplicación, necesitas:

1. .NET 9 SDK o superior
2. Visual Studio 2022, Visual Studio Code o Rider con soporte para Avalonia

## Estructura del proyecto

- **Configuration**: Configuración de la aplicación y MongoDB
- **Models**: Modelos de datos (Catalog, CatalogRow, User)
- **Services**: Servicios para acceso a datos y autenticación
- **ViewModels**: ViewModels para implementar el patrón MVVM
- **Views**: Vistas XAML de la interfaz de usuario

## Flujo de la aplicación

1. La aplicación inicia en la vista de login
2. Si el usuario ya está autenticado, se redirige a la vista de catálogos
3. Los usuarios normales solo ven sus propios catálogos
4. Los administradores pueden ver todos los catálogos
5. Se pueden crear nuevos catálogos, editar existentes y ver detalles

## Conexión a MongoDB Atlas

La aplicación se conecta a MongoDB Atlas usando la cadena de conexión configurada en `appsettings.json`.

## Compilación y ejecución

Para compilar la aplicación:
```bash
dotnet build EDFCatalogoAvalonia/EDFCatalogoAvalonia.csproj
```

Para ejecutar la aplicación:
```bash
dotnet run --project EDFCatalogoAvalonia/EDFCatalogoAvalonia.csproj
```

## Estado actual

- [x] Implementación de la interfaz de usuario básica con Avalonia UI
- [x] Configuración de la conexión a MongoDB Atlas
- [x] Implementación de autenticación de usuarios
- [x] Visualización de catálogos
- [x] Búsqueda de catálogos
- [x] Visualización de detalles de catálogo
- [x] Edición de catálogos
- [x] Creación de nuevos catálogos
- [x] Visualización mejorada de archivos multimedia
- [ ] Subida de archivos
- [ ] Eliminación de catálogos

## Próximos pasos

1. Implementar la funcionalidad para subir archivos
2. Agregar funcionalidad de eliminación de catálogos
3. Mejorar la experiencia de usuario con animaciones y transiciones
4. Implementar funcionalidades avanzadas de filtrado y ordenación
5. Agregar soporte para exportación de datos
