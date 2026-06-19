# EDF Catálogo de Tablas (macOS / .NET)

Monorepositorio con cliente nativo para macOS, aplicación web Blazor y utilidades. Los datos viven en
**MongoDB Atlas**; los archivos multimedia pueden servirse desde **AWS S3** (región y bucket
configurables por entorno).

## Contenido del repositorio

| Componente | Descripción |
| --- | --- |
| **`EDFCatalogoSwift/`** | App macOS (SwiftUI): catálogos, filas, multimedia vía S3 presignado. |
| **`EDFCatalogoTablasNet/`** | ASP.NET Core (Blazor Server): misma línea de producto en web. |
| **`CleanCatalogs/`** | CLI .NET: vaciar `catalogs` en MongoDB. URI: `MONGO_URI` o `MONGODB_URI` (ver sección). |

Documentación más detallada del cliente Swift: [`EDFCatalogoSwift/README.md`](EDFCatalogoSwift/README.md).

## Requisitos

- **Swift**: macOS 12+, Xcode compatible con Swift del `Package.swift`.
- **.NET**: el `global.json` pide como mínimo el SDK **9.0.106** y usa `rollForward: latestMajor`, de modo
  que también vale un SDK **10.x**. En el [`Brewfile`](Brewfile) se usa el **cask `dotnet-sdk`**
  (Microsoft), no el formula `dotnet`, para evitar el keg roto de **brotli** que arrastra el formula.
- Cuenta **MongoDB Atlas** y, si usas archivos en la app Swift, bucket **S3** en la región que definas.

### Apple Silicon y Homebrew (ARM64)

Tras migrar desde un Mac Intel, las fórmulas en `/usr/local` no se copian solas a **ARM64**. Casi todo
lo que tenías con `brew` en Intel tiene equivalente en `/opt/homebrew`:

- Instala lo mínimo: **`brew bundle`** (cask **`dotnet-sdk`** + **`pcre2`**). Si antes tenías el SDK como
  **`brew install dotnet`**, quítalo para no mezclar: `brew uninstall --force dotnet dotnet@9`.
- Pon **`/opt/homebrew/bin` delante** en el `PATH`. El cask deja el SDK en **`/usr/local/share/dotnet`**
  (binarios **arm64** del pkg de Microsoft). Usa **`source scripts/use-dotnet-arm64.sh`**, que define
  **`DOTNET_ROOT`** según lo que tengas instalado (formula o cask).
- Si aún tienes **`dotnet` de Intel** en `/usr/local/bin`, puede fallar NuGet por librerías como **brotli**
  enlazadas a rutas de Intel; usar el `dotnet` de ARM evita ese problema.
- Para reinstalar el resto de herramientas de tu lista antigua, instálalas poco a poco con
  **`brew install <nombre>`** (la mayoría existen en ARM). No hace falta reinstalar todo de golpe.

#### Ver qué sigue solo en Brew Intel y pasarlo a ARM (otros proyectos)

En la raíz del repo:

```bash
chmod +x scripts/brew-intel-vs-arm-report.sh
./scripts/brew-intel-vs-arm-report.sh
```

El script compara fórmulas y **casks** entre Homebrew **Intel** (`/usr/local/...`) y **ARM**
(`/opt/homebrew`), lista lo que solo está en Intel y al final imprime un **fragmento de Brewfile** para
instalar en ARM con `brew bundle --file tu-fichero` (revísalo: nombres de cask, opciones y versiones).

Listado compacto solo nombres “solo Intel”:

```bash
./scripts/brew-intel-vs-arm-report.sh --only-intel
```

Asegúrate de usar **`/opt/homebrew/bin/brew`** en terminal **arm64** (`uname -m`). Cuando ya no dependas
de `/usr/local` para desarrollo, puedes desinstalar Homebrew Intel siguiendo la
[FAQ oficial de Homebrew](https://docs.brew.sh/FAQ#how-do-i-uninstall-homebrew).

#### brew bundle: mezcla Intel/ARM, brotli y `/opt/homebrew/opt/brotli`

Si el log muestra **`/usr/local/Cellar`** (instalación “vieja”) **y** errores con
**`/opt/homebrew/opt/brotli`**, o **`brotli was built for arm64`** junto a **x86_64**, estás usando
**`brew` de `/usr/local`** mientras parte de las dependencias apuntan al prefijo **ARM**. Suele deberse a
un **symlink cruzado** (p. ej. `/usr/local/opt/brotli` → `/opt/homebrew/...`) que no debes mantener.

**En Mac Studio lo coherente es:** migrar el gran `Brewfile` con **solo** el brew ARM:

```bash
uname -m                    # debe ser arm64
export PATH="/opt/homebrew/bin:/opt/homebrew/sbin:$PATH"
hash -r
which brew                  # debe ser /opt/homebrew/bin/brew
./scripts/fix-brew-intel-arm-mixup.sh        # diagnóstico
./scripts/fix-brew-intel-arm-mixup.sh --fix  # quita symlink brotli cruzado en /usr/local/opt (pide sudo)
/opt/homebrew/bin/brew reinstall brotli
brew bundle
```

Mientras sigas lanzando **`brew bundle` desde el `brew` de `/usr/local`**, seguirás peleando con mezcla de
arquitecturas.

**Conflictos de `brew link`** (`aws`, `docker`, `gh`, `python@3.14`): o bien eliges el binario de
Homebrew (`brew link --overwrite <formula>`), o quitas/ajustas el instalador oficial que ocupa
`/usr/local/bin` (AWS CLI clásico, Docker Desktop, python.org).

#### Cask dotnet-sdk: error dnx / pkg x64 en Apple Silicon

El cask enlaza un binario **obsoleto** (`dnx`) que el SDK **ya no incluye**; Homebrew falla al final aunque
el `.pkg` se haya instalado. El [`Brewfile`](Brewfile) instala el cask con **`--no-binaries`**: deja el SDK
en **`/usr/local/share/dotnet`** y evita ese paso. Usa **`source scripts/use-dotnet-arm64.sh`** para tener
`dotnet` en el `PATH`.

Si el instalador dice **Microsoft .NET SDK … (x64)** en un Mac Studio, estás usando **Rosetta** o un
`brew` de Intel. Abre una terminal **nativa arm64** (`uname -m` → `arm64`) y usa **`/opt/homebrew/bin/brew`**.
Tras un intento fallido puedes limpiar y repetir:

```bash
brew uninstall --cask dotnet-sdk 2>/dev/null || true
# solo si no necesitas otra instalación en /usr/local/share/dotnet:
# sudo rm -rf /usr/local/share/dotnet
brew bundle
```

Comprueba: `file /usr/local/share/dotnet/dotnet` → debe incluir **arm64**.

#### `brew bundle` falla: «/opt/homebrew/opt/brotli is not a valid keg»

El **formula** `dotnet` depende de **`brotli`**; si el keg está corrupto (muy habitual tras backup iMac →
Studio), Homebrew falla. **Solución recomendada en este repo:** el [`Brewfile`](Brewfile) usa
**`cask "dotnet-sdk"`**, que no pasa por ese keg.

```bash
brew uninstall --force dotnet dotnet@9 2>/dev/null || true
brew bundle
source scripts/use-dotnet-arm64.sh
dotnet --version
```

Si quieres seguir usando el **formula** `dotnet` + `brotli`, intenta reparar el keg:

```bash
./scripts/fix-brew-brotli-keg.sh
brew bundle   # solo si vuelves a poner brew "dotnet" y brew "brotli" en el Brewfile
```

## Configuración y secretos (no versionar)

- **Swift**: en `EDFCatalogoSwift/`, copia `.env.example` como `.env` y rellena valores reales. Ese archivo
  está en `.gitignore`.
- **.NET**: copia `EDFCatalogoTablasNet/appsettings.local.json.EXAMPLE` a `appsettings.local.json`
  (ignorado por git) o usa variables de entorno según tu despliegue. La web **no** lee el `.env` de Swift:
  la URI y el nombre de base deben estar en `MongoDB` de appsettings (misma Atlas/base que en `.env` si
  quieres un solo cluster).
- No subas copias locales de variables con el nombre `# Variables de entorno para EDF Catálogo.txt` ni
  claves en scripts; el repositorio ignora patrones habituales (`.env`, `.wakatime.cfg`, etc.).

### Blazor y MongoDB (nombre de base y datos legacy)

- **`MongoDB:DatabaseName`** en `appsettings` / `appsettings.local.json` debe coincidir **exactamente** con
  el nombre de la base en Atlas/Compass (p. ej. `edf_catalogotablas` con guión bajo **no** es lo mismo que
  `edf-catalogotablas` con guión). Colección por defecto: `catalogs` (`MongoDB:CatalogsCollection`).
- La aplicación tolera documentos **antiguos** con campos mezclados (`name`/`Name`, `columns`/`Headers`,
  fechas como texto, etc.) para **listar** catálogos. Los arrays **`rows`** con formato muy distinto al modelo
  actual pueden **no mostrarse** en detalle hasta migrar al esquema `Rows` / `CatalogRow` de la web.
- Los roles de usuario se normalizan en login (p. ej. `admin` y `Admin` como administrador en UI y
  filtros de catálogos).

### Variables relevantes (Swift / `.env`)

| Variable | Uso |
| --- | --- |
| `MONGO_URI`, `MONGO_DB` / `MONGODB_DB` | Conexión y nombre de base MongoDB. |
| `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY` | Credenciales S3 (solo si `USE_S3=true`). |
| `AWS_REGION` | Por defecto en código suele ser `eu-south-2` si no se define. |
| `S3_BUCKET_NAME` | Ejemplo de bucket: `edf-catalogotablas-sp`. |
| `USE_S3` | `true` / `false`. |

### App empaquetada (`.app` con `build-macos.sh`)

El launcher puede cargar un `.env` colocado en **`Contents/Resources/.env`** dentro del bundle (solo en tu
máquina o entorno controlado). Tras compilar, el script indica la ruta. No incluyas credenciales en el
repositorio.

## Utilidad `CleanCatalogs`

Hace **ping** al cluster, cuenta documentos y, salvo modo simulación, **borra todos** los documentos de la
colección configurada (por defecto `catalogs` en la base `edf_catalogotablas`).

**Obligatorio (una de dos):** `MONGO_URI` o `MONGODB_URI` con la cadena de conexión (no commitear
credenciales).

**Opcional:** `MONGO_DB` o `MONGODB_DB` (base; por defecto `edf_catalogotablas`),
`CLEAN_CATALOGS_COLLECTION` o `MONGO_CATALOGS_COLLECTION` (colección; por defecto `catalogs`).

```bash
export MONGO_URI='mongodb+srv://USUARIO:PASSWORD@cluster.../'
dotnet run --project CleanCatalogs/CleanCatalogs.csproj -- --dry-run   # solo recuento
dotnet run --project CleanCatalogs/CleanCatalogs.csproj                # borra de verdad
dotnet run --project CleanCatalogs/CleanCatalogs.csproj -- --help       # ayuda en consola
```

Usa **`--`** antes de los argumentos del programa (`--dry-run`, `--help`) al invocar con
`dotnet run`.

## Compilar la web (.NET)

En Apple Silicon, si usas .NET de Homebrew ARM:

```bash
source scripts/use-dotnet-arm64.sh
dotnet build edf_catalogotablas_net.sln
dotnet test EDFCatalogoTablasNet.Tests/EDFCatalogoTablasNet.Tests.csproj \
  --filter "FullyQualifiedName!~.Integration."
# Pruebas contra Mongo (namespace *.Integration): export MongoDB__ConnectionString=… y
# dotnet test … --filter "FullyQualifiedName~.Integration."
dotnet run --project EDFCatalogoTablasNet/EDFCatalogoTablasNet.csproj
```

Tras arrancar, la interfaz suele estar en **`http://localhost:5005`**. Si el seeder de MongoDB ha podido
ejecutarse, el usuario de prueba es **`admin@edf.com`** / **`admin123`** (solo desarrollo; no uses estas
credenciales en producción).

Si un usuario en Mongo tiene un hash generado **fuera** de esta app, el login fallará hasta alinear el hash
(SHA256 + sal `EDF_SALT_2024`). **Una vez**, exporta variables **solo en tu terminal** (no las subas a git);
el seeder aplica un `$set` del hash al arrancar **en cualquier** `ASPNETCORE_ENVIRONMENT`:

```bash
export EDF_DEV_PASSWORD_RESET_EMAIL=edfrutos@gmail.com
export EDF_DEV_PASSWORD_RESET_PLAIN='(tu nueva contraseña)'
dotnet run --project EDFCatalogoTablasNet/EDFCatalogoTablasNet.csproj
```

Cuando veas el aviso en consola, para el proceso,
`unset EDF_DEV_PASSWORD_RESET_EMAIL EDF_DEV_PASSWORD_RESET_PLAIN` y vuelve a arrancar.

**Smoke mínimo** (compila, arranca el servidor, comprueba `GET /health` —ping a MongoDB de la app— y lo
detiene; requiere Mongo alcanzable):

```bash
./scripts/smoke-blazor.sh
```

Variables opcionales: `SMOKE_TIMEOUT` (segundos, por defecto 90), `SMOKE_LOG` (conservar log del servidor),
`SMOKE_ALWAYS_START=1` (arrancar siempre otro proceso aunque `:5005` ya responda a `/health`). Si solo tienes
runtime .NET 10.x, el script define `DOTNET_ROLL_FORWARD=LatestMajor` para ejecutar el target `net9.0`.

**E2E (Playwright):** proyecto `EDFCatalogoTablasNet.E2E` (NUnit): health, login, `/catalogs` y **crear
catálogo** en `/create`. Requiere **Mongo** como el smoke y **PowerShell Core** (`pwsh`) para instalar
Chromium la primera vez. **Plan manual ampliado:** ver `NOTEBOOK.md` §10.

```bash
./scripts/e2e-playwright-install.sh   # una vez por máquina / tras actualizar Microsoft.Playwright
./scripts/e2e-test.sh                 # arranca la app en :5107 por defecto y ejecuta las pruebas
```

Variables útiles: `E2E_BASE_URL`, `E2E_EMAIL`, `E2E_PASSWORD`, `E2E_SKIP=1` (omitir), `E2E_PORT` (puerto si
no usas el predeterminado), `E2E_USE_EXISTING=1` para apuntar a un servidor ya en marcha (p. ej. `:5005`).
También: `dotnet test edf_catalogotablas_net.sln --filter Category=E2E --settings
EDFCatalogoTablasNet.E2E/e2e.runsettings`.

**CI (GitHub Actions):** el workflow [`.github/workflows/dotnet-ci.yml`](.github/workflows/dotnet-ci.yml) ejecuta
`dotnet build` (Release) y un job E2E en Ubuntu con **MongoDB 7** como servicio, variables `MongoDB__ConnectionString` /
`MongoDB__DatabaseName=edf_ci_e2e`, instalación de **PowerShell** y Chromium vía `playwright.ps1`, y `./scripts/e2e-test.sh`.
Ajusta las ramas disparadoras en `on:` si tu repo no usa `main`/`master`/`develop`.

## Compilar el cliente Swift

```bash
cd EDFCatalogoSwift
swift build
./build-macos.sh   # genera bin/EDF Catálogo de Tablas.app
```

## Estado respecto a versiones antiguas

Las referencias a **Avalonia** u otras rutas que ya no existen en este árbol se consideran históricas. La
línea activa en este repo es **Swift (macOS)** y **Blazor (EDFCatalogoTablasNet)**.

## Licencia

Ver el archivo `LICENSE` en la raíz del proyecto.
