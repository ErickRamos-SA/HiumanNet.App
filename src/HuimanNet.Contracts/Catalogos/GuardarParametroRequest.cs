namespace HuimanNet.Contracts.Catalogos;

/// <summary>
/// Petición para crear o actualizar un parámetro.
/// </summary>
/// <param name="Clave">Clave usada en las fórmulas (no se cambia al actualizar).</param>
/// <param name="Descripcion">Descripción.</param>
/// <param name="Grupo">Grupo de presentación.</param>
/// <param name="Valor">Valor.</param>
/// <param name="Unidad">Unidad de presentación.</param>
/// <param name="EmpresaId">Empresa a la que aplica, o <c>null</c> para el global.</param>
/// <param name="VigenteDesde">Inicio de vigencia.</param>
/// <param name="VigenteHasta">Fin de vigencia, o <c>null</c>.</param>
public sealed record GuardarParametroRequest(
    string Clave,
    string Descripcion,
    string Grupo,
    decimal Valor,
    string Unidad,
    Guid? EmpresaId,
    DateOnly VigenteDesde,
    DateOnly? VigenteHasta);
