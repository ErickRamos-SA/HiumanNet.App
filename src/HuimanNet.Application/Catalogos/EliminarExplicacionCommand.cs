namespace HuimanNet.Application.Catalogos;

/// <summary>Elimina una sección de explicación.</summary>
/// <param name="ExplicacionId">Sección a eliminar.</param>
public sealed record EliminarExplicacionCommand(Guid ExplicacionId);
