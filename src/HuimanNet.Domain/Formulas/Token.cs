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

/// <summary>
/// Fragmento léxico de una fórmula, con su posición original para poder
/// señalar errores con precisión.
/// </summary>
/// <param name="Tipo">Clase léxica del fragmento.</param>
/// <param name="Texto">Texto tal y como aparece en la fórmula (identificadores en mayúsculas).</param>
/// <param name="Posicion">Índice base cero del primer carácter dentro de la fórmula.</param>
public readonly record struct Token(TipoDeToken Tipo, string Texto, int Posicion);
