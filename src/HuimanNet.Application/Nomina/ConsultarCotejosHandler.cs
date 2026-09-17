using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Nomina;

/// <summary>
/// Ejecuta <see cref="ListarCotejosQuery"/> y <see cref="ObtenerCotejoQuery"/>.
/// </summary>
public sealed class ConsultarCotejosHandler
    : IManejadorDeConsulta<ListarCotejosQuery, IReadOnlyList<CotejoDto>>,
      IManejadorDeConsulta<ObtenerCotejoQuery, CotejoDto>
{
    private readonly IConsultasNomina _consultas;
    private readonly ICotejoRepository _cotejos;
    private readonly IUsuarioRepository _usuarios;
    private readonly AutorizadorDeCasosDeUso _autorizador;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConsultarCotejosHandler"/>.
    /// </summary>
    /// <param name="consultas">Lado de lectura de nómina.</param>
    /// <param name="cotejos">Repositorio de cotejos.</param>
    /// <param name="usuarios">Repositorio de usuarios.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    public ConsultarCotejosHandler(
        IConsultasNomina consultas, ICotejoRepository cotejos, IUsuarioRepository usuarios, AutorizadorDeCasosDeUso autorizador)
    {
        _consultas = consultas;
        _cotejos = cotejos;
        _usuarios = usuarios;
        _autorizador = autorizador;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CotejoDto>> EjecutarAsync(
        ListarCotejosQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        _autorizador.Exigir(AccionDelSistema.ConsultarNomina);
        Guid empresaId = _autorizador.ResolverEmpresa(consulta.EmpresaId);

        return await _consultas.ListarCotejosAsync(consulta.CorridaId, empresaId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CotejoDto> EjecutarAsync(ObtenerCotejoQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        _autorizador.Exigir(AccionDelSistema.ConsultarNomina);
        Guid empresaId = _autorizador.ResolverEmpresa(consulta.EmpresaId);

        CotejoDeNomina cotejo = await _cotejos.ObtenerAsync(consulta.CotejoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El cotejo '{consulta.CotejoId}' no existe.");

        Usuario? usuario = await _usuarios.ObtenerPorIdAsync(cotejo.UsuarioId, cancellationToken);

        return Mapeadores.ADto(cotejo, usuario?.NombreCompleto ?? "(usuario desconocido)", incluirDetalle: true);
    }
}
