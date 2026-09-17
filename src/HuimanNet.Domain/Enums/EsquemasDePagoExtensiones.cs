namespace HuimanNet.Domain.Enums;

/// <summary>
/// Utilidades de conversión entre <see cref="EsquemaDePago"/> y <see cref="EsquemasDePago"/>.
/// </summary>
public static class EsquemasDePagoExtensiones
{
    /// <summary>
    /// Convierte un esquema concreto en su bandera equivalente.
    /// </summary>
    /// <param name="esquema">Esquema a convertir.</param>
    /// <returns>La bandera correspondiente.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si el esquema no está especificado.</exception>
    public static EsquemasDePago ComoBandera(this EsquemaDePago esquema) => esquema switch
    {
        EsquemaDePago.Imss => EsquemasDePago.Imss,
        EsquemaDePago.Sindicato => EsquemasDePago.Sindicato,
        EsquemaDePago.Honorarios => EsquemasDePago.Honorarios,
        _ => throw new ArgumentOutOfRangeException(nameof(esquema), esquema, "Esquema de pago no especificado."),
    };

    /// <summary>
    /// Indica si un conjunto de esquemas incluye uno concreto.
    /// </summary>
    /// <param name="esquemas">Conjunto de esquemas.</param>
    /// <param name="esquema">Esquema buscado.</param>
    /// <returns><c>true</c> si el conjunto incluye el esquema.</returns>
    public static bool Incluye(this EsquemasDePago esquemas, EsquemaDePago esquema)
        => (esquemas & esquema.ComoBandera()) != 0;
}
