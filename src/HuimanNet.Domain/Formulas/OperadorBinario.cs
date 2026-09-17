namespace HuimanNet.Domain.Formulas;

/// <summary>
/// Operadores binarios admitidos por las fórmulas.
/// </summary>
public enum OperadorBinario
{
    /// <summary>Suma.</summary>
    Suma,

    /// <summary>Resta.</summary>
    Resta,

    /// <summary>Multiplicación.</summary>
    Multiplicacion,

    /// <summary>División. Dividir entre cero devuelve cero, como <c>SI.ERROR(x/0;0)</c> en el modelo de referencia.</summary>
    Division,

    /// <summary>Potencia.</summary>
    Potencia,

    /// <summary>Igualdad.</summary>
    Igual,

    /// <summary>Desigualdad.</summary>
    Distinto,

    /// <summary>Menor que.</summary>
    Menor,

    /// <summary>Menor o igual que.</summary>
    MenorOIgual,

    /// <summary>Mayor que.</summary>
    Mayor,

    /// <summary>Mayor o igual que.</summary>
    MayorOIgual,

    /// <summary>Conjunción lógica (<c>Y</c>).</summary>
    Y,

    /// <summary>Disyunción lógica (<c>O</c>).</summary>
    O,
}
