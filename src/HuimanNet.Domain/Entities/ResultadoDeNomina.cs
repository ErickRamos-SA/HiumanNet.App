using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Nomina;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Cifras principales del resultado de un contrato, leídas de los conceptos
/// con las claves canónicas de <see cref="ClavesDeResumen"/>.
/// </summary>
/// <param name="Bruto">Bruto de incidencias.</param>
/// <param name="TotalPercepciones">Total de percepciones del recibo.</param>
/// <param name="TotalDeducciones">Total de deducciones del recibo.</param>
/// <param name="Neto">Neto pagado.</param>
/// <param name="Isr">ISR retenido.</param>
/// <param name="Subsidio">Subsidio al empleo entregado.</param>
/// <param name="ImssTrabajador">Cuota obrera descontada.</param>
/// <param name="ImssPatronal">Cuotas patronales IMSS.</param>
/// <param name="InfonavitPatronal">Aportación patronal INFONAVIT.</param>
/// <param name="InfonavitTrabajador">Descuento INFONAVIT al trabajador.</param>
/// <param name="Fonacot">Descuento FONACOT.</param>
/// <param name="Isn">Impuesto sobre nóminas.</param>
/// <param name="ComplementoSindical">Complemento pagado vía sindicato.</param>
/// <param name="Facturable">Base facturable al cliente.</param>
/// <param name="Comision">Comisión al cliente.</param>
/// <param name="CostoTotal">Costo total antes de IVA.</param>
/// <param name="CostoIsr">ISR trasladado en la factura.</param>
/// <param name="CostoImss">Cuotas IMSS trasladadas en la factura.</param>
/// <param name="CostoInfonavit">Retiro, cesantía e INFONAVIT trasladados en la factura.</param>
/// <param name="CostoOtros">Otros costos facturables.</param>
public sealed record ResumenDeResultado(
    decimal Bruto,
    decimal TotalPercepciones,
    decimal TotalDeducciones,
    decimal Neto,
    decimal Isr,
    decimal Subsidio,
    decimal ImssTrabajador,
    decimal ImssPatronal,
    decimal InfonavitPatronal,
    decimal InfonavitTrabajador,
    decimal Fonacot,
    decimal Isn,
    decimal ComplementoSindical,
    decimal Facturable,
    decimal Comision,
    decimal CostoTotal,
    decimal CostoIsr,
    decimal CostoImss,
    decimal CostoInfonavit,
    decimal CostoOtros)
{
    /// <summary>
    /// Construye el resumen a partir de los conceptos calculados.
    /// </summary>
    /// <param name="calculo">Resultado del motor.</param>
    /// <returns>El resumen; los conceptos ausentes se leen como cero.</returns>
    public static ResumenDeResultado Desde(ResultadoDeCalculo calculo)
    {
        ArgumentNullException.ThrowIfNull(calculo);

        return new ResumenDeResultado(
            calculo.Obtener(ClavesDeResumen.BrutoIncidencias),
            calculo.Obtener(ClavesDeResumen.TotalPercepciones),
            calculo.Obtener(ClavesDeResumen.TotalDeducciones),
            calculo.Obtener(ClavesDeResumen.NetoPagado),
            calculo.Obtener(ClavesDeResumen.Isr),
            calculo.Obtener(ClavesDeResumen.SubsidioEntregado),
            calculo.Obtener(ClavesDeResumen.ImssTrabajador),
            calculo.Obtener(ClavesDeResumen.ImssPatronal),
            calculo.Obtener(ClavesDeResumen.InfonavitPatronal),
            calculo.Obtener(ClavesDeResumen.InfonavitTrabajador),
            calculo.Obtener(ClavesDeResumen.Fonacot),
            calculo.Obtener(ClavesDeResumen.Isn),
            calculo.Obtener(ClavesDeResumen.ComplementoSindical),
            calculo.Obtener(ClavesDeResumen.TotalNominaFacturable),
            calculo.Obtener(ClavesDeResumen.Comision),
            calculo.Obtener(ClavesDeResumen.CostoTotal),
            calculo.Obtener(ClavesDeResumen.CostoIsr),
            calculo.Obtener(ClavesDeResumen.CostoImss),
            calculo.Obtener(ClavesDeResumen.CostoInfonavit),
            calculo.Obtener(ClavesDeResumen.CostoOtros));
    }
}

/// <summary>
/// Resultado del cálculo de un contrato dentro de una corrida: el resumen y el
/// detalle de todos los conceptos evaluados.
/// </summary>
/// <remarks>
/// El detalle se conserva completo para que el cotejo pueda comparar cualquier
/// concepto con el resultado manual y para que el usuario vea cómo se llegó a
/// cada importe.
/// </remarks>
public sealed class ResultadoDeNomina
{
    private ResultadoDeNomina(
        Guid id,
        Guid corridaId,
        Guid empresaId,
        Guid contratoId,
        Guid empleadoId,
        Guid razonSocialId,
        EsquemaDePago esquema,
        string claveEmpleado,
        string nombreEmpleado,
        TipoDeMovimiento tipoDeMovimiento,
        ResumenDeResultado resumen,
        IReadOnlyList<ValorDeConcepto> conceptos,
        string? advertencia)
    {
        Id = id;
        CorridaId = corridaId;
        EmpresaId = empresaId;
        ContratoId = contratoId;
        EmpleadoId = empleadoId;
        RazonSocialId = razonSocialId;
        Esquema = esquema;
        ClaveEmpleado = claveEmpleado;
        NombreEmpleado = nombreEmpleado;
        TipoDeMovimiento = tipoDeMovimiento;
        Resumen = resumen;
        Conceptos = conceptos;
        Advertencia = advertencia;
    }

    /// <summary>Obtiene el identificador único.</summary>
    /// <value>Clave primaria.</value>
    public Guid Id { get; }

    /// <summary>Obtiene la corrida.</summary>
    /// <value>Identificador de <see cref="CorridaDeNomina"/>.</value>
    public Guid CorridaId { get; }

    /// <summary>Obtiene la empresa cliente.</summary>
    /// <value>Discriminante de aislamiento.</value>
    public Guid EmpresaId { get; }

    /// <summary>Obtiene el contrato calculado.</summary>
    /// <value>Identificador de <see cref="Contrato"/>.</value>
    public Guid ContratoId { get; }

    /// <summary>Obtiene el empleado.</summary>
    /// <value>Identificador de <see cref="Empleado"/>.</value>
    public Guid EmpleadoId { get; }

    /// <summary>Obtiene la razón social pagadora.</summary>
    /// <value>Identificador de <see cref="RazonSocial"/>.</value>
    public Guid RazonSocialId { get; }

    /// <summary>Obtiene el esquema con el que se calculó.</summary>
    /// <value>IMSS, sindicato u honorarios.</value>
    public EsquemaDePago Esquema { get; }

    /// <summary>Obtiene la clave del empleado en el momento del cálculo.</summary>
    /// <value>Copia desnormalizada para reportes y cotejo.</value>
    public string ClaveEmpleado { get; }

    /// <summary>Obtiene el nombre del empleado en el momento del cálculo.</summary>
    /// <value>Copia desnormalizada para reportes.</value>
    public string NombreEmpleado { get; }

    /// <summary>Obtiene el tipo de movimiento del período.</summary>
    /// <value>Ordinaria o finiquito.</value>
    public TipoDeMovimiento TipoDeMovimiento { get; }

    /// <summary>Obtiene las cifras principales.</summary>
    /// <value>Objeto de valor inmutable.</value>
    public ResumenDeResultado Resumen { get; }

    /// <summary>Obtiene el detalle de conceptos en orden de evaluación.</summary>
    /// <value>Lista de sólo lectura.</value>
    public IReadOnlyList<ValorDeConcepto> Conceptos { get; }

    /// <summary>Obtiene la advertencia del motor para este contrato.</summary>
    /// <value>Texto, o <c>null</c> si no hubo.</value>
    public string? Advertencia { get; }

    /// <summary>
    /// Crea el resultado de un contrato.
    /// </summary>
    /// <param name="corridaId">Corrida.</param>
    /// <param name="contrato">Contrato calculado.</param>
    /// <param name="empleado">Empleado del contrato.</param>
    /// <param name="tipoDeMovimiento">Tipo de movimiento del período.</param>
    /// <param name="calculo">Resultado del motor.</param>
    /// <param name="advertencia">Advertencia, si la hubo.</param>
    /// <returns>El resultado listo para persistirse.</returns>
    public static ResultadoDeNomina Crear(
        Guid corridaId,
        Contrato contrato,
        Empleado empleado,
        TipoDeMovimiento tipoDeMovimiento,
        ResultadoDeCalculo calculo,
        string? advertencia)
    {
        ArgumentNullException.ThrowIfNull(contrato);
        ArgumentNullException.ThrowIfNull(empleado);
        ArgumentNullException.ThrowIfNull(calculo);

        return new ResultadoDeNomina(
            Guid.CreateVersion7(),
            corridaId,
            contrato.EmpresaId,
            contrato.Id,
            empleado.Id,
            contrato.RazonSocialId,
            contrato.Esquema,
            empleado.Clave,
            empleado.NombreCompleto,
            tipoDeMovimiento,
            ResumenDeResultado.Desde(calculo),
            calculo.Valores,
            advertencia);
    }

    /// <summary>
    /// Reconstruye un resultado a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="corridaId">Corrida.</param>
    /// <param name="empresaId">Empresa.</param>
    /// <param name="contratoId">Contrato.</param>
    /// <param name="empleadoId">Empleado.</param>
    /// <param name="razonSocialId">Razón social.</param>
    /// <param name="esquema">Esquema.</param>
    /// <param name="claveEmpleado">Clave del empleado.</param>
    /// <param name="nombreEmpleado">Nombre del empleado.</param>
    /// <param name="tipoDeMovimiento">Tipo de movimiento.</param>
    /// <param name="resumen">Resumen.</param>
    /// <param name="conceptos">Detalle de conceptos.</param>
    /// <param name="advertencia">Advertencia.</param>
    /// <returns>La entidad rehidratada.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static ResultadoDeNomina Rehidratar(
        Guid id,
        Guid corridaId,
        Guid empresaId,
        Guid contratoId,
        Guid empleadoId,
        Guid razonSocialId,
        EsquemaDePago esquema,
        string claveEmpleado,
        string nombreEmpleado,
        TipoDeMovimiento tipoDeMovimiento,
        ResumenDeResultado resumen,
        IReadOnlyList<ValorDeConcepto> conceptos,
        string? advertencia)
        => new(id, corridaId, empresaId, contratoId, empleadoId, razonSocialId, esquema, claveEmpleado, nombreEmpleado,
            tipoDeMovimiento, resumen, conceptos, advertencia);
}
