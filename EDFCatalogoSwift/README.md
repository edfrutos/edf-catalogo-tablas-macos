# EDF Catálogo de Tablas - Aplicación nativa para macOS

Esta aplicación nativa para macOS permite gestionar catálogos de tablas con integración a MongoDB Atlas y AWS S3 para el almacenamiento de archivos multimedia.

## Características

- Interfaz nativa para macOS usando SwiftUI
- Autenticación de usuarios
- Gestión de catálogos (crear, editar, visualizar)
- Gestión de filas y columnas en catálogos
- Soporte para archivos multimedia (imágenes, documentos, videos)
- Integración con AWS S3 para almacenamiento de archivos
- Integración con MongoDB Atlas para almacenamiento de datos

## Requisitos

- macOS 12.0 o superior
- Xcode 13.0 o superior (para compilar)
- Swift 5.5 o superior

## Configuración del proyecto

1. Clona el repositorio y entra en el directorio Swift:
   ```
   git clone https://github.com/yourusername/edf_catalogotablas_macOS.git
   cd edf_catalogotablas_macOS/EDFCatalogoSwift
   ```

2. Crea el fichero de entorno a partir de la plantilla (no subas `.env` a git):
   ```
   cp .env.example .env
   ```
   Edita `.env` con tu URI de MongoDB, y si usas S3, las claves AWS, `AWS_REGION` (p. ej. `eu-south-2`), `S3_BUCKET_NAME` (p. ej. `edf-catalogotablas-sp`) y `USE_S3=true`.

3. Instala dependencias y compila:
   ```
   swift package resolve
   swift build
   ```

## Compilación y empaquetado

Para crear un archivo `.app` ejecutable desde Finder, utiliza el script de compilación incluido:

```
./build-macos.sh
```

Este script realizará las siguientes acciones:
- Compilar el proyecto en modo release
- Crear la estructura de directorios para la aplicación macOS
- Copiar el ejecutable y los recursos necesarios
- Crear el icono de la aplicación a partir del logo
- Firmar la aplicación (firma ad-hoc)
- Quitar atributos de cuarentena

La aplicación compilada estará disponible en `bin/EDF Catálogo de Tablas.app`.

**AWS/S3 en el `.app`:** el ejecutable embebido no incluye secretos. Para que la app firmada encuentre credenciales al abrirla desde Finder, copia tu `.env` a `Contents/Resources/.env` dentro del bundle (el script de build recuerda la ruta al terminar). Alternativa: exportar las variables en el entorno desde donde lances el binario.

## Credenciales de prueba

Para probar la aplicación, puedes utilizar las siguientes credenciales:

- **Email**: test@example.com
- **Contraseña**: password

## Estructura del proyecto

- `Sources/App`: Punto de entrada de la aplicación
- `Sources/Models`: Modelos de datos
- `Sources/Views`: Vistas de la interfaz de usuario
- `Sources/Services`: Servicios para MongoDB, S3 y Keychain
- `Sources/Utilities`: Utilidades y helpers
- `Resources`: Recursos como imágenes y archivos de configuración

## Configuración de MongoDB Atlas

La lectura de `MONGO_URI` y nombres de base se hace desde el entorno (fichero `.env` cargado en tiempo de ejecución). La lógica de cliente está en `Sources/Services/MongoService.swift`.

## Configuración de AWS S3

`S3Service.swift` usa variables de entorno (`AWS_*`, `S3_BUCKET_NAME`, `USE_S3`). Si falta algún valor esencial, el servicio puede operar en modo desactivado según `USE_S3`. Región y bucket por defecto en código están alineados con el entorno de despliegue actual (`eu-south-2`, bucket `edf-catalogotablas-sp`); conviene sobreescribirlos siempre con `.env`.

## Licencia

Este proyecto está licenciado bajo la licencia MIT. Consulta el archivo LICENSE para más detalles.
