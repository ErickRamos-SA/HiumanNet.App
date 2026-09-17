namespace HuimanNet.Domain.Formulas;

/// <summary>
/// Clase léxica de un fragmento de fórmula.
/// </summary>
public enum TipoDeToken
{
    /// <summary>Literal numérico, con punto decimal opcional.</summary>
    Numero,

    /// <summary>Nombre de variable, parámetro, concepto o función.</summary>
    Identificador,

    /// <summary>Literal de texto entre comillas dobles (sólo válido como argumento de <c>TABLA</c>).</summary>
    Cadena,

    /// <summary>Operador aritmético, de comparación o lógico.</summary>
    Operador,

    /// <summary>Paréntesis de apertura.</summary>
    ParentesisAbre,

    /// <summary>Paréntesis de cierre.</summary>
    ParentesisCierra,

    /// <summary>Separador de argumentos.</summary>
    Coma,

    /// <summary>Fin de la fórmula.</summary>
    Fin,
}
