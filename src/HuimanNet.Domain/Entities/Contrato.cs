using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Vínculo entre un empleado y una razón social bajo un esquema de pago.
/// </summary>
/// <remarks>
/// Es la unidad que calcula el motor: un empleado con sueldo mixto tiene un
/// contrato por cada parte de su sueldo. Los contratos vigentes en la fecha del
/// período son los que entran en la corrida.
/// </remarks>
public sealed class Contrato
{
    /// <summary>
    /// Inicializa una instancia con valores ya validados. Sólo la usan las
    /// fábricas y <see cref="Rehidratar"/>.
    /// </summary>
    /// <inheritdoc cref="Rehidratar" path="/param"/>
    private Contrato(
        Guid id,
        Guid empleadoId,
        Guid empresaId,
        Guid razonSocialId,
        EsquemaDePago esquema,
        string? numeroTrabajador,
        string? puesto,
        string? departamento,
        string? tipoDeContrato,
        CondicionesDeContrato condiciones,
        DateOnly fechaAlta,
        DateOnly? fechaBaja,
        DateTimeOffset fechaModificacion)
    {
        Id = id;
        EmpleadoId = empleadoId;
        EmpresaId = empresaId;
        RazonSocialId = razonSocialId;
        Esquema = esquema;
        NumeroTrabajador = numeroTrabajador;
        Puesto = puesto;
        Departamento = departamento;
        TipoDeContrato = tipoDeContrato;
        Condiciones = condiciones;
        FechaAlta = fechaAlta;
        FechaBaja = fechaBaja;
        FechaModificacion = fechaModificacion;
    }

    /// <summary>Obtiene el identificador único.</summary>
    /// <value>Clave primaria.</value>
    public Guid Id { get; }

    /// <summary>Obtiene el empleado.</summary>
    /// <value>Identificador de <see cref="Empleado"/>.</value>
    public Guid EmpleadoId { get; }

    /// <summary>Obtiene la empresa cliente.</summary>
    /// <value>Discriminante de aislamiento; coincide con la del empleado y la de la razón social.</value>
    public Guid EmpresaId { get; }

    /// <summary>Obtiene la razón social que paga el contrato.</summary>
    /// <value>Identificador de <see cref="RazonSocial"/>.</value>
    public Guid RazonSocialId { get; private set; }

    /// <summary>Obtiene el esquema de pago.</summary>
    /// <value>IMSS, sindicato u honorarios.</value>
    public EsquemaDePago Esquema { get; private set; }

    /// <summary>Obtiene el número de trabajador ante la razón social (NOI).</summary>
    /// <value>Texto libre, o <c>null</c>.</value>
    public string? NumeroTrabajador { get; private set; }

    /// <summary>Obtiene el puesto.</summary>
    /// <value>Texto libre, o <c>null</c>.</value>
    public string? Puesto { get; private set; }

    /// <summary>Obtiene el departamento o campaña.</summary>
    /// <value>Texto libre, o <c>null</c>.</value>
    public string? Departamento { get; private set; }

    /// <summary>Obtiene el tipo de contrato (presencial, remoto, etc.).</summary>
    /// <value>Texto libre, o <c>null</c>.</value>
    public string? TipoDeContrato { get; private set; }

    /// <summary>Obtiene las condiciones económicas.</summary>
    /// <value>Objeto de valor inmutable.</value>
    public CondicionesDeContrato Condiciones { get; private set; }

    /// <summary>Obtiene la fecha de alta del contrato.</summary>
    /// <value>Fecha de inicio de la relación.</value>
    public DateOnly FechaAlta { get; private set; }

    /// <summary>Obtiene la fecha de baja.</summary>
    /// <value><c>null</c> mientras el contrato siga vigente.</value>
    public DateOnly? FechaBaja { get; private set; }

    /// <summary>Obtiene el instante de la última modificación.</summary>
    /// <value>Instante en UTC.</value>
    public DateTimeOffset FechaModificacion { get; private set; }

    /// <summary>Indica si el contrato no tiene fecha de baja.</summary>
    /// <value><c>true</c> mientras no se dé de baja.</value>
    public bool Activo => FechaBaja is null;

    /// <summary>
    /// Crea un contrato.
    /// </summary>
    /// <param name="empleadoId">Empleado.</param>
    /// <param name="empresaId">Empresa cliente.</param>
    /// <param name="razonSocialId">Razón social pagadora.</param>
    /// <param name="esquema">Esquema de pago.</param>
    /// <param name="numeroTrabajador">Número de trabajador, si aplica.</param>
    /// <param name="puesto">Puesto.</param>
    /// <param name="departamento">Departamento o campaña.</param>
    /// <param name="tipoDeContrato">Tipo de contrato.</param>
    /// <param name="condiciones">Condiciones económicas.</param>
    /// <param name="fechaAlta">Fecha de alta.</param>
    /// <param name="momento">Instante de creación, en UTC.</param>
    /// <returns>El contrato creado y vigente.</returns>
    /// <exception cref="CatalogoInvalidoException">Se lanza si el esquema o las condiciones no son válidos.</exception>
    public static Contrato Crear(
        Guid empleadoId,
        Guid empresaId,
        Guid razonSocialId,
        EsquemaDePago esquema,
        string? numeroTrabajador,
        string? puesto,
        string? departamento,
        string? tipoDeContrato,
        CondicionesDeContrato condiciones,
        DateOnly fechaAlta,
        DateTimeOffset momento)
    {
        Validar(esquema, condiciones);

        return new Contrato(
            Guid.CreateVersion7(),
            empleadoId,
            empresaId,
            razonSocialId,
            esquema,
            Limpiar(numeroTrabajador),
            Limpiar(puesto),
            Limpiar(departamento),
            Limpiar(tipoDeContrato),
            condiciones,
            fechaAlta,
            fechaBaja: null,
            momento);
    }

    /// <summary>
    /// Reconstruye un contrato a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="empleadoId">Empleado.</param>
    /// <param name="empresaId">Empresa.</param>
    /// <param name="razonSocialId">Razón social.</param>
    /// <param name="esquema">Esquema.</param>
    /// <param name="numeroTrabajador">Número de trabajador.</param>
    /// <param name="puesto">Puesto.</param>
    /// <param name="departamento">Departamento.</param>
    /// <param name="tipoDeContrato">Tipo de contrato.</param>
    /// <param name="condiciones">Condiciones económicas.</param>
    /// <param name="fechaAlta">Fecha de alta.</param>
    /// <param name="fechaBaja">Fecha de baja.</param>
    /// <param name="fechaModificacion">Última modificación.</param>
    /// <returns>La entidad rehidratada.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static Contrato Rehidratar(
        Guid id,
        Guid empleadoId,
        Guid empresaId,
        Guid razonSocialId,
        EsquemaDePago esquema,
        string? numeroTrabajador,
        string? puesto,
        string? departamento,
        string? tipoDeContrato,
        CondicionesDeContrato condiciones,
        DateOnly fechaAlta,
        DateOnly? fechaBaja,
        DateTimeOffset fechaModificacion)
        => new(id, empleadoId, empresaId, razonSocialId, esquema, numeroTrabajador, puesto, departamento,
            tipoDeContrato, condiciones, fechaAlta, fechaBaja, fechaModificacion);

    /// <summary>
    /// Actualiza el contrato.
    /// </summary>
    /// <param name="razonSocialId">Razón social pagadora.</param>
    /// <param name="esquema">Esquema de pago.</param>
    /// <param name="numeroTrabajador">Número de trabajador.</param>
    /// <param name="puesto">Puesto.</param>
    /// <param name="departamento">Departamento o campaña.</param>
    /// <param name="tipoDeContrato">Tipo de contrato.</param>
    /// <param name="condiciones">Condiciones económicas.</param>
    /// <param name="fechaAlta">Fecha de alta.</param>
    /// <param name="fechaBaja">Fecha de baja, o <c>null</c> para mantenerlo vigente.</param>
    /// <param name="momento">Instante de la modificación, en UTC.</param>
    /// <exception cref="CatalogoInvalidoException">Se lanza si los datos no son válidos.</exception>
    public void Actualizar(
        Guid razonSocialId,
        EsquemaDePago esquema,
        string? numeroTrabajador,
        string? puesto,
        string? departamento,
        string? tipoDeContrato,
        CondicionesDeContrato condiciones,
        DateOnly fechaAlta,
        DateOnly? fechaBaja,
        DateTimeOffset momento)
    {
        Validar(esquema, condiciones);

        if (fechaBaja is not null && fechaBaja.Value < fechaAlta)
        {
            throw new CatalogoInvalidoException("La fecha de baja no puede ser anterior a la de alta.");
        }

        RazonSocialId = razonSocialId;
        Esquema = esquema;
        NumeroTrabajador = Limpiar(numeroTrabajador);
        Puesto = Limpiar(puesto);
        Departamento = Limpiar(departamento);
        TipoDeContrato = Limpiar(tipoDeContrato);
        Condiciones = condiciones;
        FechaAlta = fechaAlta;
        FechaBaja = fechaBaja;
        FechaModificacion = momento;
    }

    /// <summary>
    /// Calcula la antigüedad en años completos a una fecha.
    /// </summary>
    /// <param name="fecha">Fecha de referencia.</param>
    /// <returns>Años completos desde la fecha de alta; cero si la fecha es anterior al alta.</returns>
    public int AntiguedadEnAnios(DateOnly fecha)
    {
        if (fecha < FechaAlta)
        {
            return 0;
        }

        int anios = fecha.Year - FechaAlta.Year;

        if (fecha.Month < FechaAlta.Month || (fecha.Month == FechaAlta.Month && fecha.Day < FechaAlta.Day))
        {
            anios--;
        }

        return anios;
    }

    /// <summary>
    /// Comprueba que el esquema y las condiciones del contrato sean coherentes.
    /// </summary>
    /// <param name="esquema">Esquema de pago.</param>
    /// <param name="condiciones">Salarios, zona y datos de INFONAVIT del contrato.</param>
    /// <exception cref="ArgumentNullException">Se lanza si faltan las condiciones o sus datos de INFONAVIT.</exception>
    /// <exception cref="CatalogoInvalidoException">
    /// Se lanza si falta el esquema o la zona, algún salario es negativo o la
    /// pensión alimenticia no es una fracción entre 0 y 1.
    /// </exception>
    private static void Validar(EsquemaDePago esquema, CondicionesDeContrato condiciones)
    {
        ArgumentNullException.ThrowIfNull(condiciones);
        ArgumentNullException.ThrowIfNull(condiciones.Infonavit);

        if (esquema == EsquemaDePago.NoEspecificado || !Enum.IsDefined(esquema))
        {
            throw new CatalogoInvalidoException("Debe indicarse el esquema de pago del contrato.");
        }

        if (condiciones.Zona == ZonaSalarioMinimo.NoEspecificado)
        {
            throw new CatalogoInvalidoException("Debe indicarse la zona de salario mínimo del contrato.");
        }

        if (condiciones.SueldoPeriodoReal < 0 || condiciones.SalarioDiarioFiscal < 0 || condiciones.SalarioDiarioIntegrado < 0)
        {
            throw new CatalogoInvalidoException("Los salarios no pueden ser negativos.");
        }

        if (condiciones.PensionAlimenticiaPorcentaje is < 0 or > 1)
        {
            throw new CatalogoInvalidoException("El porcentaje de pensión alimenticia debe expresarse como fracción entre 0 y 1.");
        }
    }

    /// <summary>Normaliza un texto opcional.</summary>
    /// <param name="valor">Texto capturado.</param>
    /// <returns>El texto sin espacios en los extremos, o <c>null</c> si está vacío.</returns>
    private static string? Limpiar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
