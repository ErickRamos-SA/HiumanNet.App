using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empleados;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Contracts.Incidencias;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Contracts.Usuarios;

namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Lado de lectura del catálogo de empresas cliente.
/// </summary>
public interface IConsultasEmpresas
{
    /// <summary>
    /// Lista las empresas registradas.
    /// </summary>
    /// <param name="soloActivas">Si es <c>true</c>, omite las empresas dadas de baja.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las empresas ordenadas por razón social.</returns>
    /// <remarks>
    /// Sólo la consumen los roles transversales: un usuario de empresa cliente
    /// no tiene por qué conocer el catálogo completo.
    /// </remarks>
    Task<IReadOnlyList<EmpresaDto>> ListarAsync(
        bool soloActivas, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una empresa por su identificador.
    /// </summary>
    /// <param name="empresaId">Empresa consultada.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La empresa, o <c>null</c> si no existe.</returns>
    Task<EmpresaDto?> ObtenerAsync(Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista las empresas con sus contadores administrativos.
    /// </summary>
    /// <param name="soloActivas">Si es <c>true</c>, omite las empresas dadas de baja.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las empresas ordenadas por razón social.</returns>
    Task<IReadOnlyList<EmpresaDetalleDto>> ListarDetalleAsync(
        bool soloActivas, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el detalle administrativo de una empresa.
    /// </summary>
    /// <param name="empresaId">Empresa consultada.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El detalle, o <c>null</c> si no existe.</returns>
    Task<EmpresaDetalleDto?> ObtenerDetalleAsync(Guid empresaId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Lado de lectura de empleados y contratos.
/// </summary>
public interface IConsultasEmpleados
{
    /// <summary>
    /// Lista los empleados de forma paginada.
    /// </summary>
    /// <param name="empresas">Empresas consultadas, o <c>null</c> para todas (sólo roles transversales).</param>
    /// <param name="soloActivos">Si es <c>true</c>, omite los dados de baja.</param>
    /// <param name="texto">Texto a buscar en clave o nombre, o <c>null</c>.</param>
    /// <param name="pagina">Número de página, empezando en 1.</param>
    /// <param name="tamanoPagina">Elementos por página.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La página de empleados, ordenada por empresa y clave.</returns>
    Task<PaginaDto<EmpleadoResumenDto>> ListarAsync(
        IReadOnlyCollection<Guid>? empresas, bool soloActivos, string? texto, int pagina, int tamanoPagina,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un empleado con sus contratos.
    /// </summary>
    /// <param name="empleadoId">Empleado consultado.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El empleado, o <c>null</c>.</returns>
    Task<EmpleadoDto?> ObtenerAsync(Guid empleadoId, Guid empresaId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Lado de lectura de incidencias.
/// </summary>
public interface IConsultasIncidencias
{
    /// <summary>
    /// Lista una fila por cada contrato vigente en el período, con la incidencia
    /// capturada o, si no la hay, con las cantidades de un período completo.
    /// </summary>
    /// <param name="periodoId">Período consultado.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="fechaDeReferencia">Fecha con la que se determina la vigencia de los contratos.</param>
    /// <param name="diasPeriodoPredeterminados">Días del período para las filas sin captura.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las filas ordenadas por clave de empleado.</returns>
    Task<IReadOnlyList<IncidenciaDto>> ListarPorPeriodoAsync(
        Guid periodoId, Guid empresaId, DateOnly fechaDeReferencia, decimal diasPeriodoPredeterminados,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lado de lectura de corridas, resultados y cotejos.
/// </summary>
public interface IConsultasNomina
{
    /// <summary>
    /// Lista las corridas de un período.
    /// </summary>
    /// <param name="periodoId">Período consultado.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las corridas de la más reciente a la más antigua.</returns>
    Task<IReadOnlyList<CorridaDeNominaDto>> ListarCorridasAsync(
        Guid periodoId, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista las corridas más recientes de una empresa o de todas.
    /// </summary>
    /// <param name="empresaId">Empresa, o <c>null</c> para todas.</param>
    /// <param name="limite">Número máximo de corridas.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las corridas de la más reciente a la más antigua.</returns>
    Task<IReadOnlyList<CorridaDeNominaDto>> ListarRecientesAsync(
        Guid? empresaId, int limite, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una corrida.
    /// </summary>
    /// <param name="corridaId">Corrida consultada.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La corrida, o <c>null</c>.</returns>
    Task<CorridaDeNominaDto?> ObtenerCorridaAsync(
        Guid corridaId, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los resultados de una corrida.
    /// </summary>
    /// <param name="corridaId">Corrida consultada.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los resultados ordenados por razón social y clave de empleado.</returns>
    Task<IReadOnlyList<ResultadoDeNominaDto>> ListarResultadosAsync(
        Guid corridaId, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el resumen de un resultado.
    /// </summary>
    /// <param name="resultadoId">Resultado consultado.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El resultado, o <c>null</c>.</returns>
    Task<ResultadoDeNominaDto?> ObtenerResultadoAsync(
        Guid resultadoId, Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los cotejos de una corrida, sin detalle.
    /// </summary>
    /// <param name="corridaId">Corrida consultada.</param>
    /// <param name="empresaId">Empresa del solicitante.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los cotejos del más reciente al más antiguo.</returns>
    Task<IReadOnlyList<CotejoDto>> ListarCotejosAsync(
        Guid corridaId, Guid empresaId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Lado de lectura de usuarios para la administración.
/// </summary>
public interface IConsultasUsuarios
{
    /// <summary>
    /// Lista los usuarios.
    /// </summary>
    /// <param name="empresaId">Empresa a filtrar, o <c>null</c> para todos.</param>
    /// <param name="incluirInactivos">Si es <c>true</c>, incluye los desactivados.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los usuarios ordenados por nombre.</returns>
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(
        Guid? empresaId, bool incluirInactivos, CancellationToken cancellationToken = default);
}

/// <summary>
/// Lado de lectura de los indicadores del panel de inicio.
/// </summary>
public interface IConsultasInicio
{
    /// <summary>
    /// Calcula los indicadores del panel.
    /// </summary>
    /// <param name="empresaId">Empresa del usuario, o <c>null</c> para el ámbito transversal.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los indicadores.</returns>
    Task<ResumenDeInicioDto> ObtenerResumenAsync(Guid? empresaId, CancellationToken cancellationToken = default);
}
