using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Empresas;

/// <summary>
/// Crea una empresa cliente.
/// </summary>
/// <param name="RazonSocial">Razón social.</param>
/// <param name="IdentificadorFiscal">Identificador fiscal.</param>
public sealed record CrearEmpresaCommand(string RazonSocial, string IdentificadorFiscal);

/// <summary>
/// Actualiza una empresa cliente.
/// </summary>
/// <param name="EmpresaId">Empresa a actualizar.</param>
/// <param name="RazonSocial">Razón social.</param>
/// <param name="IdentificadorFiscal">Identificador fiscal.</param>
/// <param name="Activa">Estado.</param>
public sealed record ActualizarEmpresaCommand(Guid EmpresaId, string RazonSocial, string IdentificadorFiscal, bool Activa);

/// <summary>
/// Lista las empresas con sus contadores administrativos.
/// </summary>
/// <param name="SoloActivas">Si es <c>true</c>, omite las dadas de baja.</param>
public sealed record ListarEmpresasDetalleQuery(bool SoloActivas);

/// <summary>
/// Obtiene el detalle de una empresa.
/// </summary>
/// <param name="EmpresaId">Empresa consultada, o <c>null</c> para la del solicitante.</param>
public sealed record ObtenerEmpresaQuery(Guid? EmpresaId);

/// <summary>
/// Ejecuta <see cref="CrearEmpresaCommand"/> y <see cref="ActualizarEmpresaCommand"/>.
/// </summary>
/// <remarks>Reservado al administrador: dar de alta clientes es una decisión comercial.</remarks>
public sealed class GuardarEmpresaHandler
    : IManejadorDeComando<CrearEmpresaCommand, EmpresaDetalleDto>,
      IManejadorDeComando<ActualizarEmpresaCommand, EmpresaDetalleDto>
{
    private readonly IEmpresaRepository _empresas;
    private readonly IConsultasEmpresas _consultas;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly RegistradorDeAuditoria _auditoria;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="GuardarEmpresaHandler"/>.
    /// </summary>
    /// <param name="empresas">Repositorio de empresas.</param>
    /// <param name="consultas">Lado de lectura de empresas.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="auditoria">Registro de auditoría.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    public GuardarEmpresaHandler(
        IEmpresaRepository empresas,
        IConsultasEmpresas consultas,
        AutorizadorDeCasosDeUso autorizador,
        RegistradorDeAuditoria auditoria,
        IUnitOfWork unitOfWork,
        TimeProvider reloj)
    {
        _empresas = empresas;
        _consultas = consultas;
        _autorizador = autorizador;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    /// <inheritdoc/>
    public async Task<EmpresaDetalleDto> EjecutarAsync(
        CrearEmpresaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        _autorizador.Exigir(AccionDelSistema.AdministrarEmpresas);

        Empresa empresa = Empresa.Crear(comando.RazonSocial, comando.IdentificadorFiscal, _reloj.GetUtcNow());

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _empresas.AgregarAsync(empresa, cancellationToken);
        await _auditoria.ExitoAsync(
            AccionAuditada.AdministracionDeCatalogo, empresa.Id, nameof(Empresa), empresa.Id, "alta", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return await _consultas.ObtenerDetalleAsync(empresa.Id, cancellationToken)
            ?? new EmpresaDetalleDto(empresa.Id, empresa.RazonSocial, empresa.IdentificadorFiscal, empresa.Activa, empresa.FechaAlta, 0, 0, 0);
    }

    /// <inheritdoc/>
    public async Task<EmpresaDetalleDto> EjecutarAsync(
        ActualizarEmpresaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        _autorizador.Exigir(AccionDelSistema.AdministrarEmpresas);

        Empresa empresa = await _empresas.ObtenerPorIdAsync(comando.EmpresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La empresa '{comando.EmpresaId}' no existe.");

        empresa.ActualizarDatos(comando.RazonSocial, comando.IdentificadorFiscal);

        if (comando.Activa)
        {
            empresa.Activar();
        }
        else
        {
            empresa.Desactivar();
        }

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _empresas.ActualizarAsync(empresa, cancellationToken);
        await _auditoria.ExitoAsync(
            AccionAuditada.AdministracionDeCatalogo, empresa.Id, nameof(Empresa), empresa.Id,
            comando.Activa ? "actualizacion" : "baja", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return await _consultas.ObtenerDetalleAsync(empresa.Id, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La empresa '{comando.EmpresaId}' no existe.");
    }
}

/// <summary>
/// Ejecuta <see cref="ListarEmpresasDetalleQuery"/> y <see cref="ObtenerEmpresaQuery"/>.
/// </summary>
public sealed class ConsultarEmpresasHandler
    : IManejadorDeConsulta<ListarEmpresasDetalleQuery, IReadOnlyList<EmpresaDetalleDto>>,
      IManejadorDeConsulta<ObtenerEmpresaQuery, EmpresaDetalleDto>
{
    private readonly IConsultasEmpresas _consultas;
    private readonly AutorizadorDeCasosDeUso _autorizador;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultarEmpresasHandler"/>.
    /// </summary>
    /// <param name="consultas">Lado de lectura de empresas.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    public ConsultarEmpresasHandler(IConsultasEmpresas consultas, AutorizadorDeCasosDeUso autorizador)
    {
        _consultas = consultas;
        _autorizador = autorizador;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EmpresaDetalleDto>> EjecutarAsync(
        ListarEmpresasDetalleQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        if (!_autorizador.EsTransversal)
        {
            throw new AccesoNoAutorizadoException("Sólo los roles transversales consultan el catálogo de empresas.");
        }

        return await _consultas.ListarDetalleAsync(consulta.SoloActivas, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<EmpresaDetalleDto> EjecutarAsync(
        ObtenerEmpresaQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        Guid empresaId = _autorizador.ResolverEmpresa(consulta.EmpresaId);

        return await _consultas.ObtenerDetalleAsync(empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La empresa '{empresaId}' no existe.");
    }
}
