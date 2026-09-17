using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Nomina;

/// <summary>
/// Ejecuta <see cref="CambiarEstadoCorridaCommand"/>.
/// </summary>
public sealed class CambiarEstadoCorridaHandler : IManejadorDeComando<CambiarEstadoCorridaCommand>
{
    private readonly ICorridaDeNominaRepository _corridas;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly RegistradorDeAuditoria _auditoria;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CambiarEstadoCorridaHandler"/>.
    /// </summary>
    /// <param name="corridas">Repositorio de corridas.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="auditoria">Registro de auditoría.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    public CambiarEstadoCorridaHandler(
        ICorridaDeNominaRepository corridas,
        AutorizadorDeCasosDeUso autorizador,
        RegistradorDeAuditoria auditoria,
        IUnitOfWork unitOfWork)
    {
        _corridas = corridas;
        _autorizador = autorizador;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task EjecutarAsync(CambiarEstadoCorridaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        _autorizador.Exigir(AccionDelSistema.AprobarNomina);
        Guid empresaId = _autorizador.ResolverEmpresa(comando.EmpresaId);

        CorridaDeNomina corrida = await _corridas.ObtenerAsync(comando.CorridaId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La corrida '{comando.CorridaId}' no existe.");

        EstadoDeCorrida anterior = corrida.Estado;

        switch (comando.Estado)
        {
            case EstadoDeCorrida.Aprobada:
                corrida.Aprobar(comando.Observaciones);
                break;
            case EstadoDeCorrida.Descartada:
                corrida.Descartar(comando.Observaciones);
                break;
            default:
                throw new NominaInvalidaException("Sólo puede aprobarse o descartarse una corrida.");
        }

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _corridas.ActualizarAsync(corrida, cancellationToken);
        await _auditoria.ExitoAsync(
            AccionAuditada.CambioEstadoCorrida, empresaId, nameof(CorridaDeNomina), corrida.Id,
            $"{anterior}->{comando.Estado}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);
    }
}
