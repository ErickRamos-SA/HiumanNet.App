namespace HuimanNet.Contracts.Catalogos;

/// <summary>
/// Petición para crear o actualizar una tabla por rangos.
/// </summary>
/// <param name="Clave">Clave (no se cambia al actualizar).</param>
/// <param name="Descripcion">Descripción.</param>
/// <param name="EmpresaId">Empresa a la que aplica, o <c>null</c>.</param>
/// <param name="VigenteDesde">Inicio de vigencia.</param>
/// <param name="VigenteHasta">Fin de vigencia, o <c>null</c>.</param>
/// <param name="Rangos">Renglones.</param>
public sealed record GuardarTablaRequest(
    string Clave,
    string Descripcion,
    Guid? EmpresaId,
    DateOnly VigenteDesde,
    DateOnly? VigenteHasta,
    IReadOnlyList<RangoDeTablaDto> Rangos);
