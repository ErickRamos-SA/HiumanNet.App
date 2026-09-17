namespace HuimanNet.Contracts.Auditoria;

/// <summary>
/// Intervalo de fechas que se consulta en la bitácora cuando no se indica otro.
/// </summary>
/// <remarks>
/// La API lo aplica si la petición omite las fechas y el portal lo usa como
/// filtro inicial, de modo que ambos muestran lo mismo.
/// </remarks>
public static class VentanaDeBitacora
{
    /// <summary>Días hacia atrás, desde el fin del intervalo, que abarca la consulta por omisión.</summary>
    public const int DiasPredeterminados = 30;
}
