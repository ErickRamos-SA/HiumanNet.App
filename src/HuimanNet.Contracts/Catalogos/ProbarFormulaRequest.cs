using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Catalogos;

/// <summary>
/// Petición para probar una fórmula con valores de ejemplo.
/// </summary>
/// <param name="Formula">Fórmula a evaluar.</param>
/// <param name="Esquema">Esquema cuyo catálogo se usa como contexto.</param>
/// <param name="EmpresaId">Empresa cuyo catálogo se usa, o <c>null</c> para el global.</param>
/// <param name="Valores">Valores de las variables de entrada; las no indicadas valen cero.</param>
public sealed record ProbarFormulaRequest(
    string Formula,
    EsquemaDePago Esquema,
    Guid? EmpresaId,
    IReadOnlyList<Contracts.Nomina.ValorDto> Valores);
