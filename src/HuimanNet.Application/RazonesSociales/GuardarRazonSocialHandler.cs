using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.RazonesSociales;

/// <summary>
/// Ejecuta <see cref="GuardarRazonSocialCommand"/>.
/// </summary>
public sealed class GuardarRazonSocialHandler : IManejadorDeComando<GuardarRazonSocialCommand, RazonSocialDto>
{
    private readonly IRazonSocialRepository _razonesSociales;
    private readonly IEmpresaRepository _empresas;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly RegistradorDeAuditoria _auditoria;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="GuardarRazonSocialHandler"/>.
    /// </summary>
    /// <param name="razonesSociales">Repositorio de razones sociales.</param>
    /// <param name="empresas">Repositorio de empresas.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="auditoria">Registro de auditoría.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    public GuardarRazonSocialHandler(
        IRazonSocialRepository razonesSociales,
        IEmpresaRepository empresas,
        AutorizadorDeCasosDeUso autorizador,
        RegistradorDeAuditoria auditoria,
        IUnitOfWork unitOfWork,
        TimeProvider reloj)
    {
        _razonesSociales = razonesSociales;
        _empresas = empresas;
        _autorizador = autorizador;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    /// <inheritdoc/>
    public async Task<RazonSocialDto> EjecutarAsync(
        GuardarRazonSocialCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ArgumentNullException.ThrowIfNull(comando.Datos);

        _autorizador.Exigir(AccionDelSistema.AdministrarEmpresas);
        Guid empresaId = _autorizador.ResolverEmpresa(comando.Datos.EmpresaId);

        _ = await _empresas.ObtenerPorIdAsync(empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La empresa '{empresaId}' no existe.");

        GuardarRazonSocialRequest d = comando.Datos;
        var configuracion = new ConfiguracionDeRazonSocial(
            d.TipoDeServicio, d.SubsidioAbsorbido, d.AplicaFaltasProporcionales, d.ModalidadDeComision,
            d.PorcentajeComision, d.ZonaIsn, d.TasaIva, d.PorcentajeOtrosCostos, d.PrimaDeRiesgo);

        RazonSocial razonSocial;

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        if (comando.RazonSocialId is null)
        {
            razonSocial = RazonSocial.Crear(
                empresaId, d.Nombre, d.Rfc, d.RegistroPatronal, d.Zona, configuracion, d.BancoDispersor, _reloj.GetUtcNow());
            await _razonesSociales.AgregarAsync(razonSocial, cancellationToken);
        }
        else
        {
            razonSocial = await _razonesSociales.ObtenerPorIdAsync(comando.RazonSocialId.Value, empresaId, cancellationToken)
                ?? throw new AccesoNoAutorizadoException($"La razón social '{comando.RazonSocialId}' no existe.");
            razonSocial.Actualizar(d.Nombre, d.Rfc, d.RegistroPatronal, d.Zona, configuracion, d.BancoDispersor, d.Activa);
            await _razonesSociales.ActualizarAsync(razonSocial, cancellationToken);
        }

        await _auditoria.ExitoAsync(
            AccionAuditada.AdministracionDeCatalogo, empresaId, nameof(RazonSocial), razonSocial.Id,
            comando.RazonSocialId is null ? "alta" : "actualizacion", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return Mapeadores.ADto(razonSocial);
    }
}
