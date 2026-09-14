using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Cantidades e importes capturados para un contrato en un período.
/// </summary>
/// <param name="DiasPeriodo">Días del período de pago.</param>
/// <param name="Vacaciones">Días de vacaciones.</param>
/// <param name="Ausentismos">Días de ausentismo.</param>
/// <param name="Incapacidades">Días de incapacidad.</param>
/// <param name="Festivos">Días festivos laborados.</param>
/// <param name="HorasDobles">Horas extra dobles.</param>
/// <param name="HorasTriples">Horas extra triples.</param>
/// <param name="DomingosTrabajados">Domingos trabajados.</param>
/// <param name="Gratificacion">Gratificaciones y bonos del período.</param>
/// <param name="Reembolsos">Reembolsos y otros.</param>
/// <param name="Teletrabajo">Apoyo por teletrabajo.</param>
/// <param name="Finiquito">Importe de finiquito.</param>
/// <param name="Cafeteria">Gastos de cafetería.</param>
/// <param name="HorasDescontadas">Horas a descontar.</param>
/// <param name="OtrosDescuentos">Otros descuentos personales.</param>
/// <param name="PrestamoPersonal">Descuento de préstamo personal del período.</param>
/// <param name="Aguinaldo">Aguinaldo pagado en el período.</param>
/// <param name="DescuentosFiscales">Otros descuentos del recibo fiscal.</param>
/// <param name="FonacotCapturado">Descuento FONACOT capturado manualmente.</param>
/// <param name="DescuentoSindicalAdicional">Descuento adicional dentro del complemento sindical.</param>
/// <param name="AjusteSindical">Ajuste manual al complemento sindical (positivo o negativo).</param>
/// <param name="IsrManual">Retención manual de ISR, o <c>null</c> para calcularla.</param>
/// <param name="TipoDeMovimiento">Nómina ordinaria o finiquito.</param>
/// <param name="Observaciones">Nota libre; documenta los ajustes manuales y su vigencia.</param>
public sealed record DatosDeIncidencia(
    decimal DiasPeriodo,
    decimal Vacaciones,
    decimal Ausentismos,
    decimal Incapacidades,
    decimal Festivos,
    decimal HorasDobles,
    decimal HorasTriples,
    decimal DomingosTrabajados,
    decimal Gratificacion,
    decimal Reembolsos,
    decimal Teletrabajo,
    decimal Finiquito,
    decimal Cafeteria,
    decimal HorasDescontadas,
    decimal OtrosDescuentos,
    decimal PrestamoPersonal,
    decimal Aguinaldo,
    decimal DescuentosFiscales,
    decimal FonacotCapturado,
    decimal DescuentoSindicalAdicional,
    decimal AjusteSindical,
    decimal? IsrManual,
    TipoDeMovimiento TipoDeMovimiento,
    string? Observaciones)
{
    /// <summary>
    /// Crea una incidencia de semana completa sin novedades.
    /// </summary>
    /// <param name="diasPeriodo">Días del período de pago.</param>
    /// <returns>Datos con todas las cantidades en cero salvo los días del período.</returns>
    public static DatosDeIncidencia SinNovedades(decimal diasPeriodo)
        => new(diasPeriodo, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, null, TipoDeMovimiento.Ordinaria, null);
}

/// <summary>
/// Incidencias de un contrato en un período de nómina: la entrada variable del
/// cálculo, capturada por la empresa cliente o importada desde su archivo.
/// </summary>
/// <remarks>
/// Hay a lo sumo una incidencia por contrato y período. Si un contrato no tiene
/// incidencia capturada, el cálculo asume período completo sin novedades.
/// </remarks>
public sealed class Incidencia
{
    private Incidencia(
        Guid id,
        Guid empresaId,
        Guid periodoId,
        Guid contratoId,
        DatosDeIncidencia datos,
        Guid capturadoPorUsuarioId,
        DateTimeOffset fechaCaptura)
    {
        Id = id;
        EmpresaId = empresaId;
        PeriodoId = periodoId;
        ContratoId = contratoId;
        Datos = datos;
        CapturadoPorUsuarioId = capturadoPorUsuarioId;
        FechaCaptura = fechaCaptura;
    }

    /// <summary>Obtiene el identificador único.</summary>
    /// <value>Clave primaria.</value>
    public Guid Id { get; }

    /// <summary>Obtiene la empresa cliente.</summary>
    /// <value>Discriminante de aislamiento.</value>
    public Guid EmpresaId { get; }

    /// <summary>Obtiene el período.</summary>
    /// <value>Identificador de <see cref="PeriodoCarga"/>.</value>
    public Guid PeriodoId { get; }

    /// <summary>Obtiene el contrato.</summary>
    /// <value>Identificador de <see cref="Contrato"/>.</value>
    public Guid ContratoId { get; }

    /// <summary>Obtiene las cantidades e importes capturados.</summary>
    /// <value>Objeto de valor inmutable.</value>
    public DatosDeIncidencia Datos { get; private set; }

    /// <summary>Obtiene el usuario que capturó o modificó por última vez.</summary>
    /// <value>Identificador local de <see cref="Usuario"/>.</value>
    public Guid CapturadoPorUsuarioId { get; private set; }

    /// <summary>Obtiene el instante de la última captura.</summary>
    /// <value>Instante en UTC.</value>
    public DateTimeOffset FechaCaptura { get; private set; }

    /// <summary>
    /// Registra la incidencia de un contrato en un período.
    /// </summary>
    /// <param name="empresaId">Empresa cliente.</param>
    /// <param name="periodoId">Período.</param>
    /// <param name="contratoId">Contrato.</param>
    /// <param name="datos">Cantidades e importes.</param>
    /// <param name="usuarioId">Usuario que captura.</param>
    /// <param name="momento">Instante de la captura, en UTC.</param>
    /// <returns>La incidencia creada.</returns>
    /// <exception cref="NominaInvalidaException">Se lanza si las cantidades no son coherentes.</exception>
    public static Incidencia Registrar(
        Guid empresaId, Guid periodoId, Guid contratoId, DatosDeIncidencia datos, Guid usuarioId, DateTimeOffset momento)
        => new(Guid.CreateVersion7(), empresaId, periodoId, contratoId, Validar(datos), usuarioId, momento);

    /// <summary>
    /// Reconstruye una incidencia a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="empresaId">Empresa.</param>
    /// <param name="periodoId">Período.</param>
    /// <param name="contratoId">Contrato.</param>
    /// <param name="datos">Cantidades e importes.</param>
    /// <param name="capturadoPorUsuarioId">Usuario que capturó.</param>
    /// <param name="fechaCaptura">Instante de captura.</param>
    /// <returns>La entidad rehidratada.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static Incidencia Rehidratar(
        Guid id,
        Guid empresaId,
        Guid periodoId,
        Guid contratoId,
        DatosDeIncidencia datos,
        Guid capturadoPorUsuarioId,
        DateTimeOffset fechaCaptura)
        => new(id, empresaId, periodoId, contratoId, datos, capturadoPorUsuarioId, fechaCaptura);

    /// <summary>
    /// Sustituye las cantidades capturadas.
    /// </summary>
    /// <param name="datos">Nuevas cantidades e importes.</param>
    /// <param name="usuarioId">Usuario que modifica.</param>
    /// <param name="momento">Instante de la modificación, en UTC.</param>
    /// <exception cref="NominaInvalidaException">Se lanza si las cantidades no son coherentes.</exception>
    public void Actualizar(DatosDeIncidencia datos, Guid usuarioId, DateTimeOffset momento)
    {
        Datos = Validar(datos);
        CapturadoPorUsuarioId = usuarioId;
        FechaCaptura = momento;
    }

    private static DatosDeIncidencia Validar(DatosDeIncidencia datos)
    {
        ArgumentNullException.ThrowIfNull(datos);

        if (datos.DiasPeriodo <= 0)
        {
            throw new NominaInvalidaException("Los días del período deben ser mayores que cero.");
        }

        if (datos.Vacaciones < 0 || datos.Ausentismos < 0 || datos.Incapacidades < 0 || datos.Festivos < 0
            || datos.HorasDobles < 0 || datos.HorasTriples < 0 || datos.DomingosTrabajados < 0
            || datos.HorasDescontadas < 0)
        {
            throw new NominaInvalidaException("Las cantidades de días y horas no pueden ser negativas.");
        }

        if (datos.Vacaciones + datos.Ausentismos + datos.Incapacidades > datos.DiasPeriodo)
        {
            throw new NominaInvalidaException(
                "La suma de vacaciones, ausentismos e incapacidades no puede superar los días del período.");
        }

        if (!Enum.IsDefined(datos.TipoDeMovimiento))
        {
            throw new NominaInvalidaException("Debe indicarse el tipo de movimiento (ordinaria o finiquito).");
        }

        return datos with
        {
            Observaciones = string.IsNullOrWhiteSpace(datos.Observaciones) ? null : datos.Observaciones.Trim(),
        };
    }
}
