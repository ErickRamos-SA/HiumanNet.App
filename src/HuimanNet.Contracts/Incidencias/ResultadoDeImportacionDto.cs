namespace HuimanNet.Contracts.Incidencias;

/// <summary>
/// Resultado de una importación de incidencias.
/// </summary>
/// <param name="FilasLeidas">Filas con datos encontradas en el archivo.</param>
/// <param name="Creadas">Incidencias creadas.</param>
/// <param name="Actualizadas">Incidencias actualizadas.</param>
/// <param name="Errores">Filas que no pudieron importarse, con el motivo.</param>
public sealed record ResultadoDeImportacionDto(
    int FilasLeidas,
    int Creadas,
    int Actualizadas,
    IReadOnlyList<string> Errores);
