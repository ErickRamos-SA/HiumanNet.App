using HuimanNet.Domain.Enums;

namespace HuimanNet.Application.Catalogos;

/// <summary>Lista las secciones de explicación.</summary>
/// <param name="Esquema">Esquema a filtrar, o <c>null</c>.</param>
/// <param name="Idioma">Idioma a filtrar, o <c>null</c>.</param>
public sealed record ListarExplicacionesQuery(EsquemaDePago? Esquema, Idioma? Idioma);
