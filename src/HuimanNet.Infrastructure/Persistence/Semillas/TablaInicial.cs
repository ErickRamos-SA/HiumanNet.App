namespace HuimanNet.Infrastructure.Persistence.Semillas;

/// <summary>Tabla del catálogo inicial.</summary>
/// <param name="Clave">Clave.</param>
/// <param name="Descripcion">Descripción.</param>
/// <param name="Rangos">Renglones.</param>
public sealed record TablaInicial(string Clave, string Descripcion, IReadOnlyList<RangoInicial> Rangos);
