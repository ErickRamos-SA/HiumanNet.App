namespace HuimanNet.Application.Catalogos;

/// <summary>Elimina una tabla.</summary>
/// <param name="TablaId">Tabla a eliminar.</param>
public sealed record EliminarTablaCommand(Guid TablaId);
