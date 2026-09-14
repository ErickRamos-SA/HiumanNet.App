using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Nomina;

/// <summary>
/// Renglón de una tabla por rangos: tarifa de ISR, subsidio al empleo o
/// cuota variable de cesantía y vejez.
/// </summary>
/// <param name="LimiteInferior">Límite inferior del rango, inclusive.</param>
/// <param name="LimiteSuperior">Límite superior del rango, inclusive, o <c>null</c> para "en adelante".</param>
/// <param name="CuotaFija">Cuota fija del rango (tarifas de ISR).</param>
/// <param name="Porcentaje">Porcentaje aplicable sobre el excedente, como fracción (16 % = 0.16).</param>
/// <param name="Valor">Valor directo del rango (subsidio al empleo).</param>
public sealed record RangoDeTabla(
    decimal LimiteInferior,
    decimal? LimiteSuperior,
    decimal CuotaFija,
    decimal Porcentaje,
    decimal Valor);

/// <summary>
/// Tabla por rangos con vigencia: tarifa de ISR, tabla de subsidio al empleo,
/// tabla de cesantía y vejez patronal, etc.
/// </summary>
/// <remarks>
/// La búsqueda tiene la semántica de <c>BUSCARV</c> aproximado del modelo de
/// referencia: se toma el rango cuyo límite inferior es el mayor que no supera
/// el valor buscado. Un valor menor que el primer límite no encuentra rango y
/// las fórmulas reciben cero, igual que <c>SI.ERROR(BUSCARV(...);0)</c>.
/// </remarks>
public sealed class TablaDeRangos
{
    private RangoDeTabla[] _rangos;

    private TablaDeRangos(
        Guid id,
        string clave,
        string descripcion,
        Guid? empresaId,
        DateOnly vigenteDesde,
        DateOnly? vigenteHasta,
        DateTimeOffset fechaModificacion,
        RangoDeTabla[] rangos)
    {
        Id = id;
        Clave = clave;
        Descripcion = descripcion;
        EmpresaId = empresaId;
        VigenteDesde = vigenteDesde;
        VigenteHasta = vigenteHasta;
        FechaModificacion = fechaModificacion;
        _rangos = rangos;
    }

    /// <summary>Obtiene el identificador único.</summary>
    /// <value>Clave primaria.</value>
    public Guid Id { get; }

    /// <summary>Obtiene la clave con la que las fórmulas consultan la tabla.</summary>
    /// <value>Por ejemplo <c>ISR</c>, <c>SUBSIDIO</c> o <c>CVR</c>.</value>
    public string Clave { get; }

    /// <summary>Obtiene la descripción legible de la tabla.</summary>
    /// <value>Texto para la interfaz de administración.</value>
    public string Descripcion { get; private set; }

    /// <summary>Obtiene la empresa a la que aplica la tabla.</summary>
    /// <value><c>null</c> para la tabla global.</value>
    public Guid? EmpresaId { get; }

    /// <summary>Obtiene el primer día de vigencia.</summary>
    /// <value>Fecha inclusiva.</value>
    public DateOnly VigenteDesde { get; private set; }

    /// <summary>Obtiene el último día de vigencia.</summary>
    /// <value>Fecha inclusiva, o <c>null</c> si sigue vigente.</value>
    public DateOnly? VigenteHasta { get; private set; }

    /// <summary>Obtiene el instante de la última modificación.</summary>
    /// <value>Instante en UTC.</value>
    public DateTimeOffset FechaModificacion { get; private set; }

    /// <summary>Obtiene los rangos ordenados por límite inferior.</summary>
    /// <value>Lista de sólo lectura.</value>
    public IReadOnlyList<RangoDeTabla> Rangos => _rangos;

    /// <summary>
    /// Crea una tabla nueva.
    /// </summary>
    /// <param name="clave">Clave de la tabla.</param>
    /// <param name="descripcion">Descripción legible.</param>
    /// <param name="empresaId">Empresa a la que aplica, o <c>null</c>.</param>
    /// <param name="vigenteDesde">Primer día de vigencia.</param>
    /// <param name="vigenteHasta">Último día de vigencia, o <c>null</c>.</param>
    /// <param name="rangos">Renglones de la tabla.</param>
    /// <param name="momento">Instante de creación, en UTC.</param>
    /// <returns>La tabla creada.</returns>
    /// <exception cref="CatalogoInvalidoException">Se lanza si la clave, la vigencia o los rangos no son válidos.</exception>
    public static TablaDeRangos Crear(
        string clave,
        string descripcion,
        Guid? empresaId,
        DateOnly vigenteDesde,
        DateOnly? vigenteHasta,
        IEnumerable<RangoDeTabla> rangos,
        DateTimeOffset momento)
    {
        if (vigenteHasta is not null && vigenteHasta.Value < vigenteDesde)
        {
            throw new CatalogoInvalidoException("La fecha de fin de vigencia no puede ser anterior al inicio.");
        }

        return new TablaDeRangos(
            Guid.CreateVersion7(),
            ParametroDeCalculo.NormalizarClave(clave),
            string.IsNullOrWhiteSpace(descripcion) ? throw new CatalogoInvalidoException("La descripción es obligatoria.") : descripcion.Trim(),
            empresaId,
            vigenteDesde,
            vigenteHasta,
            momento,
            Ordenar(rangos));
    }

    /// <summary>
    /// Reconstruye una tabla a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="clave">Clave.</param>
    /// <param name="descripcion">Descripción.</param>
    /// <param name="empresaId">Empresa, si aplica.</param>
    /// <param name="vigenteDesde">Inicio de vigencia.</param>
    /// <param name="vigenteHasta">Fin de vigencia.</param>
    /// <param name="fechaModificacion">Última modificación.</param>
    /// <param name="rangos">Renglones, en cualquier orden.</param>
    /// <returns>La entidad rehidratada.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static TablaDeRangos Rehidratar(
        Guid id,
        string clave,
        string descripcion,
        Guid? empresaId,
        DateOnly vigenteDesde,
        DateOnly? vigenteHasta,
        DateTimeOffset fechaModificacion,
        IEnumerable<RangoDeTabla> rangos)
        => new(id, clave, descripcion, empresaId, vigenteDesde, vigenteHasta, fechaModificacion, Ordenar(rangos));

    /// <summary>
    /// Actualiza los metadatos y sustituye los renglones de la tabla.
    /// </summary>
    /// <param name="descripcion">Descripción legible.</param>
    /// <param name="vigenteDesde">Primer día de vigencia.</param>
    /// <param name="vigenteHasta">Último día de vigencia, o <c>null</c>.</param>
    /// <param name="rangos">Nuevos renglones.</param>
    /// <param name="momento">Instante de la modificación, en UTC.</param>
    /// <exception cref="CatalogoInvalidoException">Se lanza si la vigencia o los rangos no son válidos.</exception>
    public void Actualizar(
        string descripcion,
        DateOnly vigenteDesde,
        DateOnly? vigenteHasta,
        IEnumerable<RangoDeTabla> rangos,
        DateTimeOffset momento)
    {
        if (vigenteHasta is not null && vigenteHasta.Value < vigenteDesde)
        {
            throw new CatalogoInvalidoException("La fecha de fin de vigencia no puede ser anterior al inicio.");
        }

        Descripcion = string.IsNullOrWhiteSpace(descripcion)
            ? throw new CatalogoInvalidoException("La descripción es obligatoria.")
            : descripcion.Trim();
        VigenteDesde = vigenteDesde;
        VigenteHasta = vigenteHasta;
        _rangos = Ordenar(rangos);
        FechaModificacion = momento;
    }

    /// <summary>
    /// Indica si la tabla está vigente en una fecha.
    /// </summary>
    /// <param name="fecha">Fecha de referencia del período.</param>
    /// <returns><c>true</c> si la fecha cae dentro de la vigencia.</returns>
    public bool EstaVigenteEn(DateOnly fecha)
        => fecha >= VigenteDesde && (VigenteHasta is null || fecha <= VigenteHasta.Value);

    /// <summary>
    /// Ubica el rango que corresponde a un valor.
    /// </summary>
    /// <param name="valor">Valor buscado (base gravable, SDI, etc.).</param>
    /// <returns>
    /// El rango cuyo límite inferior es el mayor que no supera el valor, o
    /// <c>null</c> si el valor es menor que el primer límite inferior.
    /// </returns>
    /// <remarks>Búsqueda binaria sobre los límites inferiores: coste logarítmico.</remarks>
    public RangoDeTabla? Buscar(decimal valor)
    {
        int bajo = 0;
        int alto = _rangos.Length - 1;
        int encontrado = -1;

        while (bajo <= alto)
        {
            int medio = bajo + ((alto - bajo) >> 1);

            if (_rangos[medio].LimiteInferior <= valor)
            {
                encontrado = medio;
                bajo = medio + 1;
            }
            else
            {
                alto = medio - 1;
            }
        }

        return encontrado < 0 ? null : _rangos[encontrado];
    }

    /// <summary>
    /// Devuelve un campo de un rango por su nombre.
    /// </summary>
    /// <param name="rango">Rango consultado.</param>
    /// <param name="campo">
    /// <c>LIMITE_INFERIOR</c>, <c>LIMITE_SUPERIOR</c>, <c>CUOTA_FIJA</c>,
    /// <c>PORCENTAJE</c> o <c>VALOR</c>.
    /// </param>
    /// <returns>El valor del campo.</returns>
    /// <exception cref="ErrorDeFormulaException">Se lanza si el campo no existe.</exception>
    public static decimal Campo(RangoDeTabla rango, string campo)
    {
        ArgumentNullException.ThrowIfNull(rango);

        return campo switch
        {
            "LIMITE_INFERIOR" => rango.LimiteInferior,
            "LIMITE_SUPERIOR" => rango.LimiteSuperior ?? decimal.MaxValue,
            "CUOTA_FIJA" => rango.CuotaFija,
            "PORCENTAJE" => rango.Porcentaje,
            "VALOR" => rango.Valor,
            _ => throw new ErrorDeFormulaException(
                $"El campo de tabla '{campo}' no existe; use LIMITE_INFERIOR, LIMITE_SUPERIOR, CUOTA_FIJA, PORCENTAJE o VALOR"),
        };
    }

    private static RangoDeTabla[] Ordenar(IEnumerable<RangoDeTabla> rangos)
    {
        ArgumentNullException.ThrowIfNull(rangos);

        RangoDeTabla[] ordenados = [.. rangos.OrderBy(static r => r.LimiteInferior)];

        if (ordenados.Length == 0)
        {
            throw new CatalogoInvalidoException("La tabla debe tener al menos un rango.");
        }

        for (int i = 1; i < ordenados.Length; i++)
        {
            if (ordenados[i].LimiteInferior == ordenados[i - 1].LimiteInferior)
            {
                throw new CatalogoInvalidoException(
                    $"Hay dos rangos con el mismo límite inferior ({ordenados[i].LimiteInferior}).");
            }
        }

        return ordenados;
    }
}
