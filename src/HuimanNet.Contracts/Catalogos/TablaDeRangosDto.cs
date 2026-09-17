namespace HuimanNet.Contracts.Catalogos;

/// <summary>
/// Tabla por rangos con sus renglones.
/// </summary>
/// <param name="Id">Identificador.</param>
/// <param name="Clave">Clave usada en <c>TABLA("CLAVE"; ...)</c>.</param>
/// <param name="Descripcion">Descripción.</param>
/// <param name="EmpresaId">Empresa a la que aplica, o <c>null</c>.</param>
/// <param name="VigenteDesde">Inicio de vigencia.</param>
/// <param name="VigenteHasta">Fin de vigencia, o <c>null</c>.</param>
/// <param name="FechaModificacion">Última modificación, en UTC.</param>
/// <param name="Rangos">Renglones ordenados por límite inferior.</param>
public sealed record TablaDeRangosDto(
    Guid Id,
    string Clave,
    string Descripcion,
    Guid? EmpresaId,
    DateOnly VigenteDesde,
    DateOnly? VigenteHasta,
    DateTimeOffset FechaModificacion,
    IReadOnlyList<RangoDeTablaDto> Rangos);
