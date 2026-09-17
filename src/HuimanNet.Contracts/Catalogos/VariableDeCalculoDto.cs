namespace HuimanNet.Contracts.Catalogos;

/// <summary>
/// Variable de entrada disponible para las fórmulas.
/// </summary>
/// <param name="Clave">Identificador.</param>
/// <param name="Descripcion">Qué representa.</param>
/// <param name="Origen">De dónde toma su valor.</param>
public sealed record VariableDeCalculoDto(string Clave, string Descripcion, string Origen);
