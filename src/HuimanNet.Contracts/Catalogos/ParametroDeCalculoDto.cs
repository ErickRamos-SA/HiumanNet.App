namespace HuimanNet.Contracts.Catalogos;

/// <summary>
/// Parámetro del catálogo de cálculo.
/// </summary>
/// <param name="Id">Identificador.</param>
/// <param name="Clave">Clave usada en las fórmulas.</param>
/// <param name="Descripcion">Descripción.</param>
/// <param name="Grupo">Grupo de presentación.</param>
/// <param name="Valor">Valor.</param>
/// <param name="Unidad">Unidad de presentación.</param>
/// <param name="EmpresaId">Empresa a la que aplica, o <c>null</c> para el global.</param>
/// <param name="VigenteDesde">Inicio de vigencia.</param>
/// <param name="VigenteHasta">Fin de vigencia, o <c>null</c>.</param>
/// <param name="FechaModificacion">Última modificación, en UTC.</param>
public sealed record ParametroDeCalculoDto(
    Guid Id,
    string Clave,
    string Descripcion,
    string Grupo,
    decimal Valor,
    string Unidad,
    Guid? EmpresaId,
    DateOnly VigenteDesde,
    DateOnly? VigenteHasta,
    DateTimeOffset FechaModificacion);
