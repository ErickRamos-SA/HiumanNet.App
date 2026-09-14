namespace HuimanNet.Domain.Formulas;

/// <summary>
/// Funciones incorporadas del lenguaje de fórmulas.
/// </summary>
public enum FuncionDeFormula
{
    /// <summary><c>SI(condición; si_verdadero; si_falso)</c>.</summary>
    Si,

    /// <summary><c>MAX(a; b; ...)</c>.</summary>
    Maximo,

    /// <summary><c>MIN(a; b; ...)</c>.</summary>
    Minimo,

    /// <summary><c>REDONDEAR(valor; decimales)</c>, redondeo comercial (mitad hacia arriba).</summary>
    Redondear,

    /// <summary><c>TRUNCAR(valor; decimales)</c>.</summary>
    Truncar,

    /// <summary><c>ABS(valor)</c>.</summary>
    Absoluto,

    /// <summary><c>ENTERO(valor)</c>: mayor entero menor o igual.</summary>
    Entero,

    /// <summary><c>TECHO(valor)</c>: menor entero mayor o igual.</summary>
    Techo,

    /// <summary><c>Y(a; b; ...)</c>.</summary>
    Y,

    /// <summary><c>O(a; b; ...)</c>.</summary>
    O,

    /// <summary><c>NO(a)</c>.</summary>
    No,

    /// <summary><c>ENTRE(valor; mínimo; máximo)</c>: 1 si el valor está en el intervalo cerrado.</summary>
    Entre,

    /// <summary><c>TABLA("CLAVE"; valor; "CAMPO")</c>: consulta una tabla por rangos.</summary>
    Tabla,
}

/// <summary>
/// Descripción de una función para la ayuda del administrador.
/// </summary>
/// <param name="Funcion">Función descrita.</param>
/// <param name="Nombres">Nombres aceptados (español e inglés).</param>
/// <param name="MinimoArgumentos">Número mínimo de argumentos.</param>
/// <param name="MaximoArgumentos">Número máximo de argumentos, o <c>null</c> si no hay límite.</param>
/// <param name="Firma">Firma de ejemplo.</param>
/// <param name="Descripcion">Qué hace la función.</param>
public sealed record DescripcionDeFuncion(
    FuncionDeFormula Funcion,
    IReadOnlyList<string> Nombres,
    int MinimoArgumentos,
    int? MaximoArgumentos,
    string Firma,
    string Descripcion);

/// <summary>
/// Catálogo de funciones del lenguaje de fórmulas, con sus alias y su aridad.
/// </summary>
/// <remarks>
/// Es la única lista que hay que ampliar para añadir una función nueva: el
/// analizador sintáctico y la ayuda de la interfaz la consultan.
/// </remarks>
public static class FuncionesDeFormula
{
    private static readonly DescripcionDeFuncion[] Catalogo =
    [
        new(FuncionDeFormula.Si, ["SI", "IF"], 3, 3, "SI(condición; si_verdadero; si_falso)", "Devuelve el segundo argumento si la condición es distinta de cero; en caso contrario el tercero."),
        new(FuncionDeFormula.Maximo, ["MAX", "MAXIMO"], 1, null, "MAX(a; b; ...)", "Mayor de los argumentos."),
        new(FuncionDeFormula.Minimo, ["MIN", "MINIMO"], 1, null, "MIN(a; b; ...)", "Menor de los argumentos."),
        new(FuncionDeFormula.Redondear, ["REDONDEAR", "ROUND"], 1, 2, "REDONDEAR(valor; decimales)", "Redondeo comercial (mitad hacia arriba) al número de decimales indicado; cero si se omite."),
        new(FuncionDeFormula.Truncar, ["TRUNCAR", "TRUNC"], 1, 2, "TRUNCAR(valor; decimales)", "Elimina los decimales sobrantes sin redondear."),
        new(FuncionDeFormula.Absoluto, ["ABS"], 1, 1, "ABS(valor)", "Valor absoluto."),
        new(FuncionDeFormula.Entero, ["ENTERO", "FLOOR"], 1, 1, "ENTERO(valor)", "Mayor entero menor o igual que el valor."),
        new(FuncionDeFormula.Techo, ["TECHO", "CEIL", "CEILING"], 1, 1, "TECHO(valor)", "Menor entero mayor o igual que el valor."),
        new(FuncionDeFormula.Y, ["Y", "AND"], 1, null, "Y(a; b; ...)", "1 si todos los argumentos son distintos de cero."),
        new(FuncionDeFormula.O, ["O", "OR"], 1, null, "O(a; b; ...)", "1 si alguno de los argumentos es distinto de cero."),
        new(FuncionDeFormula.No, ["NO", "NOT"], 1, 1, "NO(a)", "1 si el argumento es cero; 0 en caso contrario."),
        new(FuncionDeFormula.Entre, ["ENTRE", "BETWEEN"], 3, 3, "ENTRE(valor; mínimo; máximo)", "1 si el valor está dentro del intervalo cerrado."),
        new(FuncionDeFormula.Tabla, ["TABLA", "TABLE"], 3, 3, "TABLA(\"ISR\"; base; \"CUOTA_FIJA\")", "Busca el rango de la tabla cuyo límite inferior es el mayor que no supera el valor y devuelve el campo pedido: LIMITE_INFERIOR, LIMITE_SUPERIOR, CUOTA_FIJA, PORCENTAJE o VALOR."),
    ];

    private static readonly Dictionary<string, DescripcionDeFuncion> PorNombre = Construir();

    /// <summary>
    /// Obtiene la descripción de todas las funciones, en orden de presentación.
    /// </summary>
    /// <value>Lista de sólo lectura con el catálogo completo.</value>
    public static IReadOnlyList<DescripcionDeFuncion> Todas => Catalogo;

    /// <summary>
    /// Busca una función por cualquiera de sus nombres.
    /// </summary>
    /// <param name="nombre">Nombre en mayúsculas.</param>
    /// <param name="descripcion">Descripción encontrada.</param>
    /// <returns><c>true</c> si el nombre corresponde a una función.</returns>
    public static bool TryObtener(string nombre, out DescripcionDeFuncion descripcion)
        => PorNombre.TryGetValue(nombre, out descripcion!);

    /// <summary>
    /// Indica si un identificador es el nombre de una función.
    /// </summary>
    /// <param name="nombre">Nombre en mayúsculas.</param>
    /// <returns><c>true</c> si es una función incorporada.</returns>
    public static bool EsFuncion(string nombre) => PorNombre.ContainsKey(nombre);

    private static Dictionary<string, DescripcionDeFuncion> Construir()
    {
        var indice = new Dictionary<string, DescripcionDeFuncion>(StringComparer.Ordinal);

        foreach (DescripcionDeFuncion funcion in Catalogo)
        {
            foreach (string nombre in funcion.Nombres)
            {
                indice[nombre] = funcion;
            }
        }

        return indice;
    }
}
