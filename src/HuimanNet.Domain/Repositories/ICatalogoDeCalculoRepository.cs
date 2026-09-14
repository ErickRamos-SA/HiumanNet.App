using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Nomina;

namespace HuimanNet.Domain.Repositories;

/// <summary>
/// Acceso a la persistencia de los catálogos del cálculo: parámetros, tablas
/// por rangos, conceptos y explicaciones.
/// </summary>
/// <remarks>
/// Las lecturas devuelven tanto las entradas globales como las de la empresa
/// indicada; la resolución de prioridad (empresa sobre global) y de vigencia
/// la hace la capa de aplicación al construir el plan de cálculo.
/// </remarks>
public interface ICatalogoDeCalculoRepository
{
    /// <summary>
    /// Lista los parámetros globales y los de una empresa.
    /// </summary>
    /// <param name="empresaId">Empresa cuyas sobrescrituras se incluyen, o <c>null</c> para sólo globales.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los parámetros ordenados por grupo, clave y vigencia.</returns>
    Task<IReadOnlyList<ParametroDeCalculo>> ListarParametrosAsync(
        Guid? empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un parámetro por su identificador.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El parámetro, o <c>null</c>.</returns>
    Task<ParametroDeCalculo?> ObtenerParametroAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserta un parámetro.
    /// </summary>
    /// <param name="parametro">Entidad a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task AgregarParametroAsync(ParametroDeCalculo parametro, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un parámetro.
    /// </summary>
    /// <param name="parametro">Entidad con los valores a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task ActualizarParametroAsync(ParametroDeCalculo parametro, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un parámetro.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task EliminarParametroAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista las tablas globales y las de una empresa, con sus rangos.
    /// </summary>
    /// <param name="empresaId">Empresa cuyas sobrescrituras se incluyen, o <c>null</c>.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las tablas ordenadas por clave y vigencia.</returns>
    Task<IReadOnlyList<TablaDeRangos>> ListarTablasAsync(
        Guid? empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una tabla con sus rangos.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La tabla, o <c>null</c>.</returns>
    Task<TablaDeRangos?> ObtenerTablaAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserta una tabla con sus rangos.
    /// </summary>
    /// <param name="tabla">Entidad a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task AgregarTablaAsync(TablaDeRangos tabla, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza una tabla y sustituye sus rangos.
    /// </summary>
    /// <param name="tabla">Entidad con los valores a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task ActualizarTablaAsync(TablaDeRangos tabla, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina una tabla y sus rangos.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task EliminarTablaAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista los conceptos globales y los de una empresa.
    /// </summary>
    /// <param name="empresaId">Empresa cuyas sobrescrituras se incluyen, o <c>null</c>.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los conceptos ordenados por orden y clave.</returns>
    Task<IReadOnlyList<ConceptoDeNomina>> ListarConceptosAsync(
        Guid? empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un concepto por su identificador.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El concepto, o <c>null</c>.</returns>
    Task<ConceptoDeNomina?> ObtenerConceptoAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserta un concepto.
    /// </summary>
    /// <param name="concepto">Entidad a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task AgregarConceptoAsync(ConceptoDeNomina concepto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un concepto.
    /// </summary>
    /// <param name="concepto">Entidad con los valores a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task ActualizarConceptoAsync(ConceptoDeNomina concepto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un concepto.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task EliminarConceptoAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista las secciones de explicación.
    /// </summary>
    /// <param name="esquema">Esquema a filtrar, o <c>null</c> para todos.</param>
    /// <param name="idioma">Idioma a filtrar, o <c>null</c> para todos.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Las secciones ordenadas por esquema, idioma y orden.</returns>
    Task<IReadOnlyList<ExplicacionDeCalculo>> ListarExplicacionesAsync(
        EsquemaDePago? esquema, Idioma? idioma, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una sección por su identificador.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La sección, o <c>null</c>.</returns>
    Task<ExplicacionDeCalculo?> ObtenerExplicacionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserta una sección.
    /// </summary>
    /// <param name="explicacion">Entidad a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task AgregarExplicacionAsync(ExplicacionDeCalculo explicacion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza una sección.
    /// </summary>
    /// <param name="explicacion">Entidad con los valores a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task ActualizarExplicacionAsync(ExplicacionDeCalculo explicacion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina una sección.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task EliminarExplicacionAsync(Guid id, CancellationToken cancellationToken = default);
}
