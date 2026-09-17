using HuimanNet.Domain.Enums;

namespace HuimanNet.Application.Catalogos;

/// <summary>Obtiene la explicación completa de un esquema con el catálogo vigente.</summary>
/// <param name="Esquema">Esquema explicado.</param>
/// <param name="Idioma">Idioma del texto.</param>
/// <param name="EmpresaId">Empresa cuyo catálogo se usa, o <c>null</c> para el global.</param>
/// <param name="Fecha">Fecha de referencia, o <c>null</c> para hoy.</param>
public sealed record ObtenerExplicacionCompletaQuery(EsquemaDePago Esquema, Idioma Idioma, Guid? EmpresaId, DateOnly? Fecha);
