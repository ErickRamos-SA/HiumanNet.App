# HuimanNet — Solución .NET 10

> Portal de nómina multiempresa: intercambio seguro de documentos, captura de incidencias,
> **cálculo de nómina configurable** y cotejo contra el cálculo manual.
> **Web** (Blazor Server) + **móvil** (MAUI, Android/iOS) sobre los **mismos casos de uso**.

Documentos de referencia: [ARQUITECTURA.md](ARQUITECTURA.md) · [ESPECIFICACION.md](ESPECIFICACION.md) · `Calculo de Nomina 1.1.docx` · hojas de `nomina/`.

---

## 1. Estructura

```
HuimanNet.sln
│
├── src/
│   ├── HuimanNet.Domain/          # Entidades, políticas, lenguaje de fórmulas y motor de cálculo. Sin dependencias.
│   ├── HuimanNet.Contracts/       # Contrato HTTP, JsonSerializerContext y textos es/en (Localizacion/).
│   ├── HuimanNet.Application/     # Casos de uso (CQRS ligero, sin mediador).
│   ├── HuimanNet.Infrastructure/  # ADO.NET, scripts SQL, catálogo inicial, almacenamiento local/Blob, identidad.
│   ├── HuimanNet.Api/             # Minimal API + Native AOT (backend de la app móvil).
│   ├── HuimanNet.Web/             # Blazor Server (consume Application en proceso).
│   └── HuimanNet.App/             # MAUI + MVVM (Android · iOS).
│
└── tests/
    ├── HuimanNet.Domain.Tests/          # Fórmulas, plan de cálculo, permisos, aislamiento entre empresas.
    ├── HuimanNet.Application.Tests/
    ├── HuimanNet.Api.Tests/
    └── HuimanNet.Infrastructure.Tests/  # Motor contra la hoja de referencia + flujo completo en SQL Server.
```

### Regla de dependencia

```
App (MAUI) ─┐
Web (Blazor)├─► Application ─► Domain
Api (AOT)  ─┘        ▲
                Infrastructure ─┘
```

- **Domain** no referencia nada. **Contracts** sólo a Domain.
- **La app móvil no referencia Application ni Infrastructure**: consume la API.
- **La web consume Application en proceso**: sin salto HTTP y sin duplicar reglas.

---

## 2. Módulos

| Módulo | Web | Móvil | Roles (predeterminado) |
|--------|-----|-------|------------------------|
| Inicio con indicadores y pendientes | ✔ | ✔ | Todos |
| Períodos y documentos (carga directa y descarga con enlace firmado) | ✔ | ✔ | Cliente (sólo su empresa), operador |
| Incidencias por período (captura e importación desde el archivo del período) | ✔ | ✔ | Cliente, operador |
| Cálculo y reproceso de nómina, resumen, detalle por trabajador, facturación | ✔ | ✔ | Operador y administrador (cálculo), cliente (consulta en solo lectura) |
| Cotejo contra el archivo del cálculo manual publicado en el período | ✔ | — | Operador |
| Aprobación / descarte y exportación CSV | ✔ | ✔ | Operador |
| Empleados y contratos (salario mixto), con filtro por empresa | ✔ | ✔ (consulta) | Operador (alta y edición), todos (consulta) |
| Empresas y razones sociales | ✔ | — | Administrador |
| Usuarios y permisos | ✔ | ✔ (consulta) | Administrador |
| Catálogos de cálculo y prueba de fórmulas | ✔ | ✔ (prueba) | Administrador |
| Explicación de cálculos por esquema | ✔ | ✔ | Operador, administrador |
| Bitácora de auditoría | ✔ | — | Todos (lo propio) |
| Idioma español / inglés | ✔ | ✔ | Todos |

Los formularios extensos de administración (catálogos, empresas, alta de usuarios) viven en la web; la app ofrece consulta y prueba de fórmulas.

**Permisos**: cada rol concede un conjunto de acciones (`PermisosPorRol`) y el administrador puede **conceder o negar acciones concretas** a cada usuario. La base de datos es la fuente de verdad: el token (Entra o local) sólo autentica. Las acciones de procesamiento de nómina y de administración no pueden concederse a la empresa cliente (§3.7).

---

## 3. Cálculo de nómina

### 3.1 Nada fijo en el código

Todo valor del cálculo vive en catálogos editables desde **Catálogos de cálculo**:

| Catálogo | Ejemplos |
|----------|----------|
| **Parámetros** (con vigencia) | Salario mínimo por zona, UMA, cuotas IMSS, tasas de ISN, prima de riesgo, días del período |
| **Tablas por rangos** (con vigencia) | ISR, subsidio al empleo, cesantía y vejez patronal |
| **Conceptos** | Cada renglón de la nómina con **su fórmula**, por esquema (IMSS, sindicato, honorarios) |
| **Explicaciones** | Texto por esquema e idioma que se muestra en *Explicación de cálculos* |

Cada elemento puede tener una **sustitución por empresa**, que prevalece sobre el global para esa empresa.

### 3.2 Lenguaje de fórmulas

```
REDONDEAR(BRUTO_INCIDENCIAS - NETO_PAGADO - TOTAL_DEDUCCIONES; 2)
SI(SALARIO_DIARIO_FISCAL <= SALARIO_MINIMO_ZONA; 0; MAX(ISR_NETO_TABLA; 0))
TABLA("ISR"; BASE_GRAVABLE_TABLA; "CUOTA_FIJA")
```

- Operadores `+ - * / ^`, comparaciones y `Y` / `O` infijos; división entre cero da 0.
- Funciones: `SI`, `MAX`, `MIN`, `REDONDEAR`, `TRUNCAR`, `ABS`, `ENTERO`, `TECHO`, `Y`, `O`, `NO`, `ENTRE`, `TABLA`.
- Argumentos separados con `;` o `,`. Aritmética `decimal` de principio a fin.
- Una fórmula puede usar variables de entrada (contrato, incidencia, razón social), parámetros, tablas y **otros conceptos**. El orden de cálculo lo determinan las dependencias (orden topológico); los ciclos y las referencias desconocidas se rechazan al guardar.

Para corregir o ampliar el cálculo basta editar o agregar conceptos: no hay que recompilar. La pestaña **Probar fórmula** evalúa una fórmula con el catálogo vigente y valores de ejemplo.

### 3.3 Esquemas y salario mixto

Un empleado puede tener **varios contratos** en distintas razones sociales y esquemas. El caso típico de salario mixto es un contrato IMSS con *Paga complemento sindical*: el contrato paga el salario fiscal y la diferencia hasta el sueldo real se paga como complemento sindical.

### 3.4 Rendimiento

- Una consulta por tipo de entidad y corrida (contratos, incidencias, catálogo), nunca una por empleado.
- Las fórmulas se compilan **una vez por corrida** y se evalúan en paralelo por trabajador.
- Los resultados se escriben con `SqlBulkCopy`; el detalle por concepto se guarda como JSON en una columna, así los listados no lo leen.
- Las listas grandes de la web se paginan en el servidor (empleados) o en el cliente (resultados, incidencias); en móvil se cargan por páginas.

### 3.5 Verificación contra la hoja de referencia

`MotorConCatalogoInicialTests` reproduce las cifras de *NOMINA SEM (05)* (complemento sindical, ISN, IMSS patronal, INFONAVIT, comisión y costo total) con el catálogo inicial. Si alguien corrige una fórmula y se desvía de la hoja, la prueba lo señala.

### 3.6 Cuándo se calcula: el flujo del período

El cálculo **no es automático ni se repite al consultar**: es una acción manual de nómina sobre un período que ya tiene archivos.

| Paso | Quién | Qué pasa |
|------|-------|----------|
| 1. Abrir el período | Nómina | El período queda **Abierto** para la empresa. |
| 2. Subir archivos | Empresa cliente | Sube incidencias y datos de empleados a *Documentos del período*. Cuando el primero supera el antimalware, el período pasa a **Recibido**. |
| 3. Importar incidencias | Empresa o nómina | En *Documentos del período* o en *Incidencias* se elige un archivo de incidencias **del período** y se importa. También se pueden capturar a mano. |
| 4. Calcular | Nómina o administrador | *Nómina → Calcular nómina*. Se habilita con el período en **Recibido**, **En proceso** o **Resultados disponibles**. |
| 5. Consultar | Todos | El resultado se guarda como **corrida** del período y la empresa (`CorridasDeNomina` y `ResultadosDeNomina`). El resumen, el detalle y la exportación **leen lo guardado**; no recalculan. |
| 6. Reprocesar | Nómina o administrador | Si cambian las incidencias o el catálogo, *Reprocesar nómina* crea la corrida siguiente (#2, #3…) y marca las anteriores no aprobadas como **reemplazadas** (descartadas con una nota). El historial se conserva. |
| 7. Cotejar | Nómina | Se publica el archivo del cálculo manual en el período (tipo *Resultado de nómina* o *Ajuste*) y se coteja contra la corrida. |
| 8. Aprobar | Nómina | La corrida aprobada es la nómina definitiva y **ya no se reprocesa**. Después se publican los resultados y se cierra el período. |

Si una empresa captura sus incidencias en el sistema sin enviar archivos, nómina puede avanzar el período a **En proceso** para habilitar el cálculo.

### 3.7 Separación de roles

| Acción | Empresa cliente | Operador de nómina | Administrador |
|--------|-----------------|--------------------|---------------|
| Empresas visibles | Su empresa principal y sus empresas adicionales | Todas | Todas |
| Subir incidencias y datos de empleados | ✔ (sólo sus empresas) | — | — |
| Importar o capturar incidencias | ✔ (sólo sus empresas) | ✔ | ✔ |
| Calcular, reprocesar, cotejar y aprobar | — | ✔ | ✔ |
| Publicar resultados y ajustes, gestionar períodos | — | ✔ | ✔ |
| Consultar corridas | Solo lectura; sólo la vigente de sus empresas | Todas, con historial | Todas, con historial |
| Empleados | Consulta de sus empresas, con filtro si tiene varias | Todas las empresas con filtro; alta y edición | Igual que el operador |

Procesar la nómina y administrar son acciones **exclusivas de nómina y administración**: `PermisosPorRol.PuedeConcederse` las bloquea para la empresa cliente aunque se le concedan como permiso personalizado, y la pantalla de usuarios no las ofrece para ese rol.

---

## 4. Decisiones de diseño que conviene conocer

### 4.1 Sin ORM, sin mediador, sin mapeador por convención

Native AOT descarta cualquier biblioteca que genere código en tiempo de ejecución:

| Descartado | Motivo | Sustituto |
|-----------|--------|-----------|
| Entity Framework Core | Soporte AOT incompleto en .NET 10 | ADO.NET directo |
| Dapper | Emisión dinámica de IL | Mapeadores manuales por ordinal |
| MediatR | Despacho por reflexión | `IManejadorDeComando<,>` registrado de forma explícita |
| AutoMapper | Mapeo por reflexión | Mapeadores escritos a mano |
| FluentValidation | `Expression.Compile()` en tiempo de ejecución | `IValidadorDeEntrada<T>` y validación en el dominio |

### 4.2 Los archivos entran por el período y no atraviesan el servidor

1. El cliente pide permiso → el servidor **autoriza, valida y firma** un enlace de escritura (un archivo, un permiso, minutos de vigencia).
2. El cliente sube **directo** al almacenamiento.
3. El cliente confirma con el hash SHA-256; el servidor contrasta el tamaño real.
4. El documento no es descargable hasta que el antimalware lo declara limpio.

Todo intercambio de archivos pertenece a un período. La importación de incidencias y el cotejo **no aceptan archivos sueltos**: se elige un documento del período ya analizado y el servidor lo lee directamente del almacenamiento (`IAlmacenDocumentos.LeerAsync`, a través de `ArchivosDelPeriodo`). Es la única lectura del lado del servidor y queda en la bitácora con el documento usado.

### 4.3 Almacenamiento local en desarrollo, Blob en Azure

`Almacenamiento:Proveedor` elige la implementación de `IAlmacenDocumentos`:

| Proveedor | Dónde | Enlaces | Antimalware |
|-----------|-------|---------|-------------|
| `Local` (desarrollo) | `%LOCALAPPDATA%/HuimanNet/almacen` | Firmados con HMAC-SHA256 y servidos por `/almacen-local` | Veredicto limpio inmediato (configurable) |
| `AzureBlob` (despliegue) | Contenedores privados | SAS con *user delegation key* e identidad administrada | Defender for Storage vía Event Grid |

Los dos emiten enlaces con el mismo contrato, así que **migrar a Azure es sólo configuración**.

### 4.4 Aislamiento entre empresas

- Un usuario de empresa cliente tiene una **empresa principal** (`Usuarios.EmpresaId`) y puede tener **empresas adicionales** (`EmpresasAdicionalesDeUsuario`). Sólo opera sobre ellas: si pide cualquier otra, `PoliticaDeAcceso.ResolverEmpresaObjetivo` la rechaza. Con varias empresas elige entre las suyas en la cabecera (web) o en Inicio (app); con una sola, el servidor le impone la suya.
- Nómina y administración operan sobre **todas** las empresas.
- Los repositorios llevan `EmpresaId` **en la cláusula `WHERE`**.
- El acceso denegado devuelve **404, no 403**.

### 4.5 Idiomas

Los textos viven en `src/HuimanNet.Contracts/Localizacion/textos_es.json` y `textos_en.json` (claves planas, p. ej. `nomina.calcular`), compartidos por web y móvil. Los enumerados se traducen con claves `enum.Tipo.Valor`. El idioma se guarda en el perfil del usuario.

---

## 5. Puesta en marcha

### 5.1 Requisitos

- **.NET 10 SDK** (banda `10.0.100` en `global.json`).
- **SQL Server local** para desarrollo (Express o Developer; la configuración usa `localhost\SQLEXPRESS` con autenticación de Windows).
- Para la app: cargas de trabajo **`maui-android`** / **`maui-ios`** y la plataforma **Android API 36** (ver 5.5).
- Para publicar la API con AOT: MSVC en Windows, `clang` + `zlib1g-dev` en Linux.

### 5.2 Primer arranque

```bash
dotnet run --project src/HuimanNet.Web
```

En desarrollo el arranque **crea la base de datos `HuimanNet` si no existe**, aplica los scripts, **carga el catálogo de cálculo inicial** y crea el **administrador inicial** (`Identidad:AdministradorInicial` en `appsettings.Development.json`; se le pide cambiar la contraseña en el primer acceso). Con ese usuario se dan de alta empresas, razones sociales, usuarios y empleados.

Además, con `SqlServer:SembrarDatosDePrueba` (sólo en `appsettings.Development.json`) el arranque carga **datos de prueba** para recorrer el portal con los tres roles:

| Usuario | Rol | Empresas que ve |
|---------|-----|-----------------|
| `admin@huimannet.local` | Administrador | Todas |
| `nomina@huimannet.local` | Operador de nómina | Todas |
| `cliente@huimannet.local` | Empresa cliente | Creatfor Demo (principal) y Creatfor Servicios Demo |

- Los tres usan **la misma contraseña que el administrador**: el arranque copia su *hash* a los dos usuarios de prueba, así que si el administrador la cambia, los otros la siguen en el siguiente arranque. No se les pide cambiarla.
- **Creatfor Demo** tiene empleados IMSS (puro y mixto con complemento sindical), de sindicato y de honorarios; **Creatfor Servicios Demo**, dos empleados IMSS; **Empresa Ajena Demo** no la ve el cliente y sirve para comprobar el aislamiento.
- Cada empresa recibe un período **abierto** del mes en curso. Flujo de prueba: el cliente sube `docs/datos-de-prueba/incidencias-creatfor-demo.csv` como *Incidencia* en los documentos del período y la importa; después nómina calcula (y reprocesa) desde *Nómina*, y el cliente consulta el resultado en sólo lectura.
- Es idempotente: una empresa que ya existe (por RFC) no se modifica, y de los usuarios de prueba sólo se sincroniza la contraseña. Para empezar de cero, borre la base `HuimanNet` y vuelva a arrancar.

La API para la app móvil:

```bash
dotnet run --project src/HuimanNet.Api
```

### 5.3 Pruebas

```bash
dotnet test tests/HuimanNet.Domain.Tests
```

```bash
dotnet test tests/HuimanNet.Infrastructure.Tests
```

`FlujoDeNominaEnSqlServerTests` recorre el flujo completo contra SQL Server en una base **exclusiva, `HuimanNet_Pruebas`, que se borra y se recrea** en cada ejecución. La cadena se cambia con la variable de entorno `HUIMANNET_SQL_PRUEBAS` (por seguridad, el nombre de la base debe terminar en `_Pruebas`). Sin servidor accesible, la prueba se omite.

### 5.4 Publicar

```bash
dotnet publish src/HuimanNet.Api -c Release -r linux-x64
```

```bash
dotnet publish src/HuimanNet.Web -c Release
```

```bash
dotnet publish src/HuimanNet.App -c Release -f net10.0-android
```

> Al publicar la API, revise las advertencias `IL2xxx` / `IL3xxx`: son la señal de que algo rompería el recorte. Los ensamblados de HuimanNet compilan sin ninguna.

> **La API no puede usar el modo de globalización invariante.** `Microsoft.Data.SqlClient` lo rechaza al abrir la primera conexión (`Globalization Invariant Mode is not supported`), por eso `HuimanNet.Api.csproj` fija `InvariantGlobalization` en `false`. Si la API se publica en un contenedor Linux, la imagen debe incluir ICU (`libicu`); las imágenes *chiseled* necesitan la variante `-extra`.

### 5.5 App móvil

- **Ejecute `dotnet` desde la carpeta de la solución.** `global.json` fija el SDK 10.0.100, la banda donde están instaladas las cargas de MAUI. Desde otra carpeta (por ejemplo una terminal de administrador que abre en `C:\WINDOWS\system32`) se usa otro SDK y el build falla con `NETSDK1147` pidiendo `wasm-tools`; no hace falta instalar esa carga.
- `net10.0-android` compila contra **API 36**. Si falta, el build falla con `XA5207`. El SDK está en `Program Files (x86)`, así que la instalación exige una terminal **ejecutada como administrador** (si no, falla con `UnauthorizedAccessException`). `-nr:false` evita que la tarea se ejecute en un proceso de compilación previo sin elevar:

```powershell
cd "C:\Users\erick\Documents\Proyectos\HuimanNet.App"; dotnet build-server shutdown; dotnet build src\HuimanNet.App\HuimanNet.App.csproj -t:InstallAndroidDependencies -f net10.0-android -nr:false -p:AndroidSdkDirectory="C:\Program Files (x86)\Android\android-sdk" -p:AcceptAndroidSDKLicenses=true
```

  Alternativa gráfica: Visual Studio con la carga *Desarrollo de .NET Multi-platform App UI* → **Herramientas > Android > Administrador de Android SDK**.
- La versión de `Microsoft.Maui.Controls` en `Directory.Packages.props` debe coincidir con la carga `maui-android` instalada (`dotnet workload list`). Sin esa referencia, MAUI no se carga y el código no encuentra `Microsoft.Maui`.
- Con Visual Studio abierto sobre la solución, un build desde la terminal puede chocar con la compilación en segundo plano del IDE (`XARLP7024`, archivo en uso); basta con repetirlo.

- En depuración la app apunta a la API local por HTTP (el tráfico sin cifrar sólo se permite hacia `10.0.2.2` y `localhost`); en publicación usa HTTPS.
  - **Emulador**: usa `http://10.0.2.2:5208`, que es el equipo anfitrión visto desde el emulador.
  - **Teléfono por USB** (depuración USB activada): usa `http://localhost:5208`; reenvíe el puerto del teléfono al equipo antes de abrir la app:

```powershell
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" reverse tcp:5208 tcp:5208
```
- El modo de identidad lo informa la API (`/api/v1/configuracion`): con cuentas locales la app muestra correo y contraseña; con Entra abre el inicio de sesión de la organización.
- iOS sólo se agrega al compilar en macOS (o con `-p:IncluirIos=true` y un Mac emparejado).

---

## 6. Configuración

Ningún secreto debe quedar en `appsettings.json` fuera de desarrollo; en Azure se resuelven con **Key Vault + identidad administrada**.

| Sección | Desarrollo | Azure |
|---------|-----------|-------|
| `SqlServer:CadenaDeConexion` | `Server=localhost\SQLEXPRESS;Database=HuimanNet;Trusted_Connection=True;...` | Azure SQL (Key Vault; identidad administrada con `Authentication=Active Directory Default`) |
| `SqlServer:CrearBaseDeDatosSiFalta` | `true` | `false` (la crea la infraestructura) |
| `SqlServer:AplicarScriptsAlIniciar` | `true` | `false` (los aplica CI/CD) |
| `SqlServer:SembrarDatosIniciales` | `true` | `true` (sólo inserta lo que falte) |
| `SqlServer:SembrarDatosDePrueba` | `true` (empresas, empleados y usuarios de prueba) | `false` (nunca en Azure) |
| `Identidad:Modo` | `Local` | `Entra` |
| `Identidad:AdministradorInicial` | Correo y contraseña de desarrollo | Sólo correo (se enlaza con su cuenta de Entra en el primer acceso) |
| `Almacenamiento:Proveedor` | `Local` | `AzureBlob` + `UriServicio` |
| `Notificaciones`, `Correo` | Deshabilitados | Cola de avisos y Azure Communication Services |
| `Carga` | Extensiones y tamaño máximo | Igual |

En modo Entra deben registrarse los roles de aplicación `ClienteEmpresa`, `OperadorNomina` y `Administrador`. Los usuarios también pueden darse de alta primero en el portal: se enlazan por correo en su primer acceso.

---

## 7. Base de datos

El esquema se gestiona con **scripts SQL versionados**, incrustados como recursos:

```
src/HuimanNet.Infrastructure/Persistence/Scripts/
├── 0001_Esquema_Inicial.sql
├── 0002_Datos_Semilla.sql
├── 0003_Nomina_Esquema.sql
└── 0004_Empresas_Adicionales_De_Usuario.sql
```

`InicializadorDeBaseDeDatos` los aplica en orden y anota cada uno en `dbo.__HistorialScripts`. Para un cambio de esquema, cree el siguiente número (`0005_....sql`) con el mismo estilo idempotente.

El **catálogo de cálculo inicial** no está en SQL sino en `Persistence/Semillas/catalogo-inicial.json`: es la única fuente para el sembrado y para las pruebas del motor. `SembradorInicial` sólo inserta cuando la parte correspondiente del catálogo está vacía, así que es seguro en cada arranque y nunca sobrescribe lo que el administrador haya cambiado.
