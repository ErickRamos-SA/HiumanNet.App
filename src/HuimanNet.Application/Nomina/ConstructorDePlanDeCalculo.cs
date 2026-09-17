using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Nomina;

/// <summary>
/// Carga los catálogos y resuelve vigencias y sobrescrituras por empresa.
/// </summary>
/// <remarks>
/// Reglas de resolución, por clave:
/// <list type="number">
///   <item><description>Sólo cuentan las entradas vigentes en la fecha.</description></item>
///   <item><description>La entrada de la empresa prevalece sobre la global.</description></item>
///   <item><description>Entre varias vigentes del mismo ámbito gana la de inicio de vigencia más reciente.</description></item>
/// </list>
/// Para los conceptos no hay vigencia: la entrada de la empresa sustituye a la
/// global con la misma clave, y si está inactiva el concepto queda excluido
/// para esa empresa.
/// </remarks>
public sealed class ConstructorDePlanDeCalculo
{
    private readonly ICatalogoDeCalculoRepository _catalogos;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ConstructorDePlanDeCalculo"/>.
    /// </summary>
    /// <param name="catalogos">Repositorio de catálogos.</param>
    public ConstructorDePlanDeCalculo(ICatalogoDeCalculoRepository catalogos) => _catalogos = catalogos;

    /// <summary>
    /// Resuelve el catálogo de una empresa para una fecha.
    /// </summary>
    /// <param name="empresaId">Empresa, o <c>null</c> para el catálogo global.</param>
    /// <param name="fecha">Fecha de referencia.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El catálogo resuelto.</returns>
    public async Task<CatalogoResuelto> ResolverAsync(
        Guid? empresaId, DateOnly fecha, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ParametroDeCalculo> parametros = await _catalogos.ListarParametrosAsync(empresaId, cancellationToken);
        IReadOnlyList<TablaDeRangos> tablas = await _catalogos.ListarTablasAsync(empresaId, cancellationToken);
        IReadOnlyList<ConceptoDeNomina> conceptos = await _catalogos.ListarConceptosAsync(empresaId, cancellationToken);

        return new CatalogoResuelto(
            fecha,
            ResolverVigentes(parametros, empresaId, fecha, static p => p.Clave, static p => p.EmpresaId, static p => p.EstaVigenteEn, static p => p.VigenteDesde),
            ResolverVigentes(tablas, empresaId, fecha, static t => t.Clave, static t => t.EmpresaId, static t => t.EstaVigenteEn, static t => t.VigenteDesde),
            ResolverConceptos(conceptos, empresaId));
    }

    /// <summary>
    /// Elige, por clave, la entrada vigente en la fecha que aplica a la empresa:
    /// la propia de la empresa antes que la general y, a igualdad, la de inicio
    /// de vigencia más reciente.
    /// </summary>
    /// <typeparam name="T">Parámetro o tabla.</typeparam>
    /// <param name="entradas">Entradas del catálogo general y de la empresa.</param>
    /// <param name="empresaId">Empresa que se calcula, o <c>null</c> para el catálogo general.</param>
    /// <param name="fecha">Fecha de referencia.</param>
    /// <param name="clave">Obtiene la clave de una entrada.</param>
    /// <param name="empresa">Obtiene la empresa de una entrada.</param>
    /// <param name="vigente">Obtiene si una entrada está vigente en una fecha.</param>
    /// <param name="desde">Obtiene el inicio de vigencia de una entrada.</param>
    /// <returns>Una entrada por clave.</returns>
    private static IReadOnlyList<T> ResolverVigentes<T>(
        IReadOnlyList<T> entradas,
        Guid? empresaId,
        DateOnly fecha,
        Func<T, string> clave,
        Func<T, Guid?> empresa,
        Func<T, Func<DateOnly, bool>> vigente,
        Func<T, DateOnly> desde)
    {
        var elegidos = new Dictionary<string, T>(StringComparer.Ordinal);

        foreach (T entrada in entradas)
        {
            Guid? ambito = empresa(entrada);

            if (ambito is not null && ambito != empresaId)
            {
                continue;
            }

            if (!vigente(entrada)(fecha))
            {
                continue;
            }

            string k = clave(entrada);

            if (!elegidos.TryGetValue(k, out T? actual) || EsMejor(entrada, actual, empresa, desde))
            {
                elegidos[k] = entrada;
            }
        }

        return [.. elegidos.Values];
    }

    /// <summary>Decide si una entrada debe sustituir a la elegida para su clave.</summary>
    /// <typeparam name="T">Parámetro o tabla.</typeparam>
    /// <param name="candidato">Entrada nueva.</param>
    /// <param name="actual">Entrada elegida hasta ahora.</param>
    /// <param name="empresa">Obtiene la empresa de una entrada.</param>
    /// <param name="desde">Obtiene el inicio de vigencia de una entrada.</param>
    /// <returns>
    /// <c>true</c> si el candidato es de la empresa y la actual no, o si ambos
    /// tienen el mismo ámbito y el candidato empieza después.
    /// </returns>
    private static bool EsMejor<T>(T candidato, T actual, Func<T, Guid?> empresa, Func<T, DateOnly> desde)
    {
        bool candidatoDeEmpresa = empresa(candidato) is not null;
        bool actualDeEmpresa = empresa(actual) is not null;

        if (candidatoDeEmpresa != actualDeEmpresa)
        {
            return candidatoDeEmpresa;
        }

        return desde(candidato) > desde(actual);
    }

    /// <summary>
    /// Elige los conceptos que aplican a la empresa: la definición propia
    /// sustituye a la general con la misma clave y esquemas.
    /// </summary>
    /// <param name="conceptos">Conceptos del catálogo general y de la empresa.</param>
    /// <param name="empresaId">Empresa que se calcula, o <c>null</c> para el catálogo general.</param>
    /// <returns>Los conceptos en orden de presentación.</returns>
    private static IReadOnlyList<ConceptoDeNomina> ResolverConceptos(IReadOnlyList<ConceptoDeNomina> conceptos, Guid? empresaId)
    {
        // Un mismo concepto (por ejemplo TOTAL_PERCEPCIONES) puede tener una
        // definición distinta por esquema; la identidad es clave + esquemas.
        var elegidos = new Dictionary<(string Clave, EsquemasDePago Esquemas), ConceptoDeNomina>();

        foreach (ConceptoDeNomina concepto in conceptos)
        {
            if (concepto.EmpresaId is not null && concepto.EmpresaId != empresaId)
            {
                continue;
            }

            (string Clave, EsquemasDePago Esquemas) identidad = (concepto.Clave, concepto.Esquemas);

            if (!elegidos.TryGetValue(identidad, out ConceptoDeNomina? actual)
                || (concepto.EmpresaId is not null && actual.EmpresaId is null))
            {
                elegidos[identidad] = concepto;
            }
        }

        return [.. elegidos.Values.OrderBy(static c => c.Orden).ThenBy(static c => c.Clave, StringComparer.Ordinal)];
    }
}
