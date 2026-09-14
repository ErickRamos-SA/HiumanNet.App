using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Auditoria;
using HuimanNet.Contracts.Common;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Services;

namespace HuimanNet.Application.Auditoria.Queries;

/// <summary>
/// Consulta la bitácora de auditoría.
/// </summary>
/// <param name="EmpresaId">
/// Empresa a la que se restringe la consulta, o <c>null</c> para todas.
/// Sólo el administrador puede consultar sin restricción de empresa.
/// </param>
/// <param name="Desde">Límite inferior del rango de fechas, en UTC.</param>
/// <param name="Hasta">Límite superior del rango de fechas, en UTC.</param>
/// <param name="Accion">Acción a filtrar, o <c>null</c> para todas.</param>
/// <param name="UsuarioId">Usuario a filtrar, o <c>null</c> para todos.</param>
/// <param name="Pagina">Número de página, empezando en 1.</param>
/// <param name="TamanoPagina">Número de asientos por página.</param>
public sealed record ConsultarBitacoraQuery(
    Guid? EmpresaId,
    DateTimeOffset Desde,
    DateTimeOffset Hasta,
    AccionAuditada? Accion,
    Guid? UsuarioId,
    int Pagina,
    int TamanoPagina);

/// <summary>
/// Ejecuta <see cref="ConsultarBitacoraQuery"/> acotando el ámbito al que el
/// rol del solicitante tiene derecho.
/// </summary>
public sealed class ConsultarBitacoraHandler
    : IManejadorDeConsulta<ConsultarBitacoraQuery, PaginaDto<RegistroAuditoriaDto>>
{
    private readonly IConsultasAuditoria _consultas;
    private readonly IUsuarioActual _usuarioActual;
    private readonly PoliticaDeAcceso _politicaDeAcceso;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultarBitacoraHandler"/>.
    /// </summary>
    /// <param name="consultas">Lado de lectura de la bitácora.</param>
    /// <param name="usuarioActual">Identidad efectiva del solicitante.</param>
    /// <param name="politicaDeAcceso">Matriz de permisos por rol.</param>
    public ConsultarBitacoraHandler(
        IConsultasAuditoria consultas,
        IUsuarioActual usuarioActual,
        PoliticaDeAcceso politicaDeAcceso)
    {
        _consultas = consultas;
        _usuarioActual = usuarioActual;
        _politicaDeAcceso = politicaDeAcceso;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="consulta"/> es <c>null</c>.</exception>
    /// <exception cref="Domain.Exceptions.AccesoNoAutorizadoException">
    /// Se lanza si el rol no alcanza para el ámbito solicitado.
    /// </exception>
    public async Task<PaginaDto<RegistroAuditoriaDto>> EjecutarAsync(
        ConsultarBitacoraQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        // Un usuario de empresa cliente sólo puede ver la bitácora de una de sus
        // empresas: la elegida o, si no eligió, la principal.
        Guid? empresaId = _politicaDeAcceso.OperaSobreTodasLasEmpresas(_usuarioActual.Rol)
            ? consulta.EmpresaId
            : _politicaDeAcceso.ResolverEmpresaObjetivo(
                _usuarioActual.Rol, _usuarioActual.EmpresaId, _usuarioActual.Empresas, consulta.EmpresaId);

        _politicaDeAcceso.GarantizarPuedeConsultarBitacora(_usuarioActual.Rol, empresaId);

        var filtro = new FiltroDeAuditoria(
            empresaId,
            consulta.Desde,
            consulta.Hasta,
            consulta.Accion,
            consulta.UsuarioId,
            Math.Max(1, consulta.Pagina),
            IConsultasAuditoria.NormalizarTamanoPagina(consulta.TamanoPagina));

        return await _consultas.ConsultarAsync(filtro, cancellationToken);
    }
}
