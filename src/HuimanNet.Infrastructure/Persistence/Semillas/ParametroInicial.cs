namespace HuimanNet.Infrastructure.Persistence.Semillas;

/// <summary>Parámetro del catálogo inicial.</summary>
/// <param name="Clave">Clave.</param>
/// <param name="Descripcion">Descripción.</param>
/// <param name="Grupo">Grupo.</param>
/// <param name="Valor">Valor.</param>
/// <param name="Unidad">Unidad.</param>
public sealed record ParametroInicial(string Clave, string Descripcion, string Grupo, decimal Valor, string Unidad);
