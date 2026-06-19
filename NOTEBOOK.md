# NOTEBOOK — EDF Catálogo de Tablas (macOS / .NET)

Cuaderno de trabajo del monorepo: documentación revisada, formas de ejecución, portabilidad y
**pruebas de compilación / fiabilidad** ejecutadas en el entorno de análisis.

**Última actualización de este cuaderno:** 24 de marzo de 2026 (secciones 4.5 revisión multi-entorno;
4.6 CI/otro SO).

---

## 1. Documentación analizada

| Ubicación | Contenido relevante |
| --- | --- |
| `README.md` | Visión del monorepo, Homebrew ARM/Intel, `.env`, Blazor, Swift, `CleanCatalogs`. |
| `EDFCatalogoSwift/README.md` | Requisitos, `.env.example` → `.env`, `swift build`, `build-macos.sh`, S3/Mongo. |
| `EDFCatalogoSwift/MANUAL_DE_USUARIO.md` | App nativa; manual de usuario (no releído íntegro en esta pasada). |
| `vscode-profiles/README.md`, `essential-extensions.md` | Perfiles y extensiones VS Code. |
| `Brewfile`, `Brewfile_ORG` | Dependencias Homebrew (actual vs plantilla mínima comentada). |

**Nota:** En versiones anteriores de este NOTEBOOK se citaba un árbol `docs/` con guías
(`CONFIGURACION_*.md`, fases, etc.). **En el árbol actual del repositorio no existe la carpeta
`docs/`**; si la documentación migró, conviene enlazarla desde el README o restaurar la carpeta.

---

## 2. Estructura real del repositorio (resumen)

```text
edf_catalogotablas_macOS/
├── EDFCatalogoSwift/          # App macOS (SwiftPM + SwiftUI)
├── EDFCatalogoTablasNet/      # Blazor Server (.NET)
├── CleanCatalogs/             # CLI .NET (vaciar colección catalogs)
├── scripts/                   # Homebrew, dotnet ARM, validación Brewfile, desinst. /usr/local
├── vscode-profiles/           # Ajustes multiplataforma VS Code
├── .vscode/                   # settings, tasks, extensions
├── global.json                # SDK .NET mínimo + rollForward
├── edf_catalogotablas_net.sln
├── Brewfile / Brewfile_ORG
├── README.md
└── NOTEBOOK.md                # Este archivo
```

---

## 3. Componentes y roles

| Componente | Tecnología | Entrada en runtime |
| --- | --- | --- |
| **EDFCatalogoSwift** | SwiftUI, MongoSwift, NIO | `.env` o `Contents/Resources/.env` en el `.app`. |
| **EDFCatalogoTablasNet** | ASP.NET Core, Blazor Server | `appsettings*` y/o variables de entorno. |
| **CleanCatalogs** | .NET CLI | URI `MONGO_URI` o `MONGODB_URI`; opc. `MONGO_DB`, colección, `--dry-run`. |

**Backend de datos:** MongoDB Atlas (red). **Ficheros:** AWS S3 opcional en Swift (`USE_S3`,
credenciales AWS, bucket, región).

---

## 4. Ejecución según entorno

### 4.1 Desarrollo — .NET (Blazor)

1. SDK: `global.json` fija **9.0.106** con **`rollForward: latestMajor`** (en la prueba se usó
   **10.0.105** sin error de compilación).
2. En Apple Silicon, priorizar toolchain coherente:

   ```bash
   export PATH="/opt/homebrew/bin:/opt/homebrew/sbin:$PATH"
   source scripts/use-dotnet-arm64.sh   # define DOTNET_ROOT (formula o cask Microsoft)
   dotnet restore edf_catalogotablas_net.sln
   dotnet build edf_catalogotablas_net.sln
   dotnet run --project EDFCatalogoTablasNet/EDFCatalogoTablasNet.csproj
   ```

3. URL típica: `http://localhost:5005` (según configuración del proyecto).

**Config:** copiar `appsettings.local.json.EXAMPLE` → `appsettings.local.json` y rellenar
`MongoDB:ConnectionString` (sin subir a git).

### 4.2 Desarrollo — Swift (línea de comandos)

```bash
cd EDFCatalogoSwift
cp .env.example .env    # una vez; editar MONGO_URI, S3 si aplica
swift package resolve
swift build
# Ejecutable en .build/debug/EDFCatalogoSwift (según target del Package)
```

### 4.3 Desarrollo / distribución — Swift (`.app`)

El empaquetado del `.app` usa **`build-macos.sh`**.

```bash
cd EDFCatalogoSwift
./build-macos.sh
open "bin/EDF Catálogo de Tablas.app"
```

Credenciales en app firmada: copiar `.env` a `Contents/Resources/.env` dentro del bundle (ver README
Swift).

### 4.4 Utilidad — CleanCatalogs

Hace **ping**, cuenta documentos y elimina **todos** los de la colección (por defecto `catalogs` en
base `edf_catalogotablas`), salvo `--dry-run`.

```bash
export MONGO_URI='mongodb+srv://USUARIO:PASSWORD@cluster.../'
# o: export MONGODB_URI='...'
# opcional: export MONGO_DB=edf_catalogotablas
# opcional: export CLEAN_CATALOGS_COLLECTION=catalogs   # o MONGO_CATALOGS_COLLECTION

dotnet run --project CleanCatalogs/CleanCatalogs.csproj -- --dry-run   # solo recuento
dotnet run --project CleanCatalogs/CleanCatalogs.csproj              # borrado real
dotnet run --project CleanCatalogs/CleanCatalogs.csproj -- --help
```

**Peligro:** el modo sin `--dry-run` vacía la colección. Solo entornos controlados. Usar **`--`**
antes de `--dry-run` / `--help` con `dotnet run`.

### 4.5 Revisión de funcionamiento por entorno (checklist)

| Entorno | Configuración mínima | Comportamiento esperado | Comprobación |
| --- | --- | --- | --- |
| Blazor dotnet run | DB y conexión = Atlas (`DatabaseName`). | :5005, seeder; legacy. | `dotnet build`, `/health`. |
| Blazor Development | `ASPNETCORE_ENVIRONMENT=Development`. | Swagger; sin redirect HTTPS en dev. | Manual. |
| Blazor no Development | Mongo + despliegue. | `UseHttpsRedirection` activo. | Manual servidor. |
| Swift CLI | `.env` cwd: URI y bases `MONGO_*` / `MONGODB_*`. | `MongoService`: cwd + bundle. | `swift build` OK. |
| Swift `.app` | `Resources/.env` o `launcher.sh`. | Env coherente. | Manual. |
| CleanCatalogs | URI; opc. base y colección. | Ping; borrado salvo `--dry-run`. | Build; `--dry-run` antes. |

**Detalle Blazor run:** `ConnectionString` válida y **`DatabaseName` idéntico** al de Atlas (p. ej.
`edf_catalogotablas` vs nombre con guión). Sin Mongo: fallo al arrancar o en seed. **Legacy:** pueden
listarse catálogos sin filas en detalle hasta alinear BSON/`Rows`. **`/health`:** 200 y
`mongoDb.ok` si el ping OK; **503** si falla (el proceso puede seguir vivo).

**Detalle .NET:** `Program.cs` fija URLs (`http://localhost:5005`, `https://localhost:7005`) y el
`HttpClient` Blazor usa `BaseAddress = http://localhost:5005`; si cambias puertos, alinea ambos.

**Límite:** no hubo E2E contra MongoDB/S3 sin credenciales reales; la tabla resume requisitos y la
corrección Swift ↔ `.env.example`.

### 4.6 CI / otro SO

- **Blazor** y **CleanCatalogs** son portables a Linux/Windows con .NET instalado y misma config
  MongoDB.
- **Swift** está acoplado a **macOS** (SwiftUI).

---

## 5. Variables y secretos (referencia, sin valores reales)

**Swift (`EDFCatalogoSwift/.env`, gitignored):**  
`MONGO_URI`, `MONGO_DB` / `MONGODB_DB`, `AWS_*`, `S3_BUCKET_NAME`, `AWS_REGION`, `USE_S3`, etc.
Plantilla: `.env.example`.

**.NET:** `MongoDB:ConnectionString`, `DatabaseName`, colecciones en `appsettings` /
`appsettings.local.json`.

**CleanCatalogs (solo al ejecutar la CLI):** URI obligatoria (`MONGO_URI` o `MONGODB_URI`); base y
colección alineables (`MONGO_DB`/`MONGODB_DB`, `CLEAN_CATALOGS_COLLECTION` o
`MONGO_CATALOGS_COLLECTION`).

**Nunca commitear:** URIs con usuario/contraseña, claves AWS, tokens.  
*(En una edición previa de este NOTEBOOK había un ejemplo JSON con cadena MongoDB real; se ha
eliminado. Si esa clave llegó a un remoto público, **rotar credenciales** en Atlas.)*

---

## 6. Portabilidad

| Ámbito | Grado | Notas |
| --- | --- | --- |
| macOS Apple Silicon | Alto | Flujo con `/opt/homebrew` y cask `dotnet-sdk`. |
| macOS Intel | Medio | Swift posible; Homebrew `/usr/local`; no mezclar ARM/Intel. |
| Linux / Windows | Parcial | Solo .NET; mismo Blazor/CLI. |
| MongoDB / S3 | Dependencia externa | Sin red y sin credenciales no hay datos ni multimedia. |
| Blazor Server | No es SPA estática | Requiere proceso servidor activo. |

**Herramientas del repo:** scripts `brew-*`, `validate-brewfile.sh`,
`uninstall-homebrew-usrlocal.sh` son **específicos de macOS/Homebrew**.

---

## 7. Pruebas de fiabilidad ejecutadas (24–25 mar 2026)

Objetivo: comprobar que el **código compila** y los **shell scripts** son sintácticamente válidos. Hay
**proyecto E2E** (Playwright); no hay aún tests unitarios clásicos (`*.Tests.csproj`) fuera de E2E.

### 7.1 `dotnet build edf_catalogotablas_net.sln`

- **Resultado:** correcto (código 0).
- **SDK observado:** 10.0.105 (compatible con `rollForward: latestMajor`).
- **Advertencias (25 mar 2026):** **0** tras `dotnet build` (EDFCatalogoTablasNet + CleanCatalogs).
  Una revisión anterior citaba ~19 (nullable, `LegacyRows`/`CS0618`); el recuento puede cambiar con
  código o SDK — repetir el build tras cambios. Deuda razonable: migrar `LegacyRows` → `Rows` y
  nullability en UI.

### 7.2 `swift build` (en `EDFCatalogoSwift/`)

- **Resultado:** correcto (código 0, ~33 s en el entorno de prueba).
- Dependencias resueltas (MongoSwift, NIO, driver C, etc.).
- **Código:** `MongoService` alineado con `.env.example` (`MONGO_URI` / `MONGO_DB` y carga de `.env`
  desde cwd y bundle); ver sección 4.5.

### 7.3 Scripts Bash

- **Comando:** `bash -n` sobre `scripts/*.sh` y `EDFCatalogoSwift/*.sh`.
- **Resultado:** sin errores de sintaxis.

### 7.4 Tests automatizados .NET

- **EDFCatalogoTablasNet.E2E:** NUnit + Playwright (`Category=E2E`): `/health`, `/login`, login → `/` →
  `/catalogs`, y **crear catálogo** en `/create` (mensaje de éxito). Script: `./scripts/e2e-test.sh` (puerto
  **5107** por defecto, `ASPNETCORE_URLS`). Chromium: `./scripts/e2e-playwright-install.sh` (`pwsh`).
- **CleanCatalogs** sigue sin tests unitarios dedicados.

### 7.5 Ejecución en runtime (manual)

- **Smoke Blazor:** `scripts/smoke-blazor.sh` — build, `dotnet run` temporal, `curl` a
  `http://127.0.0.1:5005/health` (exige `status` Healthy y `mongoDb.ok`). Si `:5005` ya sirve
  `/health` con ese cuerpo, OK sin segundo arranque (`SMOKE_ALWAYS_START=1` para forzar). Sin runtime
  9.x: `DOTNET_ROLL_FORWARD=LatestMajor`.
- **No probada** en esta sesión: login, APIs, subida S3, ni `brew bundle` en máquina del usuario.
- Fiabilidad funcional: pruebas manuales previas en README/NOTEBOOK; para regresiones conviene smoke
  o Playwright/API tests.

---

## 8. Homebrew y toolchain (estado coherente con el repo)

- **`Brewfile` actual (raíz):** `tap azizuysal/simtool`, cask `dotnet-sdk` con `no-binaries`, `pcre2`,
  varias fórmulas/casks de usuario; ver fichero para lista exacta.
- **`Brewfile_ORG`:** plantilla comentada mínima (.NET + `pcre2`) para el monorepo.
- Scripts útiles: `validate-brewfile.sh`, `brew-intel-vs-arm-report.sh`,
  `fix-brew-intel-arm-mixup.sh`, `uninstall-homebrew-usrlocal.sh` (desinst. brew en `/usr/local`).

Tras desinstalar Homebrew Intel, puede quedar **`/usr/local/Homebrew`** o restos bajo `/usr/local`;
**`brew doctor`** en ARM puede avisar de cabeceras en `/usr/local/include/node` (no gestionadas por
`/opt/homebrew`).

---

## 9. Riesgos y deuda técnica visible

1. **Cobertura automática parcial:** E2E Blazor (ver §7.4 y §10); faltan unitarios e integración Mongo
   dedicados.
2. **Calidad de compilación:** 25 mar 2026 → **0** advertencias; pendiente consolidar modelo/UI
   (`LegacyRows` → `Rows`, nullability) para futuros SDK.
3. ~~**Dos scripts de build**~~ — unificado en **`build-macos.sh`** (se eliminó `build_app.sh`
   duplicado).
4. **Casks/formulas deprecadas** que `brew doctor` puede listar (`icu4c@77`, algunos casks).
5. **Documentación dispersa:** ausencia de `docs/` frente a referencias antiguas.
6. **Datos Mongo heterogéneos:** documentos antiguos (`name`/`Name`, `rows` como texto); BSON tolerante
   en listado; migración a `Rows`/`CatalogRow` pendiente para detalle en UI.
7. **Usuarios Mongo:** posible desalineación `Inactive` / `IsActive` en modelo vs BSON (revisar al
   tocar gestión de usuarios).

---

## 10. Plan de comprobación sistemática (manual + E2E)

### 10.1 Objetivo y registro

Comprobar **flujos reales** con datos en Mongo (y Swift/S3 si aplica). Cada ejecución: anotar **fecha**,
**commit/etiqueta**, **entorno** (dev/staging), **operador** y resultado en una copia de las tablas
siguientes (hoja de cálculo o duplicado de este apartado).

### 10.2 Entorno mínimo antes de empezar

- Web: `appsettings.local.json` con Mongo alcanzable; `DatabaseName` = base real de prueba.
- E2E automático: `./scripts/e2e-playwright-install.sh` (una vez) y `./scripts/e2e-test.sh` (puerto **5107**).
- CI (GitHub): `.github/workflows/dotnet-ci.yml` compila la solución y ejecuta el mismo E2E contra **Mongo 7** en servicio (`MongoDB__DatabaseName=edf_ci_e2e`).
- Opcional Mongo solo E2E: otra base (`*_e2e`) y usuario de prueba para no ensuciar producción.

### 10.3 Cobertura ya automatizada (`Category=E2E`)

| ID | Comprobación |
| --- | --- |
| A1 | `GET /health` → JSON `status=Healthy`, `mongoDb.ok=true` |
| A2 | `GET /login` muestra email, contraseña y botón iniciar sesión |
| A3 | Login → `/` con bienvenida Blazor |
| A4 | Tras login, `/catalogs` muestra título con «Catálogos» |
| A5 | Tras login, `/create` rellena nombre, descripción, columnas → alerta de éxito |
| A6 | Encadenado: enlace del aviso → `/catalogs`, búsqueda por nombre, «Ver detalles» → `/catalog/{id}` y `h1` con el mismo nombre |
| A7 | Tras A6: «Agregar Fila» → modal, rellenar columnas → «Guardar Fila» → `.catalog-row` y `.column-value` con los textos guardados |
| A8 | Usuario Admin (`E2E_EMAIL`): `/admin/users` muestra «Gestión de Usuarios», tabla «Lista de Usuarios» y el email de sesión en el listado (sin mutar datos) |
| A9 | `/login` con contraseña incorrecta → `.alert-danger` «Credenciales incorrectas» y URL sigue en login |
| A10 | Sin cookies, `/catalogs` → tarjeta «Acceso Restringido» y enlace «Iniciar Sesión» (alinea M4) |
| A11 | Ciclo completo en un solo test: crear catálogo → fila → exportar JSON (nombre + celda) → editar fila → editar titular en `/catalog/edit/{id}` → eliminar fila (confirm); depende de rehidratar `rows` al leer BSON (`CatalogBsonDeserializer`) |
| U1 | `EDFCatalogoTablasNet.Tests` (xUnit): `RoleHelper`, deserialización JSON de `MongoDbSettings` |
| I1 | Mismo proyecto, namespace `*.Integration`: escritura/lectura/borrado en Mongo si `MongoDB__ConnectionString` |

### 10.4 Checklist manual — autenticación y sesión

| ID | Flujo | Pasos breves | OK |
| --- | --- | --- | --- |
| M1 | Login OK | Credenciales válidas → redirección inicio | [ ] |
| M2 | Login KO | Credenciales falsas → mensaje error, sin sesión | [ ] |
| M3 | Logout | Cerrar sesión → `/login` o bloqueo en rutas protegidas | [ ] |
| M4 | Sin sesión | Abrir `/catalogs` sin login → pantalla restringida o login | [ ] |
| M5 | Registro | `/register` crear usuario (solo si lo usáis en ese entorno) | [ ] |
| M6 | Olvido | `/forgot-password` flujo completo si está operativo | [ ] |

### 10.5 Checklist manual — catálogos (Blazor)

| ID | Flujo | Pasos breves | OK |
| --- | --- | --- | --- |
| C1 | Listado | `/catalogs` carga; búsqueda/filtros si existen | [ ] |
| C2 | Detalle | Abrir catálogo; ver columnas y filas (o vacío coherente) | [ ] |
| C3 | Fila | Añadir / editar / borrar fila en un catálogo de prueba | [ ] |
| C4 | Borrar catálogo | Solo si el rol debe poderlo; confirmar desaparece del listado | [ ] |
| C5 | Legacy BSON | Documento antiguo: listado vs detalle sin filas (documentar esperado) | [ ] |
| C6 | Recarga | F5 en inicio, listado y detalle con circuito activo | [ ] |
| C7 | Dos sesiones | Dos navegadores o incógnito: usuarios no se mezclan | [ ] |

### 10.6 Checklist manual — administración

| ID | Flujo | Pasos breves | OK |
| --- | --- | --- | --- |
| AD1 | Listado usuarios | `/admin/users` como admin | [ ] |
| AD2 | Rol | Cambiar rol y comprobar permisos en catálogos | [ ] |
| AD3 | Reset password | Si la función existe, probar y login con nueva clave | [ ] |

### 10.7 Checklist manual — API y operación

| ID | Flujo | Pasos breves | OK |
| --- | --- | --- | --- |
| O1 | Health fallo | Mongo caído o URI mala → `503` y cuerpo coherente | [ ] |
| O2 | Swagger | En Development, `/api-docs` carga | [ ] |

### 10.8 Checklist manual — Swift (si aplica)

| ID | Flujo | Pasos breves | OK |
| --- | --- | --- | --- |
| S1 | CLI | `swift run` con `.env` → login y listados | [ ] |
| S2 | S3 | Con `USE_S3=true`, subida/descarga presignada | [ ] |
| S3 | `.app` | `build-macos.sh` y smoke en bundle con `.env` en Resources | [ ] |

### 10.9 Ampliación futura de automatización (prioridad sugerida)

1. ~~E2E: abrir detalle de catálogo por enlace tras crear (o por nombre en lista).~~ → cubierto en A6.
2. ~~E2E: una fila añadida en detalle y comprobación en tabla.~~ → cubierto en A7.
3. ~~E2E admin: solo lectura de `/admin/users` con usuario admin (sin borrar datos reales).~~ → A8.
4. ~~Integración .NET:~~ proyecto **`EDFCatalogoTablasNet.Tests`** (xUnit): unitarios sin Mongo (U1) e **I1** en job CI E2E. Opcional: **Testcontainers** para ejecutar I1 sin servicio GHA.
5. ~~CI: workflow **GitHub Actions** `.github/workflows/dotnet-ci.yml` — job `build` (Release) y job E2E (Mongo 7 + `MongoDB__*` + `./scripts/e2e-test.sh`, sin Atlas).~~ Para staging real, puedes añadir job con secretos `MongoDB__ConnectionString`.
6. ~~E2E: login KO (A9) y `/catalogs` sin sesión (A10); script `e2e-test.sh` libera el puerto E2E si estaba ocupado.~~

---

## 11. Conclusión

El repositorio **compila** en .NET (solución completa) y **Swift** (`swift build`), y los **scripts
shell** revisados pasan `bash -n`. La **portabilidad** de producto es máxima en **macOS + .NET**; el
cliente Swift es **macOS-only**. La **fiabilidad** mejora con **E2E Playwright** (§7.4, §10.3) y el
**plan manual** (§10.4–10.8); conviene seguir ampliando pruebas y CI.

---

*Este NOTEBOOK incluye plan de comprobación §10, E2E ampliado (crear catálogo) y §11 Conclusión.*
