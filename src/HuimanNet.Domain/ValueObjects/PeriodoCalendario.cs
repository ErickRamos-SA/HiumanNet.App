using System.Globalization;
using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.ValueObjects;

/// <summary>
/// Identifica un período de nómina en el calendario: año, mes y número de
/// período dentro del mes (por ejemplo, primera y segunda quincena).
/// </summary>
/// <remarks>
/// Sirve como clave natural y estable para organizar los contenedores de Blob
/// Storage y para ordenar la bandeja del operador.
/// </remarks>
public readonly record struct PeriodoCalendario
{
    /// <summary>Año mínimo aceptado por el sistema.</summary>
    public const int AnioMinimo = 2000;

    /// <summary>Año máximo aceptado por el sistema.</summary>
    public const int AnioMaximo = 2100;

    /// <summary>Número máximo de períodos dentro de un mismo mes (semanal).</summary>
    public const int ConsecutivoMaximo = 5;

    private PeriodoCalendario(int anio, int mes, int consecutivo)
    {
        Anio = anio;
        Mes = mes;
        Consecutivo = consecutivo;
    }

    /// <summary>
    /// Obtiene el año del período.
    /// </summary>
    /// <value>Entre <see cref="AnioMinimo"/> y <see cref="AnioMaximo"/>.</value>
    public int Anio { get; }

    /// <summary>
    /// Obtiene el mes del período.
    /// </summary>
    /// <value>Entre 1 y 12.</value>
    public int Mes { get; }

    /// <summary>
    /// Obtiene el número de período dentro del mes.
    /// </summary>
    /// <value>1 para mensual, 1 o 2 para quincenal, hasta <see cref="ConsecutivoMaximo"/> para semanal.</value>
    public int Consecutivo { get; }

    /// <summary>
    /// Obtiene la clave canónica del período, apta para rutas y ordenación lexicográfica.
    /// </summary>
    /// <value>Formato <c>aaaa-MM-cc</c>, por ejemplo <c>"2026-08-01"</c>.</value>
    public string Clave => string.Create(
        CultureInfo.InvariantCulture,
        $"{Anio:D4}-{Mes:D2}-{Consecutivo:D2}");

    /// <summary>
    /// Crea un <see cref="PeriodoCalendario"/> validando los rangos.
    /// </summary>
    /// <param name="anio">Año del período.</param>
    /// <param name="mes">Mes del período (1-12).</param>
    /// <param name="consecutivo">Número de período dentro del mes.</param>
    /// <returns>Instancia validada e inmutable.</returns>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si alguno de los componentes está fuera de rango.
    /// </exception>
    public static PeriodoCalendario Crear(int anio, int mes, int consecutivo)
    {
        if (anio is < AnioMinimo or > AnioMaximo)
        {
            throw new DocumentoInvalidoException(
                $"El año del período debe estar entre {AnioMinimo} y {AnioMaximo}.");
        }

        if (mes is < 1 or > 12)
        {
            throw new DocumentoInvalidoException("El mes del período debe estar entre 1 y 12.");
        }

        if (consecutivo is < 1 or > ConsecutivoMaximo)
        {
            throw new DocumentoInvalidoException(
                $"El consecutivo del período debe estar entre 1 y {ConsecutivoMaximo}.");
        }

        return new PeriodoCalendario(anio, mes, consecutivo);
    }

    /// <summary>
    /// Reconstruye un período a partir de su clave canónica.
    /// </summary>
    /// <param name="clave">Clave en formato <c>aaaa-MM-cc</c>.</param>
    /// <returns>Instancia validada e inmutable.</returns>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si la clave no tiene el formato esperado o sus componentes están fuera de rango.
    /// </exception>
    public static PeriodoCalendario DesdeClave(string? clave)
    {
        if (string.IsNullOrWhiteSpace(clave))
        {
            throw new DocumentoInvalidoException("La clave del período es obligatoria.");
        }

        string[] partes = clave.Split('-', StringSplitOptions.TrimEntries);

        if (partes.Length != 3
            || !int.TryParse(partes[0], CultureInfo.InvariantCulture, out int anio)
            || !int.TryParse(partes[1], CultureInfo.InvariantCulture, out int mes)
            || !int.TryParse(partes[2], CultureInfo.InvariantCulture, out int consecutivo))
        {
            throw new DocumentoInvalidoException(
                $"La clave de período '{clave}' no tiene el formato 'aaaa-MM-cc'.");
        }

        return Crear(anio, mes, consecutivo);
    }

    /// <summary>
    /// Devuelve la clave canónica del período.
    /// </summary>
    /// <returns>El valor de <see cref="Clave"/>.</returns>
    public override string ToString() => Clave;
}
