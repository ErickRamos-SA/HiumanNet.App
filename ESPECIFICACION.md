# HuimanNet.App — Especificación del Proyecto

> **V1 — Portal de intercambio de documentos de nómina.** Aplicación web **Blazor Server** y app móvil **.NET MAUI** (.NET 10), con **Clean Architecture + MVVM** en el cliente y **Minimal APIs con Native AOT** en el backend.

Ver también: [ARQUITECTURA.md](ARQUITECTURA.md) · Diagramas en [diagramas/](diagramas/)

---

## 1. Visión general

**HuimanNet** es un **portal seguro de intercambio de documentos** entre las empresas cliente y la empresa que opera la nómina.

Las empresas cliente **suben** documentos de incidencias y datos de empleados; el operador de nómina los **descarga y los procesa de forma manual fuera del sistema**, y **sube** de vuelta los archivos de resultado o ajustes para que el cliente los descargue.

### 1.1 Alcance de la V1

| ✅ Dentro del alcance | ❌ Fuera del alcance (V2+) |
|----------------------|---------------------------|
| Carga de documentos (incidencias, datos de empleados) | Motor de cálculo de nómina |
| Organización por período y tipo de documento | Reglas impositivas / motor tributario |
| Descarga por el operador de nómina | Generación automática de recibos PDF |
| Carga de archivos de resultado / ajustes | Integración bancaria (dispersión de pagos) |
| Descarga de resultados por el cliente | Integración fiscal / seguridad social |
| Estados del ciclo (recibido · en proceso · resultados disponibles) | Procesamiento masivo automatizado |
| Notificaciones por correo | |
| Autenticación, roles y aislamiento entre empresas | |
| Bitácora de auditoría (cargas y descargas) | |
| Paridad funcional entre web (Blazor) y móvil (MAUI) | |

> **Implicación clave**: el dominio de la V1 es **gestión documental y flujo de estados**, no cálculo. La exigencia técnica se concentra en **seguridad de archivos, control de acceso y trazabilidad**.

### 1.2 Objetivos clave

| Objetivo | Descripción |
|----------|-------------|
| **Multiplataforma** | Web (Blazor Server) y móvil (Android/iOS con MAUI) con la misma funcionalidad. |
| **Seguridad de archivos** | Escaneo de malware, validación de tipo/tamaño y acceso por SAS de mínimo privilegio. |
| **Aislamiento** | Una empresa **nunca** accede a documentos de otra. |
| **Trazabilidad** | Bitácora de quién subió y **quién descargó** cada documento. |
| **Rendimiento** | Backend con Native AOT: arranque en frío mínimo y baja huella de memoria. |
| **Mantenibilidad** | Clean Architecture: el motor de cálculo de V2 entrará **sin reescribir** el portal. |
| **Testeabilidad** | Lógica de dominio y presentación desacoplada de la infraestructura. |
| **Documentación** | Cada clase, método y propiedad documentado con comentarios XML (`///`). |

---

## 2. Stack tecnológico

| Capa | Tecnología |
|------|------------|
| **Cliente web** | **Blazor Server** (.NET 10) |
| **Cliente móvil** | .NET MAUI (.NET 10) — Android e iOS, XAML, MVVM |
| **Toolkit MVVM** | CommunityToolkit.Mvvm (source generators) |
| **Backend API** | ASP.NET Core Minimal APIs (.NET 10) con **Native AOT** |
| **Serialización** | System.Text.Json con `JsonSerializerContext` (source-generated, AOT-safe) |
| **Acceso a datos** | ADO.NET directo con `Microsoft.Data.SqlClient` (AOT-safe, sin ORM) |
| **Base de datos** | **SQL Server** (metadatos, estados y bitácora) |
| **Almacenamiento de archivos** | **Azure Blob Storage** (`Azure.Storage.Blobs`) — núcleo del sistema |
| **Cliente HTTP** | `HttpClient` + `IHttpClientFactory` con handlers tipados |
| **Validación** | FluentValidation / Data Annotations |
| **Mapeo** | Mapeadores manuales (sin reflexión por requisito AOT) |
| **Logging** | `Microsoft.Extensions.Logging` + Serilog |
| **Autenticación** | Microsoft Entra — OpenID Connect / JWT Bearer |
| **Testing** | xUnit, FluentAssertions, NSubstitute |
| **CI/CD** | GitHub Actions |

---

## 3. Arquitectura — Clean Architecture

La solución se organiza en **cuatro anillos concéntricos**. Las dependencias apuntan **siempre hacia el centro** (regla de dependencia). Las capas externas conocen a las internas, nunca al revés.

```
┌─────────────────────────────────────────────────────────┐
│  Presentation (MAUI · MVVM)   +   API (Minimal · AOT)    │
│  ┌───────────────────────────────────────────────────┐  │
│  │            Infrastructure                          │  │
│  │  ┌─────────────────────────────────────────────┐  │  │
│  │  │        Application (Casos de uso)            │  │  │
│  │  │  ┌───────────────────────────────────────┐  │  │  │
│  │  │  │            Domain (Núcleo)             │  │  │  │
│  │  │  └───────────────────────────────────────┘  │  │  │
│  │  └─────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### 3.1 Domain (Núcleo)

Contiene la lógica de negocio pura. **Sin dependencias externas.**

- **Entidades**: `Empresa`, `Usuario`, `PeriodoCarga`, `Documento`, `LoteDocumentos`, `RegistroAuditoria`.
- **Objetos de valor (Value Objects)**: `NombreArchivo`, `TamanoArchivo`, `HuellaArchivo` (hash), `PeriodoCalendario`.
- **Enumeraciones**: `TipoDocumento` (Incidencia · DatosEmpleado · Resultado · Ajuste), `EstadoDocumento` (Pendiente · Escaneando · Disponible · EnCuarentena), `EstadoPeriodo` (Abierto · Recibido · EnProceso · ResultadosDisponibles · Cerrado), `RolUsuario` (ClienteEmpresa · OperadorNomina · Administrador).
- **Excepciones de dominio**: `DocumentoInvalidoException`, `PeriodoCerradoException`, `AccesoNoAutorizadoException`.
- **Interfaces de repositorio**: `IEmpresaRepository`, `IDocumentoRepository`, `IPeriodoRepository`, `IAuditoriaRepository`.
- **Servicios de dominio**: `ValidadorDeDocumento` (tipo y tamaño permitidos), `PoliticaDeAcceso` (qué rol puede ver qué documento).

> **Nota de alcance**: no existen `CalculadoraNomina`, `MotorDeReglasImpositivas`, `Deduccion` ni `Percepcion` — el cálculo es manual y externo en V1 (§1.1). Entrarán como nuevos tipos de `Domain` cuando se aborde el motor de cálculo, **sin alterar** el modelo documental aquí definido.

### 3.2 Application (Casos de uso)

Orquesta el dominio para cumplir casos de uso. Depende sólo de `Domain`.

- **Comandos**: `SolicitarCargaDocumentoCommand` (valida y emite SAS de escritura), `ConfirmarCargaCommand`, `RegistrarResultadoEscaneoCommand`, `SubirResultadoCommand`, `CambiarEstadoPeriodoCommand`.
- **Consultas**: `ListarDocumentosPorPeriodoQuery`, `ObtenerEnlaceDescargaQuery` (emite SAS de lectura), `ConsultarBitacoraQuery`.
- **Handlers**: implementan la lógica de cada caso de uso.
- **DTOs**: objetos de transferencia entre capas.
- **Interfaces de servicios de aplicación**: `IAlmacenDocumentos` (abstrae Blob Storage y la emisión de SAS), `INotificationService`, `IUsuarioActual` (identidad y empresa del solicitante), `IUnitOfWork`.
- **Validadores**: reglas de validación de entrada.

### 3.3 Infrastructure

Implementa las interfaces definidas en `Domain` y `Application`.

- **Persistencia (ADO.NET)**: fábrica de conexiones `ISqlConnectionFactory`, repositorios con `SqlCommand`/`SqlDataReader`, y **mapeadores manuales** `SqlDataReader → Entidad`.
- **Scripts SQL**: esquema, procedimientos almacenados y scripts de versionado (migraciones con SQL versionado, no con un ORM).
- **Almacenamiento de documentos**: `BlobAlmacenDocumentos` — implementa `IAlmacenDocumentos` sobre `Azure.Storage.Blobs`, genera **URL SAS de mínimo privilegio** (un blob, permiso único, minutos de vigencia).
- **Notificaciones**: envío de correo vía Azure Communication Services + cola de avisos.
- **Clientes HTTP** hacia servicios de terceros.

> **Nota de diseño (AOT)**: no se usa Entity Framework Core ni Dapper. El acceso a datos es ADO.NET puro para garantizar compatibilidad total con Native AOT. Ver §6.1.

### 3.4 Presentation & API

- **HuimanNet.Web (Blazor Server)**: aplicación web; consume `Application` **en proceso**.
- **HuimanNet.App (MAUI)**: app móvil Android/iOS con patrón MVVM; consume la API por HTTPS.
- **HuimanNet.Api (Minimal API AOT)**: endpoints HTTP — es la frontera para el cliente móvil.

---

## 4. Estructura de la solución

```
HuimanNet.sln
│
├── src/
│   ├── HuimanNet.Domain/            # Núcleo — entidades, VOs, contratos
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Enums/
│   │   ├── Exceptions/
│   │   ├── Repositories/            # Interfaces
│   │   └── Services/                # Servicios de dominio
│   │
│   ├── HuimanNet.Application/       # Casos de uso
│   │   ├── Common/
│   │   ├── Documentos/
│   │   │   ├── Commands/            # SolicitarCarga, ConfirmarCarga, SubirResultado
│   │   │   ├── Queries/             # ListarPorPeriodo, ObtenerEnlaceDescarga
│   │   │   └── Validators/
│   │   ├── Periodos/
│   │   ├── Empresas/
│   │   ├── Auditoria/
│   │   ├── DTOs/
│   │   └── Interfaces/              # IAlmacenDocumentos, INotificationService...
│   │
│   ├── HuimanNet.Infrastructure/    # Implementaciones
│   │   ├── Persistence/
│   │   │   ├── Connections/         # ISqlConnectionFactory
│   │   │   ├── Repositories/        # ADO.NET (SqlCommand/SqlDataReader)
│   │   │   ├── Mappers/             # SqlDataReader -> Entidad (manual)
│   │   │   └── Scripts/             # Esquema, SPs y scripts de versionado
│   │   ├── Storage/                 # BlobAlmacenDocumentos (SAS, contenedores)
│   │   ├── Notifications/           # Correo + cola de avisos
│   │   └── DependencyInjection.cs
│   │
│   ├── HuimanNet.Api/               # Minimal API + Native AOT (backend de MAUI)
│   │   ├── Endpoints/
│   │   ├── Serialization/           # JsonSerializerContext
│   │   ├── Extensions/
│   │   └── Program.cs
│   │
│   ├── HuimanNet.Web/               # Blazor Server (aplicación web)
│   │   ├── Components/
│   │   │   ├── Pages/
│   │   │   └── Shared/
│   │   ├── Services/
│   │   └── Program.cs
│   │
│   └── HuimanNet.App/               # MAUI + MVVM (Android · iOS)
│       ├── Views/
│       ├── ViewModels/
│       ├── Services/                # Cliente API, navegación
│       ├── Controls/
│       ├── Converters/
│       ├── Resources/
│       ├── Platforms/
│       ├── App.xaml
│       ├── AppShell.xaml
│       └── MauiProgram.cs
│
└── tests/
    ├── HuimanNet.Domain.Tests/
    ├── HuimanNet.Application.Tests/
    ├── HuimanNet.Api.Tests/
    ├── HuimanNet.Web.Tests/
    └── HuimanNet.App.Tests/
```

---

## 5. Patrón MVVM en el cliente MAUI

Se utiliza **CommunityToolkit.Mvvm** para eliminar código repetitivo mediante *source generators* (compatible con recorte/trimming).

### Responsabilidades

| Componente | Responsabilidad |
|------------|-----------------|
| **View (XAML)** | Presentación pura. Enlaces (`Binding`) a la ViewModel. Sin lógica de negocio en code-behind. |
| **ViewModel** | Estado observable, comandos, orquestación de servicios. Hereda de `ObservableObject`. |
| **Model / DTO** | Datos que fluyen entre ViewModel y servicios. |
| **Service** | Acceso a la API, navegación, almacenamiento local. |

### Convenciones MVVM

- Propiedades observables con `[ObservableProperty]`.
- Comandos con `[RelayCommand]` (incluye soporte `async` y `CanExecute`).
- Inyección de dependencias vía constructor (registrado en `MauiProgram`).
- Navegación desacoplada mediante `INavigationService`.

### Paridad funcional web ↔ móvil

Ambos clientes ofrecen **la misma funcionalidad** (§1.1), pero con modelos de presentación distintos:

| Aspecto | Blazor Server (web) | MAUI (móvil) |
|---------|---------------------|--------------|
| Patrón de UI | Componentes `.razor` con code-behind | **MVVM** con `ObservableObject` |
| Acceso a la lógica | Consume `Application` **en proceso** | Consume la **API AOT** por HTTPS |
| Estado | Circuito SignalR del servidor | Estado en el dispositivo |

> La lógica de negocio vive **una sola vez** en `Application`/`Domain`. Los clientes sólo aportan presentación — así se garantiza la paridad sin duplicar reglas.
- **Prohibido** lógica de negocio en el code-behind de las vistas.

---

## 6. Minimal API con Native AOT

El backend expone endpoints ligeros compilados a código nativo.

### Reglas obligatorias para AOT

1. **Serialización source-generated**: definir un `JsonSerializerContext` con `[JsonSerializable]` para cada tipo. **No usar reflexión.**
2. **Sin dependencias incompatibles con trimming**: evitar librerías que usen reflexión dinámica o emisión de IL en runtime.
3. **`RequestDelegate` tipados**: usar delegados fuertemente tipados; el generador de rutas de Minimal API es AOT-friendly.
4. **Configurar el proyecto**:
   ```xml
   <PropertyGroup>
     <PublishAot>true</PublishAot>
     <InvariantGlobalization>true</InvariantGlobalization>
   </PropertyGroup>
   ```
5. **Validación de compatibilidad**: publicar con `dotnet publish -c Release` y revisar advertencias de trimming/AOT (`IL2xxx`, `IL3xxx`).

### 6.1 Estrategia de acceso a datos (AOT-safe)

El acceso a SQL Server se realiza con **ADO.NET directo** sobre `Microsoft.Data.SqlClient`. **No se usa ORM.**

#### Justificación

| Opción | Veredicto | Motivo |
|--------|-----------|--------|
| **Entity Framework Core 10** | ❌ Descartado | No tiene soporte completo de Native AOT en .NET 10. Las *precompiled queries* y *compiled models* siguen siendo limitados y arrastran reflexión/emisión de IL que rompe el trimming. |
| **Dapper (clásico)** | ❌ Descartado | Usa emisión dinámica de IL en tiempo de ejecución → incompatible con AOT (además excluido por requisito). |
| **ADO.NET + `Microsoft.Data.SqlClient`** | ✅ **Elegido** | API estable, sin reflexión, arranque instantáneo y huella mínima. Control total sobre SQL y mapeo. |

#### Principios de implementación

- **Fábrica de conexiones**: `ISqlConnectionFactory` crea instancias de `SqlConnection`; la cadena de conexión se inyecta vía configuración.
- **Comandos parametrizados**: **siempre** usar `SqlParameter`. Prohibida la concatenación de SQL (previene inyección).
- **Mapeo manual**: cada repositorio traduce `SqlDataReader` a entidades de dominio mediante mapeadores explícitos escritos a mano (sin reflexión ni convenciones mágicas).
- **Async**: usar `OpenAsync`, `ExecuteReaderAsync`, `ExecuteNonQueryAsync` con `CancellationToken`.
- **Acceso por ordinal**: leer columnas por índice (`reader.GetInt32(0)`) o mediante ordinales cacheados (`reader.GetOrdinal(...)`) para rendimiento.
- **Transacciones**: `SqlTransaction` gestionada por un `IUnitOfWork` cuando el caso de uso abarca varias escrituras.
- **Esquema y migraciones**: gestionados con **scripts SQL versionados** en `Persistence/Scripts/` (por ejemplo con una herramienta tipo DbUp o ejecución idempotente), no con migraciones de ORM.

> **Nota**: verificar en cada publicación que `Microsoft.Data.SqlClient` no introduzca advertencias AOT. Evitar rutas de autenticación que dependan de reflexión (p. ej. algunos modos de Azure AD); preferir autenticación SQL o integrada según el entorno.

#### Ejemplo de repositorio ADO.NET documentado

```csharp
namespace HuimanNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de documentos basado en ADO.NET sobre SQL Server.
/// Compatible con Native AOT: no utiliza reflexión ni emisión de IL.
/// </summary>
public sealed class DocumentoRepository : IDocumentoRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="DocumentoRepository"/>.
    /// </summary>
    /// <param name="connectionFactory">Fábrica de conexiones a SQL Server.</param>
    public DocumentoRepository(ISqlConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory
            ?? throw new ArgumentNullException(nameof(connectionFactory));

    /// <summary>
    /// Obtiene un documento por su identificador, restringido a la empresa
    /// indicada para garantizar el aislamiento entre empresas cliente.
    /// </summary>
    /// <param name="id">Identificador único del documento.</param>
    /// <param name="empresaId">
    /// Empresa del usuario solicitante. El filtro es obligatorio: impide que
    /// una empresa acceda a documentos de otra.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// El <see cref="Documento"/> encontrado, o <c>null</c> si no existe
    /// o no pertenece a la empresa indicada.
    /// </returns>
    public async Task<Documento?> ObtenerPorIdAsync(
        Guid id, Guid empresaId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, EmpresaId, PeriodoId, TipoDocumento, NombreOriginal,
                   RutaBlob, TamanoBytes, Estado, FechaCarga, CargadoPorUsuarioId
            FROM   dbo.Documentos
            WHERE  Id = @Id AND EmpresaId = @EmpresaId
            """;

        await using var connection = _connectionFactory.Crear();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(
            new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        command.Parameters.Add(
            new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? DocumentoMapper.Map(reader)   // mapeo manual, sin reflexión
            : null;
    }
}
```

### Ejemplo de organización de endpoints

Los endpoints se agrupan por recurso mediante métodos de extensión (`MapDocumentoEndpoints`, `MapPeriodoEndpoints`) y se registran en `Program.cs`.

---

## 7. Estándar de documentación de código

**Requisito no negociable**: cada `class`, `interface`, `record`, `struct`, `enum`, **método** y **propiedad pública** debe llevar comentarios de documentación XML (`///`).

### 7.1 Configuración del proyecto

Activar la generación del archivo XML y tratar la falta de documentación como advertencia:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <!-- Sin esto, CS1591 avisa de miembros públicos sin documentar -->
  <NoWarn></NoWarn>
</PropertyGroup>
```

### 7.2 Etiquetas requeridas

| Etiqueta | Uso |
|----------|-----|
| `<summary>` | Descripción de la clase/método/propiedad. **Obligatorio.** |
| `<param>` | Cada parámetro de un método. |
| `<returns>` | Valor de retorno. |
| `<remarks>` | Detalles adicionales, notas de implementación. |
| `<exception>` | Excepciones que puede lanzar. |
| `<value>` | Descripción de lo que representa una propiedad. |
| `<example>` | Ejemplo de uso cuando aporta valor. |
| `<inheritdoc/>` | Heredar documentación de interfaz/base. |

### 7.3 Ejemplo de clase documentada

```csharp
namespace HuimanNet.Domain.Services;

/// <summary>
/// Valida que un documento cumpla las reglas de negocio de carga:
/// tipo de archivo permitido, tamaño máximo y período abierto.
/// </summary>
/// <remarks>
/// Este servicio de dominio es puro: no accede a infraestructura y es
/// completamente determinista, lo que facilita su prueba unitaria.
/// La validación ocurre <b>antes</b> de emitir cualquier URL SAS de escritura.
/// </remarks>
public sealed class ValidadorDeDocumento
{
    private readonly PoliticaDeCarga _politica;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ValidadorDeDocumento"/>.
    /// </summary>
    /// <param name="politica">
    /// Política vigente con las extensiones permitidas y el tamaño máximo.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="politica"/> es <c>null</c>.
    /// </exception>
    public ValidadorDeDocumento(PoliticaDeCarga politica)
    {
        _politica = politica ?? throw new ArgumentNullException(nameof(politica));
    }

    /// <summary>
    /// Valida una solicitud de carga de documento contra la política vigente.
    /// </summary>
    /// <param name="nombreArchivo">Nombre original del archivo aportado por el usuario.</param>
    /// <param name="tamano">Tamaño del archivo a cargar.</param>
    /// <param name="periodo">Período al que se asociará el documento.</param>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza cuando la extensión no está permitida o se excede el tamaño máximo.
    /// </exception>
    /// <exception cref="PeriodoCerradoException">
    /// Se lanza cuando el período ya no admite cargas.
    /// </exception>
    public void Validar(
        NombreArchivo nombreArchivo, TamanoArchivo tamano, PeriodoCarga periodo)
    {
        // Implementación...
    }
}
```

### 7.4 Ejemplo de propiedad documentada

```csharp
/// <summary>
/// Representa el tamaño inmutable de un archivo, validado en su construcción.
/// </summary>
public readonly record struct TamanoArchivo
{
    /// <summary>
    /// Obtiene el tamaño del archivo expresado en bytes.
    /// </summary>
    /// <value>Siempre mayor que cero; el límite superior lo define la política de carga.</value>
    public long Bytes { get; }

    /// <summary>
    /// Obtiene el tamaño expresado en megabytes, para presentación en la interfaz.
    /// </summary>
    /// <value>Valor derivado de <see cref="Bytes"/>, redondeado a dos decimales.</value>
    public decimal Megabytes => Math.Round(Bytes / 1_048_576m, 2);
}
```

---

## 8. Módulos funcionales (V1)

| Módulo | Descripción |
|--------|-------------|
| **Gestión de empresas y usuarios** | Alta de empresas cliente, usuarios y asignación de roles. |
| **Períodos de carga** | Apertura y cierre de períodos para recepción de documentos. |
| **Carga de documentos** | Subida de incidencias y datos de empleados por el cliente; validación de tipo y tamaño; emisión de SAS de escritura. |
| **Bandeja del operador** | Consulta y descarga de los documentos recibidos por período y empresa. |
| **Carga de resultados** | Subida de archivos de resultado o ajustes por el operador de nómina. |
| **Descarga de resultados** | Descarga de los archivos de resultado por el usuario de la empresa cliente. |
| **Seguimiento de estado** | Estado del ciclo: recibido · en proceso · resultados disponibles. |
| **Notificaciones** | Avisos por correo ante nuevas cargas y resultados listos. |
| **Auditoría** | Bitácora de cargas y **descargas**: quién, qué documento y cuándo. |

### Módulos previstos para V2+ (fuera del alcance actual)

Cálculo de nómina · reglas impositivas · generación automática de recibos PDF · integración bancaria · integración fiscal · reportes de costos laborales.

---

## 9. Mejores prácticas transversales

### Diseño y código
- **Inmutabilidad** en entidades de dominio y Value Objects (`record`, `readonly struct`).
- **Nullability** habilitada (`<Nullable>enable</Nullable>`) en toda la solución.
- **`sealed`** por defecto en clases no diseñadas para herencia.
- **Un archivo por tipo**; nombres en español coherentes con el dominio.
- **Guard clauses** para validar precondiciones.

### Arquitectura
- Respetar la **regla de dependencia** (hacia el centro).
- **Inyección de dependencias** en todas las capas.
- **CQRS** ligero para separar lecturas de escrituras.
- Repositorios sólo exponen **agregados**.

### API y AOT
- Endpoints **pequeños y tipados**.
- **`ProblemDetails`** (RFC 7807) para errores.
- **Versionado** de la API (`/api/v1/`).
- Validar compatibilidad AOT en cada compilación de release.

### Seguridad
- **JWT** con expiración corta y refresh tokens.
- **Aislamiento entre empresas**: **toda** consulta a documentos filtra por la empresa del token. Es el riesgo #1 del portal — nunca confiar en un identificador enviado por el cliente.
- **Autorización verificada en el servidor** antes de emitir cualquier URL SAS.
- **SAS de mínimo privilegio**: un solo blob, un solo permiso (lectura *o* escritura), vigencia de minutos.
- **Nombres de blob generados por el sistema** (GUID), nunca el nombre original del usuario → evita *path traversal* y colisiones.
- **Archivo subido = no confiable** hasta que el escaneo de malware lo declare limpio; hasta entonces no es descargable.
- **Nunca** registrar datos sensibles (identificadores fiscales, datos personales) en logs de forma legible.
- Validación de entrada en el borde (API) y en el dominio.
- **HTTPS** obligatorio.

### Calidad
- Cobertura de pruebas del dominio y aplicación **> 80%**.
- **`.editorconfig`** compartido para estilo consistente.
- Analizadores de Roslyn con **advertencias como errores** en CI.
- Documentación XML **obligatoria** (CS1591 activo).

---

## 10. Flujo de compilación y publicación

```bash
# Restaurar
dotnet restore

# Compilar toda la solución
dotnet build -c Release

# Ejecutar pruebas
dotnet test

# Publicar API con Native AOT (destino: App Service Linux)
dotnet publish src/HuimanNet.Api -c Release -r linux-x64

# Publicar web Blazor Server
dotnet publish src/HuimanNet.Web -c Release

# Publicar app MAUI — Android
dotnet publish src/HuimanNet.App -c Release -f net10.0-android

# Publicar app MAUI — iOS (requiere macOS)
dotnet publish src/HuimanNet.App -c Release -f net10.0-ios
```

---

## 11. Requisitos del entorno de desarrollo

- **.NET 10 SDK**
- Cargas de trabajo: `dotnet workload install maui`
- Toolchain de Native AOT (compilador C++ de la plataforma):
  - Windows: MSVC (Visual Studio Build Tools)
  - Linux: `clang`, `zlib1g-dev`
  - macOS: Xcode Command Line Tools
- IDE: Visual Studio 2022+ / JetBrains Rider / VS Code con extensión C# Dev Kit.

---

## 12. Convenciones de nombres

| Elemento | Convención | Ejemplo |
|----------|------------|---------|
| Proyecto | `HuimanNet.<Capa>` | `HuimanNet.Domain` |
| Namespace | Refleja carpeta | `HuimanNet.Domain.Entities` |
| Clase / Record | PascalCase | `Documento` |
| Interfaz | `I` + PascalCase | `IDocumentoRepository` |
| Método | PascalCase | `ObtenerPorIdAsync` |
| Propiedad | PascalCase | `FechaCarga` |
| Campo privado | `_camelCase` | `_connectionFactory` |
| Parámetro / variable local | camelCase | `periodoCarga` |
| ViewModel | `<Nombre>ViewModel` | `CargaDocumentosViewModel` |
| View — MAUI | `<Nombre>Page` | `CargaDocumentosPage` |
| Componente — Blazor | `<Nombre>.razor` | `CargaDocumentos.razor` |

---

*Documento vivo — se actualizará a medida que evolucione la arquitectura y los requisitos del proyecto.*
