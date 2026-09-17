using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Ejecución del cálculo de nómina de un período por parte del sistema.
/// </summary>
/// <remarks>
/// Cada corrida es inmutable una vez calculada: si cambian las incidencias o
/// el catálogo se genera una corrida nueva con número consecutivo. Así el
/// cotejo contra el resultado manual siempre se refiere a una foto concreta.
/// </remarks>
public sealed class CorridaDeNomina
{
    /// <summary>
    /// Inicializa una instancia con valores ya validados. Sólo la usan las
    /// fábricas y <see cref="Rehidratar"/>.
    /// </summary>
    /// <inheritdoc cref="Rehidratar" path="/param"/>
    private CorridaDeNomina(
        Guid id,
        Guid empresaId,
        Guid periodoId,
        int numero,
        EstadoDeCorrida estado,
        DateOnly fechaDeReferencia,
        DateTimeOffset fechaCalculo,
        Guid calculadaPorUsuarioId,
        TotalesDeCorrida totales,
        long duracionMs,
        string? observaciones,
        string? advertencias)
    {
        Id = id;
        EmpresaId = empresaId;
        PeriodoId = periodoId;
        Numero = numero;
        Estado = estado;
        FechaDeReferencia = fechaDeReferencia;
        FechaCalculo = fechaCalculo;
        CalculadaPorUsuarioId = calculadaPorUsuarioId;
        Totales = totales;
        DuracionMs = duracionMs;
        Observaciones = observaciones;
        Advertencias = advertencias;
    }

    /// <summary>Obtiene el identificador único.</summary>
    /// <value>Clave primaria.</value>
    public Guid Id { get; }

    /// <summary>Obtiene la empresa cliente.</summary>
    /// <value>Discriminante de aislamiento.</value>
    public Guid EmpresaId { get; }

    /// <summary>Obtiene el período calculado.</summary>
    /// <value>Identificador de <see cref="PeriodoCarga"/>.</value>
    public Guid PeriodoId { get; }

    /// <summary>Obtiene el número consecutivo de la corrida dentro del período.</summary>
    /// <value>Empieza en 1.</value>
    public int Numero { get; }

    /// <summary>Obtiene el estado.</summary>
    /// <value>Calculada, cotejada, aprobada o descartada.</value>
    public EstadoDeCorrida Estado { get; private set; }

    /// <summary>Obtiene la fecha con la que se resolvieron las vigencias del catálogo.</summary>
    /// <value>Normalmente el último día del período.</value>
    public DateOnly FechaDeReferencia { get; }

    /// <summary>Obtiene el instante del cálculo.</summary>
    /// <value>Instante en UTC.</value>
    public DateTimeOffset FechaCalculo { get; }

    /// <summary>Obtiene el usuario que ejecutó el cálculo.</summary>
    /// <value>Identificador local de <see cref="Usuario"/>.</value>
    public Guid CalculadaPorUsuarioId { get; }

    /// <summary>Obtiene los totales consolidados.</summary>
    /// <value>Objeto de valor inmutable.</value>
    public TotalesDeCorrida Totales { get; private set; }

    /// <summary>Obtiene la duración del cálculo.</summary>
    /// <value>Milisegundos; sirve para vigilar el rendimiento con empresas grandes.</value>
    public long DuracionMs { get; private set; }

    /// <summary>Obtiene la nota del usuario al calcular, cotejar o aprobar.</summary>
    /// <value>Texto libre, o <c>null</c>.</value>
    public string? Observaciones { get; private set; }

    /// <summary>Obtiene las advertencias generadas por el motor.</summary>
    /// <value>Una por línea, o <c>null</c> si no hubo.</value>
    public string? Advertencias { get; private set; }

    /// <summary>
    /// Indica si la corrida sigue abierta: calculada o cotejada, sin decisión final.
    /// </summary>
    /// <value><c>true</c> si todavía puede cotejarse, aprobarse, descartarse o reemplazarse.</value>
    public bool EstaAbierta => Estado is EstadoDeCorrida.Calculada or EstadoDeCorrida.Cotejada;

    /// <summary>
    /// Inicia una corrida.
    /// </summary>
    /// <param name="empresaId">Empresa cliente.</param>
    /// <param name="periodoId">Período.</param>
    /// <param name="numero">Número consecutivo dentro del período.</param>
    /// <param name="fechaDeReferencia">Fecha para resolver vigencias.</param>
    /// <param name="usuarioId">Usuario que calcula.</param>
    /// <param name="momento">Instante del cálculo, en UTC.</param>
    /// <param name="observaciones">Nota opcional.</param>
    /// <returns>La corrida en estado <see cref="EstadoDeCorrida.Calculada"/> con totales en cero.</returns>
    public static CorridaDeNomina Iniciar(
        Guid empresaId, Guid periodoId, int numero, DateOnly fechaDeReferencia, Guid usuarioId,
        DateTimeOffset momento, string? observaciones)
        => new(
            Guid.CreateVersion7(), empresaId, periodoId, numero, EstadoDeCorrida.Calculada, fechaDeReferencia,
            momento, usuarioId, TotalesDeCorrida.Vacios, 0, Limpiar(observaciones), null);

    /// <summary>
    /// Reconstruye una corrida a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="empresaId">Empresa.</param>
    /// <param name="periodoId">Período.</param>
    /// <param name="numero">Número consecutivo.</param>
    /// <param name="estado">Estado.</param>
    /// <param name="fechaDeReferencia">Fecha de referencia.</param>
    /// <param name="fechaCalculo">Instante del cálculo.</param>
    /// <param name="calculadaPorUsuarioId">Usuario que calculó.</param>
    /// <param name="totales">Totales.</param>
    /// <param name="duracionMs">Duración.</param>
    /// <param name="observaciones">Nota.</param>
    /// <param name="advertencias">Advertencias.</param>
    /// <returns>La entidad rehidratada.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static CorridaDeNomina Rehidratar(
        Guid id,
        Guid empresaId,
        Guid periodoId,
        int numero,
        EstadoDeCorrida estado,
        DateOnly fechaDeReferencia,
        DateTimeOffset fechaCalculo,
        Guid calculadaPorUsuarioId,
        TotalesDeCorrida totales,
        long duracionMs,
        string? observaciones,
        string? advertencias)
        => new(id, empresaId, periodoId, numero, estado, fechaDeReferencia, fechaCalculo, calculadaPorUsuarioId,
            totales, duracionMs, observaciones, advertencias);

    /// <summary>
    /// Registra el resultado consolidado del cálculo.
    /// </summary>
    /// <param name="totales">Totales consolidados.</param>
    /// <param name="duracionMs">Duración del cálculo en milisegundos.</param>
    /// <param name="advertencias">Advertencias del motor, o vacío.</param>
    public void RegistrarTotales(TotalesDeCorrida totales, long duracionMs, IReadOnlyCollection<string> advertencias)
    {
        ArgumentNullException.ThrowIfNull(totales);
        ArgumentNullException.ThrowIfNull(advertencias);

        Totales = totales;
        DuracionMs = duracionMs;
        Advertencias = advertencias.Count == 0 ? null : string.Join('\n', advertencias);
    }

    /// <summary>
    /// Marca la corrida como cotejada contra el resultado manual.
    /// </summary>
    /// <exception cref="NominaInvalidaException">Se lanza si la corrida está descartada o aprobada.</exception>
    public void MarcarCotejada()
    {
        if (Estado is EstadoDeCorrida.Descartada or EstadoDeCorrida.Aprobada)
        {
            throw new NominaInvalidaException($"La corrida no admite cotejo en estado '{Estado}'.");
        }

        Estado = EstadoDeCorrida.Cotejada;
    }

    /// <summary>
    /// Aprueba la corrida como resultado definitivo del período.
    /// </summary>
    /// <param name="observaciones">Nota opcional.</param>
    /// <exception cref="NominaInvalidaException">Se lanza si la corrida está descartada.</exception>
    public void Aprobar(string? observaciones)
    {
        if (Estado == EstadoDeCorrida.Descartada)
        {
            throw new NominaInvalidaException("Una corrida descartada no puede aprobarse.");
        }

        Estado = EstadoDeCorrida.Aprobada;
        Observaciones = Limpiar(observaciones) ?? Observaciones;
    }

    /// <summary>
    /// Descarta la corrida.
    /// </summary>
    /// <param name="observaciones">Motivo del descarte.</param>
    /// <exception cref="NominaInvalidaException">Se lanza si la corrida ya está aprobada.</exception>
    public void Descartar(string? observaciones)
    {
        if (Estado == EstadoDeCorrida.Aprobada)
        {
            throw new NominaInvalidaException("Una corrida aprobada no puede descartarse.");
        }

        Estado = EstadoDeCorrida.Descartada;
        Observaciones = Limpiar(observaciones) ?? Observaciones;
    }

    /// <summary>
    /// Marca la corrida como reemplazada por un reproceso del período.
    /// </summary>
    /// <param name="numeroDeLaNueva">Número de la corrida que la sustituye.</param>
    /// <exception cref="NominaInvalidaException">Se lanza si la corrida ya está aprobada o descartada.</exception>
    /// <remarks>
    /// Queda descartada con una nota que remite a la nueva: se conserva para
    /// consulta y auditoría, pero deja de ser el resultado vigente del período.
    /// </remarks>
    public void Reemplazar(int numeroDeLaNueva)
    {
        // Misma longitud que la columna Observaciones.
        const int longitudMaxima = 1000;

        if (!EstaAbierta)
        {
            throw new NominaInvalidaException($"Sólo una corrida calculada o cotejada puede reemplazarse; su estado es '{Estado}'.");
        }

        string nota = $"Reemplazada por la corrida #{numeroDeLaNueva}.";
        string texto = Observaciones is null ? nota : $"{Observaciones} · {nota}";

        Estado = EstadoDeCorrida.Descartada;
        Observaciones = texto.Length <= longitudMaxima ? texto : texto[^longitudMaxima..];
    }

    /// <summary>Normaliza un texto opcional.</summary>
    /// <param name="valor">Texto capturado.</param>
    /// <returns>El texto sin espacios en los extremos, o <c>null</c> si está vacío.</returns>
    private static string? Limpiar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
