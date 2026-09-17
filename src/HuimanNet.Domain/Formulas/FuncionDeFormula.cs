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
