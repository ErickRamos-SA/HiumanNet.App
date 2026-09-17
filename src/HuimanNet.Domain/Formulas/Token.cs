namespace HuimanNet.Domain.Formulas;

/// <summary>
/// Fragmento léxico de una fórmula, con su posición original para poder
/// señalar errores con precisión.
/// </summary>
/// <param name="Tipo">Clase léxica del fragmento.</param>
/// <param name="Texto">Texto tal y como aparece en la fórmula (identificadores en mayúsculas).</param>
/// <param name="Posicion">Índice base cero del primer carácter dentro de la fórmula.</param>
public readonly record struct Token(TipoDeToken Tipo, string Texto, int Posicion);
