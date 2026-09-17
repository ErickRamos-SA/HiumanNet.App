using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Nomina;

/// <summary>
/// Sección de la explicación de los cálculos que se muestra en el módulo de
/// administración para cada esquema de pago.
/// </summary>
/// <remarks>
/// El texto vive en la base de datos, no en código: el equipo de nómina puede
/// afinarlo a medida que se depuran las fórmulas. Se complementa en tiempo de
/// consulta con las fórmulas y parámetros vigentes del catálogo, de modo que la
/// explicación nunca queda desfasada respecto de lo que el sistema calcula.
/// </remarks>
public sealed class ExplicacionDeCalculo
{
    /// <summary>
    /// Inicializa una instancia con valores ya validados. Sólo la usan las
    /// fábricas y <see cref="Rehidratar"/>.
    /// </summary>
    /// <inheritdoc cref="Rehidratar" path="/param"/>
    private ExplicacionDeCalculo(
        Guid id,
        EsquemaDePago esquema,
        Idioma idioma,
        int orden,
        string titulo,
        string cuerpo,
        DateTimeOffset fechaModificacion)
    {
        Id = id;
        Esquema = esquema;
        Idioma = idioma;
        Orden = orden;
        Titulo = titulo;
        Cuerpo = cuerpo;
        FechaModificacion = fechaModificacion;
    }

    /// <summary>Obtiene el identificador único.</summary>
    /// <value>Clave primaria.</value>
    public Guid Id { get; }

    /// <summary>Obtiene el esquema de pago que explica la sección.</summary>
    /// <value>IMSS, sindicato u honorarios.</value>
    public EsquemaDePago Esquema { get; }

    /// <summary>Obtiene el idioma del texto.</summary>
    /// <value>Español o inglés.</value>
    public Idioma Idioma { get; }

    /// <summary>Obtiene el orden de presentación.</summary>
    /// <value>Entero; menor primero.</value>
    public int Orden { get; private set; }

    /// <summary>Obtiene el título de la sección.</summary>
    /// <value>Texto corto.</value>
    public string Titulo { get; private set; }

    /// <summary>Obtiene el cuerpo de la sección.</summary>
    /// <value>Texto con saltos de línea; admite listas con guion y fórmulas en texto plano.</value>
    public string Cuerpo { get; private set; }

    /// <summary>Obtiene el instante de la última modificación.</summary>
    /// <value>Instante en UTC.</value>
    public DateTimeOffset FechaModificacion { get; private set; }

    /// <summary>
    /// Crea una sección nueva.
    /// </summary>
    /// <param name="esquema">Esquema de pago.</param>
    /// <param name="idioma">Idioma del texto.</param>
    /// <param name="orden">Orden de presentación.</param>
    /// <param name="titulo">Título.</param>
    /// <param name="cuerpo">Cuerpo.</param>
    /// <param name="momento">Instante de creación, en UTC.</param>
    /// <returns>La sección creada.</returns>
    /// <exception cref="CatalogoInvalidoException">Se lanza si el esquema no está especificado o el título está vacío.</exception>
    public static ExplicacionDeCalculo Crear(
        EsquemaDePago esquema, Idioma idioma, int orden, string titulo, string cuerpo, DateTimeOffset momento)
    {
        if (esquema == EsquemaDePago.NoEspecificado)
        {
            throw new CatalogoInvalidoException("Debe indicarse el esquema de pago de la explicación.");
        }

        return new ExplicacionDeCalculo(
            Guid.CreateVersion7(),
            esquema,
            idioma,
            orden,
            string.IsNullOrWhiteSpace(titulo) ? throw new CatalogoInvalidoException("El título es obligatorio.") : titulo.Trim(),
            cuerpo?.Trim() ?? string.Empty,
            momento);
    }

    /// <summary>
    /// Reconstruye una sección a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="esquema">Esquema.</param>
    /// <param name="idioma">Idioma.</param>
    /// <param name="orden">Orden.</param>
    /// <param name="titulo">Título.</param>
    /// <param name="cuerpo">Cuerpo.</param>
    /// <param name="fechaModificacion">Última modificación.</param>
    /// <returns>La entidad rehidratada.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static ExplicacionDeCalculo Rehidratar(
        Guid id, EsquemaDePago esquema, Idioma idioma, int orden, string titulo, string cuerpo, DateTimeOffset fechaModificacion)
        => new(id, esquema, idioma, orden, titulo, cuerpo, fechaModificacion);

    /// <summary>
    /// Actualiza el contenido de la sección.
    /// </summary>
    /// <param name="orden">Orden de presentación.</param>
    /// <param name="titulo">Título.</param>
    /// <param name="cuerpo">Cuerpo.</param>
    /// <param name="momento">Instante de la modificación, en UTC.</param>
    /// <exception cref="CatalogoInvalidoException">Se lanza si el título está vacío.</exception>
    public void Actualizar(int orden, string titulo, string cuerpo, DateTimeOffset momento)
    {
        Orden = orden;
        Titulo = string.IsNullOrWhiteSpace(titulo) ? throw new CatalogoInvalidoException("El título es obligatorio.") : titulo.Trim();
        Cuerpo = cuerpo?.Trim() ?? string.Empty;
        FechaModificacion = momento;
    }
}
