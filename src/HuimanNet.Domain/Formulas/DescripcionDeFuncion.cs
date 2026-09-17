namespace HuimanNet.Domain.Formulas;

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
