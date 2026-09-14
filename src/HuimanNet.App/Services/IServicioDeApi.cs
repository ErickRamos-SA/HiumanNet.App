using System.Net;
using HuimanNet.Contracts.Catalogos;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Contracts.Empleados;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Contracts.Incidencias;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Enums;

namespace HuimanNet.App.Services;

/// <summary>
/// Rechazo de la API con el mensaje que redactó el servidor.
/// </summary>
public sealed class ErrorDeApiException : Exception
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ErrorDeApiException"/>.
    /// </summary>
    /// <param name="mensaje">Mensaje para el usuario.</param>
    /// <param name="estado">Código HTTP de la respuesta.</param>
    public ErrorDeApiException(string mensaje, HttpStatusCode estado)
        : base(mensaje)
        => Estado = estado;

    /// <summary>Inicializa una nueva instancia de <see cref="ErrorDeApiException"/>.</summary>
    public ErrorDeApiException()
    {
    }

    /// <summary>Inicializa una nueva instancia de <see cref="ErrorDeApiException"/>.</summary>
    /// <param name="message">Mensaje.</param>
    public ErrorDeApiException(string message)
        : base(message)
    {
    }

    /// <summary>Inicializa una nueva instancia de <see cref="ErrorDeApiException"/>.</summary>
    /// <param name="message">Mensaje.</param>
    /// <param name="innerException">Excepción de origen.</param>
    public ErrorDeApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Obtiene el código HTTP de la respuesta.</summary>
    /// <value>Por ejemplo <see cref="HttpStatusCode.BadRequest"/>.</value>
    public HttpStatusCode Estado { get; }
}

/// <summary>
/// Archivo generado por el servidor (por ejemplo, la exportación de una corrida).
/// </summary>
/// <param name="Nombre">Nombre sugerido.</param>
/// <param name="TipoDeContenido">Tipo MIME.</param>
/// <param name="Contenido">Bytes del archivo.</param>
public sealed record ArchivoDescargado(string Nombre, string TipoDeContenido, byte[] Contenido);

/// <summary>
/// Cliente tipado de la API de HuimanNet para la app móvil.
/// </summary>
/// <remarks>
/// Cubre las mismas operaciones que la web: la web las ejecuta en proceso y la
/// app por HTTPS, pero ambas llegan a los mismos casos de uso, con la misma
/// autorización y las mismas reglas. Todas las respuestas se deserializan con
/// el contexto generado en compilación, sin reflexión.
/// </remarks>
public interface IServicioDeApi
{
    /// <summary>Obtiene la configuración pública del servidor (modo de identidad, versión).</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La configuración.</returns>
    Task<ConfiguracionPublicaDto> ObtenerConfiguracionAsync(CancellationToken cancellationToken = default);

    /// <summary>Inicia sesión con correo y contraseña (modo de identidad local).</summary>
    /// <param name="peticion">Credenciales.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El token emitido y la identidad.</returns>
    Task<IniciarSesionResponse> IniciarSesionLocalAsync(IniciarSesionRequest peticion, CancellationToken cancellationToken = default);

    /// <summary>Obtiene la identidad efectiva del usuario.</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Usuario, rol, idioma y acciones permitidas.</returns>
    Task<UsuarioActualDto> ObtenerUsuarioActualAsync(CancellationToken cancellationToken = default);

    /// <summary>Cambia la contraseña del usuario (modo local).</summary>
    /// <param name="peticion">Contraseña actual y nueva.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task CambiarContrasenaAsync(CambiarContrasenaRequest peticion, CancellationToken cancellationToken = default);

    /// <summary>Guarda las preferencias del usuario (idioma).</summary>
    /// <param name="peticion">Preferencias.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task ActualizarPreferenciasAsync(ActualizarPreferenciasRequest peticion, CancellationToken cancellationToken = default);

    /// <summary>Obtiene los indicadores y pendientes del inicio.</summary>
    /// <param name="empresaId">Empresa elegida por una empresa cliente con varias empresas, o <c>null</c>.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El resumen.</returns>
    Task<ResumenDeInicioDto> ObtenerResumenDeInicioAsync(Guid? empresaId, CancellationToken cancellationToken = default);

    /// <summary>Lista las empresas activas (sólo roles transversales).</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Las empresas.</returns>
    Task<IReadOnlyList<EmpresaDto>> ListarEmpresasAsync(CancellationToken cancellationToken = default);

    /// <summary>Lista los períodos.</summary>
    /// <param name="empresaId">Empresa de trabajo (roles transversales), o <c>null</c>.</param>
    /// <param name="incluirCerrados">Si se incluyen los cerrados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Los períodos.</returns>
    Task<IReadOnlyList<PeriodoDto>> ListarPeriodosAsync(Guid? empresaId, bool incluirCerrados, CancellationToken cancellationToken = default);

    /// <summary>Lista los documentos de un período.</summary>
    /// <param name="periodoId">Período.</param>
    /// <param name="empresaId">Empresa de trabajo, o <c>null</c>.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Los documentos.</returns>
    Task<IReadOnlyList<DocumentoDto>> ListarDocumentosAsync(Guid periodoId, Guid? empresaId, CancellationToken cancellationToken = default);

    /// <summary>Sube un documento: autoriza, sube directo al almacenamiento y confirma con la huella.</summary>
    /// <param name="periodoId">Período.</param>
    /// <param name="tipo">Tipo de documento.</param>
    /// <param name="nombreArchivo">Nombre original.</param>
    /// <param name="contenido">Contenido.</param>
    /// <param name="empresaId">Empresa de trabajo, o <c>null</c>.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El documento confirmado.</returns>
    Task<DocumentoDto> SubirDocumentoAsync(
        Guid periodoId, TipoDocumento tipo, string nombreArchivo, Stream contenido, Guid? empresaId, CancellationToken cancellationToken = default);

    /// <summary>Obtiene un enlace temporal de descarga.</summary>
    /// <param name="documentoId">Documento.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El enlace, ya absoluto.</returns>
    Task<EnlaceDescargaResponse> ObtenerEnlaceDeDescargaAsync(Guid documentoId, CancellationToken cancellationToken = default);

    /// <summary>Lista las incidencias de un período (una fila por contrato vigente).</summary>
    /// <param name="periodoId">Período.</param>
    /// <param name="empresaId">Empresa de trabajo, o <c>null</c>.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Las filas.</returns>
    Task<IReadOnlyList<IncidenciaDto>> ListarIncidenciasAsync(Guid periodoId, Guid? empresaId, CancellationToken cancellationToken = default);

    /// <summary>Guarda la incidencia de un contrato.</summary>
    /// <param name="peticion">Datos capturados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La incidencia guardada.</returns>
    Task<IncidenciaDto> GuardarIncidenciaAsync(GuardarIncidenciaRequest peticion, CancellationToken cancellationToken = default);

    /// <summary>Elimina una incidencia (el contrato vuelve a período completo).</summary>
    /// <param name="incidenciaId">Incidencia.</param>
    /// <param name="empresaId">Empresa de trabajo, o <c>null</c>.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task EliminarIncidenciaAsync(Guid incidenciaId, Guid? empresaId, CancellationToken cancellationToken = default);

    /// <summary>Importa las incidencias de un archivo de incidencias ya cargado en el período.</summary>
    /// <param name="peticion">Documento del período y empresa de trabajo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Filas leídas, creadas, actualizadas y errores.</returns>
    Task<ResultadoDeImportacionDto> ImportarIncidenciasAsync(ImportarIncidenciasRequest peticion, CancellationToken cancellationToken = default);

    /// <summary>Lista las corridas de nómina de un período.</summary>
    /// <param name="periodoId">Período.</param>
    /// <param name="empresaId">Empresa de trabajo, o <c>null</c>.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Las corridas.</returns>
    Task<IReadOnlyList<CorridaDeNominaDto>> ListarCorridasAsync(Guid periodoId, Guid? empresaId, CancellationToken cancellationToken = default);

    /// <summary>Calcula la nómina de un período.</summary>
    /// <param name="peticion">Período y observaciones.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La corrida creada.</returns>
    Task<CorridaDeNominaDto> CalcularNominaAsync(CalcularNominaRequest peticion, CancellationToken cancellationToken = default);

    /// <summary>Obtiene el resumen de una corrida con sus resultados y su facturación.</summary>
    /// <param name="corridaId">Corrida.</param>
    /// <param name="empresaId">Empresa de trabajo, o <c>null</c>.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El resumen.</returns>
    Task<ResumenDeCorridaDto> ObtenerResumenDeCorridaAsync(Guid corridaId, Guid? empresaId, CancellationToken cancellationToken = default);

    /// <summary>Obtiene el detalle por concepto de un trabajador.</summary>
    /// <param name="resultadoId">Resultado.</param>
    /// <param name="empresaId">Empresa de trabajo, o <c>null</c>.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El detalle.</returns>
    Task<DetalleDeResultadoDto> ObtenerDetalleDeResultadoAsync(Guid resultadoId, Guid? empresaId, CancellationToken cancellationToken = default);

    /// <summary>Aprueba o descarta una corrida.</summary>
    /// <param name="corridaId">Corrida.</param>
    /// <param name="peticion">Estado nuevo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task CambiarEstadoCorridaAsync(Guid corridaId, CambiarEstadoCorridaRequest peticion, CancellationToken cancellationToken = default);

    /// <summary>Descarga la exportación CSV de una corrida.</summary>
    /// <param name="corridaId">Corrida.</param>
    /// <param name="empresaId">Empresa de trabajo, o <c>null</c>.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El archivo.</returns>
    Task<ArchivoDescargado> ExportarCorridaAsync(Guid corridaId, Guid? empresaId, CancellationToken cancellationToken = default);

    /// <summary>Lista empleados de forma paginada.</summary>
    /// <param name="empresaId">Empresa de trabajo, o <c>null</c>.</param>
    /// <param name="texto">Texto a buscar, o <c>null</c>.</param>
    /// <param name="pagina">Página, desde 1.</param>
    /// <param name="tamanoPagina">Elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La página.</returns>
    Task<PaginaDto<EmpleadoResumenDto>> ListarEmpleadosAsync(
        Guid? empresaId, string? texto, int pagina, int tamanoPagina, CancellationToken cancellationToken = default);

    /// <summary>Obtiene un empleado con sus contratos.</summary>
    /// <param name="empleadoId">Empleado.</param>
    /// <param name="empresaId">Empresa de trabajo, o <c>null</c>.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El empleado.</returns>
    Task<EmpleadoDto> ObtenerEmpleadoAsync(Guid empleadoId, Guid? empresaId, CancellationToken cancellationToken = default);

    /// <summary>Obtiene la explicación completa de un esquema de pago.</summary>
    /// <param name="esquema">Esquema.</param>
    /// <param name="idioma">Idioma de la narrativa.</param>
    /// <param name="empresaId">Empresa (para incluir sus sustituciones), o <c>null</c>.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La explicación.</returns>
    Task<ExplicacionCompletaDto> ObtenerExplicacionAsync(
        EsquemaDePago esquema, Idioma idioma, Guid? empresaId, CancellationToken cancellationToken = default);

    /// <summary>Lista los usuarios (administración).</summary>
    /// <param name="empresaId">Empresa a filtrar, o <c>null</c>.</param>
    /// <param name="incluirInactivos">Si se incluyen los desactivados.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Los usuarios.</returns>
    Task<IReadOnlyList<UsuarioDto>> ListarUsuariosAsync(Guid? empresaId, bool incluirInactivos, CancellationToken cancellationToken = default);

    /// <summary>Evalúa una fórmula de prueba con el catálogo vigente.</summary>
    /// <param name="peticion">Fórmula, esquema y valores.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El resultado o el error.</returns>
    Task<ProbarFormulaResponse> ProbarFormulaAsync(ProbarFormulaRequest peticion, CancellationToken cancellationToken = default);
}
