using HuimanNet.Application.Common;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.RazonesSociales;

/// <summary>
/// Ejecuta <see cref="ListarRazonesSocialesQuery"/>.
/// </summary>
public sealed class ListarRazonesSocialesHandler
    : IManejadorDeConsulta<ListarRazonesSocialesQuery, IReadOnlyList<RazonSocialDto>>
{
    private readonly IRazonSocialRepository _razonesSociales;
    private readonly AutorizadorDeCasosDeUso _autorizador;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ListarRazonesSocialesHandler"/>.
    /// </summary>
    /// <param name="razonesSociales">Repositorio de razones sociales.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    public ListarRazonesSocialesHandler(IRazonSocialRepository razonesSociales, AutorizadorDeCasosDeUso autorizador)
    {
        _razonesSociales = razonesSociales;
        _autorizador = autorizador;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RazonSocialDto>> EjecutarAsync(
        ListarRazonesSocialesQuery consulta, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        Guid empresaId = _autorizador.ResolverEmpresa(consulta.EmpresaId);

        IReadOnlyList<RazonSocial> lista =
            await _razonesSociales.ListarPorEmpresaAsync(empresaId, consulta.SoloActivas, cancellationToken);

        return lista.Select(Mapeadores.ADto).ToList();
    }
}
