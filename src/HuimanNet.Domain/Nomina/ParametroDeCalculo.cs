using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Nomina;

/// <summary>
/// Valor con vigencia del catálogo de parámetros del cálculo: UMA, salarios
/// mínimos, porcentajes de cuotas, factores, tasas de ISN, etc.
/// </summary>
/// <remarks>
/// Ningún valor del cálculo vive en código: todos se leen de este catálogo.
/// Un parámetro puede tener una versión global y otra por empresa; la de la
/// empresa prevalece. La vigencia permite capturar con antelación los valores
/// del siguiente ejercicio sin afectar al actual.
/// </remarks>
public sealed class ParametroDeCalculo
{
    /// <summary>Longitud máxima de la clave.</summary>
    public const int LongitudMaximaClave = 64;

    private ParametroDeCalculo(
        Guid id,
        string clave,
        string descripcion,
        string grupo,
        decimal valor,
        string unidad,
        Guid? empresaId,
        DateOnly vigenteDesde,
        DateOnly? vigenteHasta,
        DateTimeOffset fechaModificacion)
    {
        Id = id;
        Clave = clave;
        Descripcion = descripcion;
        Grupo = grupo;
        Valor = valor;
        Unidad = unidad;
        EmpresaId = empresaId;
        VigenteDesde = vigenteDesde;
        VigenteHasta = vigenteHasta;
        FechaModificacion = fechaModificacion;
    }

    /// <summary>Obtiene el identificador único.</summary>
    /// <value>Clave primaria.</value>
    public Guid Id { get; }

    /// <summary>Obtiene la clave con la que las fórmulas referencian el parámetro.</summary>
    /// <value>Mayúsculas, dígitos y guion bajo; por ejemplo <c>UMA_DIARIA</c>.</value>
    public string Clave { get; }

    /// <summary>Obtiene la descripción legible del parámetro.</summary>
    /// <value>Texto para la interfaz de administración.</value>
    public string Descripcion { get; private set; }

    /// <summary>Obtiene el grupo en el que se presenta el parámetro.</summary>
    /// <value>Por ejemplo <c>Generales</c>, <c>ISR</c>, <c>IMSS</c>, <c>INFONAVIT</c>.</value>
    public string Grupo { get; private set; }

    /// <summary>Obtiene el valor del parámetro.</summary>
    /// <value>Los porcentajes se expresan como fracción (16 % = 0.16).</value>
    public decimal Valor { get; private set; }

    /// <summary>Obtiene la unidad de presentación.</summary>
    /// <value>Por ejemplo <c>MXN</c>, <c>%</c>, <c>días</c> o <c>factor</c>.</value>
    public string Unidad { get; private set; }

    /// <summary>Obtiene la empresa a la que aplica el valor.</summary>
    /// <value><c>null</c> para el valor global; una empresa concreta cuando la sobrescribe.</value>
    public Guid? EmpresaId { get; }

    /// <summary>Obtiene el primer día de vigencia.</summary>
    /// <value>Fecha inclusiva.</value>
    public DateOnly VigenteDesde { get; private set; }

    /// <summary>Obtiene el último día de vigencia.</summary>
    /// <value>Fecha inclusiva, o <c>null</c> si sigue vigente indefinidamente.</value>
    public DateOnly? VigenteHasta { get; private set; }

    /// <summary>Obtiene el instante de la última modificación.</summary>
    /// <value>Instante en UTC.</value>
    public DateTimeOffset FechaModificacion { get; private set; }

    /// <summary>
    /// Crea un parámetro nuevo.
    /// </summary>
    /// <param name="clave">Clave del parámetro.</param>
    /// <param name="descripcion">Descripción legible.</param>
    /// <param name="grupo">Grupo de presentación.</param>
    /// <param name="valor">Valor.</param>
    /// <param name="unidad">Unidad de presentación.</param>
    /// <param name="empresaId">Empresa a la que aplica, o <c>null</c> para el valor global.</param>
    /// <param name="vigenteDesde">Primer día de vigencia.</param>
    /// <param name="vigenteHasta">Último día de vigencia, o <c>null</c>.</param>
    /// <param name="momento">Instante de creación, en UTC.</param>
    /// <returns>El parámetro creado.</returns>
    /// <exception cref="CatalogoInvalidoException">Se lanza si la clave o las vigencias no son válidas.</exception>
    public static ParametroDeCalculo Crear(
        string clave,
        string descripcion,
        string grupo,
        decimal valor,
        string unidad,
        Guid? empresaId,
        DateOnly vigenteDesde,
        DateOnly? vigenteHasta,
        DateTimeOffset momento)
    {
        string claveNormalizada = NormalizarClave(clave);
        ValidarVigencia(vigenteDesde, vigenteHasta);

        return new ParametroDeCalculo(
            Guid.CreateVersion7(),
            claveNormalizada,
            Requerido(descripcion, "descripción"),
            string.IsNullOrWhiteSpace(grupo) ? "Generales" : grupo.Trim(),
            valor,
            string.IsNullOrWhiteSpace(unidad) ? string.Empty : unidad.Trim(),
            empresaId,
            vigenteDesde,
            vigenteHasta,
            momento);
    }

    /// <summary>
    /// Reconstruye un parámetro a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="clave">Clave.</param>
    /// <param name="descripcion">Descripción.</param>
    /// <param name="grupo">Grupo.</param>
    /// <param name="valor">Valor.</param>
    /// <param name="unidad">Unidad.</param>
    /// <param name="empresaId">Empresa, si aplica.</param>
    /// <param name="vigenteDesde">Inicio de vigencia.</param>
    /// <param name="vigenteHasta">Fin de vigencia.</param>
    /// <param name="fechaModificacion">Última modificación.</param>
    /// <returns>La entidad rehidratada.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static ParametroDeCalculo Rehidratar(
        Guid id,
        string clave,
        string descripcion,
        string grupo,
        decimal valor,
        string unidad,
        Guid? empresaId,
        DateOnly vigenteDesde,
        DateOnly? vigenteHasta,
        DateTimeOffset fechaModificacion)
        => new(id, clave, descripcion, grupo, valor, unidad, empresaId, vigenteDesde, vigenteHasta, fechaModificacion);

    /// <summary>
    /// Actualiza el valor y los metadatos del parámetro.
    /// </summary>
    /// <param name="descripcion">Descripción legible.</param>
    /// <param name="grupo">Grupo de presentación.</param>
    /// <param name="valor">Nuevo valor.</param>
    /// <param name="unidad">Unidad de presentación.</param>
    /// <param name="vigenteDesde">Primer día de vigencia.</param>
    /// <param name="vigenteHasta">Último día de vigencia, o <c>null</c>.</param>
    /// <param name="momento">Instante de la modificación, en UTC.</param>
    /// <exception cref="CatalogoInvalidoException">Se lanza si las vigencias no son válidas.</exception>
    public void Actualizar(
        string descripcion,
        string grupo,
        decimal valor,
        string unidad,
        DateOnly vigenteDesde,
        DateOnly? vigenteHasta,
        DateTimeOffset momento)
    {
        ValidarVigencia(vigenteDesde, vigenteHasta);

        Descripcion = Requerido(descripcion, "descripción");
        Grupo = string.IsNullOrWhiteSpace(grupo) ? "Generales" : grupo.Trim();
        Valor = valor;
        Unidad = string.IsNullOrWhiteSpace(unidad) ? string.Empty : unidad.Trim();
        VigenteDesde = vigenteDesde;
        VigenteHasta = vigenteHasta;
        FechaModificacion = momento;
    }

    /// <summary>
    /// Indica si el parámetro está vigente en una fecha.
    /// </summary>
    /// <param name="fecha">Fecha de referencia del período.</param>
    /// <returns><c>true</c> si la fecha cae dentro de la vigencia.</returns>
    public bool EstaVigenteEn(DateOnly fecha)
        => fecha >= VigenteDesde && (VigenteHasta is null || fecha <= VigenteHasta.Value);

    /// <summary>
    /// Normaliza y valida una clave de catálogo.
    /// </summary>
    /// <param name="clave">Clave aportada por el usuario.</param>
    /// <returns>La clave en mayúsculas.</returns>
    /// <exception cref="CatalogoInvalidoException">Se lanza si la clave está vacía o contiene caracteres no válidos.</exception>
    public static string NormalizarClave(string? clave)
    {
        if (string.IsNullOrWhiteSpace(clave))
        {
            throw new CatalogoInvalidoException("La clave es obligatoria.");
        }

        string normalizada = clave.Trim().ToUpperInvariant();

        if (normalizada.Length > LongitudMaximaClave)
        {
            throw new CatalogoInvalidoException($"La clave no puede exceder {LongitudMaximaClave} caracteres.");
        }

        if (!char.IsAsciiLetter(normalizada[0]))
        {
            throw new CatalogoInvalidoException("La clave debe empezar por una letra.");
        }

        foreach (char c in normalizada)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '_')
            {
                throw new CatalogoInvalidoException(
                    "La clave sólo admite letras sin acento, dígitos y guion bajo.");
            }
        }

        return normalizada;
    }

    private static void ValidarVigencia(DateOnly desde, DateOnly? hasta)
    {
        if (hasta is not null && hasta.Value < desde)
        {
            throw new CatalogoInvalidoException("La fecha de fin de vigencia no puede ser anterior al inicio.");
        }
    }

    private static string Requerido(string? valor, string nombre)
        => string.IsNullOrWhiteSpace(valor)
            ? throw new CatalogoInvalidoException($"La {nombre} es obligatoria.")
            : valor.Trim();
}
