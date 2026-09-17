namespace HuimanNet.Domain.Nomina;

/// <summary>
/// Descripción de una variable de cálculo, para la ayuda del administrador.
/// </summary>
/// <param name="Clave">Identificador que se usa en las fórmulas.</param>
/// <param name="Descripcion">Qué representa la variable.</param>
/// <param name="Origen">De dónde toma su valor.</param>
public sealed record DescripcionDeVariable(string Clave, string Descripcion, OrigenDeVariable Origen);
