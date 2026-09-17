using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using HuimanNet.Domain.Nomina;

namespace HuimanNet.Infrastructure.Persistence.Semillas;

/// <summary>
/// Catálogo de cálculo con el que arranca una instalación nueva.
/// </summary>
/// <param name="Version">Versión del formato.</param>
/// <param name="VigenteDesde">Inicio de vigencia de parámetros y tablas (<c>aaaa-MM-dd</c>).</param>
/// <param name="Fuente">Documentos de los que se tomaron los valores.</param>
/// <param name="Parametros">Parámetros.</param>
/// <param name="Tablas">Tablas por rangos.</param>
/// <param name="Conceptos">Conceptos y fórmulas.</param>
/// <param name="Explicaciones">Explicación narrativa por esquema e idioma.</param>
/// <param name="AliasDeCotejo">
/// Alias de cotejo por clave de concepto; se aplican a todas las definiciones
/// de esa clave, sea cual sea su esquema.
/// </param>
/// <remarks>
/// Vive en <c>Persistence/Semillas/catalogo-inicial.json</c>, incrustado en el
/// ensamblado. Es la <b>única</b> fuente de los valores iniciales: el sembrador
/// lo carga en la base de datos y las pruebas lo usan para verificar que el
/// motor reproduce el archivo de cálculo de referencia. A partir de ahí el
/// administrador lo mantiene desde el portal.
/// </remarks>
public sealed record CatalogoInicial(
    int Version,
    string VigenteDesde,
    string? Fuente,
    IReadOnlyList<ParametroInicial> Parametros,
    IReadOnlyList<TablaInicial> Tablas,
    IReadOnlyList<ConceptoInicial> Conceptos,
    IReadOnlyList<ExplicacionInicial> Explicaciones,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? AliasDeCotejo = null)
{
    /// <summary>Nombre del recurso JSON incrustado con el catálogo inicial.</summary>
    private const string NombreDelRecurso ="HuimanNet.Infrastructure.Persistence.Semillas.catalogo-inicial.json";

    /// <summary>
    /// Lee el catálogo incrustado en el ensamblado.
    /// </summary>
    /// <returns>El catálogo inicial.</returns>
    /// <exception cref="InvalidOperationException">Se lanza si el recurso falta o no es válido.</exception>
    public static CatalogoInicial Cargar()
    {
        Assembly ensamblado = typeof(CatalogoInicial).Assembly;

        using Stream flujo = ensamblado.GetManifestResourceStream(NombreDelRecurso)
            ?? throw new InvalidOperationException($"No se encontró el recurso incrustado '{NombreDelRecurso}'.");

        return JsonSerializer.Deserialize(flujo, CatalogoInicialJsonContext.Default.CatalogoInicial)
            ?? throw new InvalidOperationException("El catálogo inicial está vacío.");
    }

    /// <summary>Obtiene la fecha de inicio de vigencia.</summary>
    /// <value>Fecha interpretada de <see cref="VigenteDesde"/>.</value>
    [JsonIgnore]
    public DateOnly FechaDeVigencia => DateOnly.ParseExact(VigenteDesde, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Construye las entidades de parámetros globales.
    /// </summary>
    /// <param name="momento">Instante de creación.</param>
    /// <returns>Los parámetros.</returns>
    public IReadOnlyList<ParametroDeCalculo> ConstruirParametros(DateTimeOffset momento)
        => Parametros.Select(p => ParametroDeCalculo.Crear(
            p.Clave, p.Descripcion, p.Grupo, p.Valor, p.Unidad, null, FechaDeVigencia, null, momento)).ToList();

    /// <summary>
    /// Construye las entidades de tablas globales.
    /// </summary>
    /// <param name="momento">Instante de creación.</param>
    /// <returns>Las tablas.</returns>
    public IReadOnlyList<TablaDeRangos> ConstruirTablas(DateTimeOffset momento)
        => Tablas.Select(t => TablaDeRangos.Crear(
            t.Clave, t.Descripcion, null, FechaDeVigencia, null,
            t.Rangos.Select(static r => new RangoDeTabla(r.LimiteInferior, r.LimiteSuperior, r.CuotaFija, r.Porcentaje, r.Valor)),
            momento)).ToList();

    /// <summary>
    /// Construye las entidades de conceptos globales, validando sus fórmulas.
    /// </summary>
    /// <param name="momento">Instante de creación.</param>
    /// <returns>Los conceptos.</returns>
    public IReadOnlyList<ConceptoDeNomina> ConstruirConceptos(DateTimeOffset momento)
        => Conceptos.Select(c => ConceptoDeNomina.Crear(
            c.Clave, c.Nombre, c.Descripcion, c.Tipo, c.Esquemas, c.Orden, c.Formula, c.VisibleEnRecibo, null, momento,
            AliasDeCotejo?.GetValueOrDefault(c.Clave))).ToList();

    /// <summary>
    /// Construye las secciones de explicación.
    /// </summary>
    /// <param name="momento">Instante de creación.</param>
    /// <returns>Las secciones.</returns>
    public IReadOnlyList<ExplicacionDeCalculo> ConstruirExplicaciones(DateTimeOffset momento)
        => Explicaciones.Select(e => ExplicacionDeCalculo.Crear(e.Esquema, e.Idioma, e.Orden, e.Titulo, e.Cuerpo, momento)).ToList();
}
