using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Configuración de operación de una razón social: banderas que gobiernan
/// el cálculo de todos sus trabajadores.
/// </summary>
/// <param name="TipoDeServicio">Nómina o Maquila.</param>
/// <param name="SubsidioAbsorbido">Si la empresa absorbe el subsidio al empleo en el complemento sindical.</param>
/// <param name="AplicaFaltasProporcionales">Si las faltas se castigan con el factor del séptimo día.</param>
/// <param name="ModalidadDeComision">Base de la comisión al cliente.</param>
/// <param name="PorcentajeComision">Porcentaje de comisión, como fracción.</param>
/// <param name="ZonaIsn">Regla para la tasa del ISN.</param>
/// <param name="TasaIva">Tasa de IVA de la factura, como fracción; cero si no aplica.</param>
/// <param name="PorcentajeOtrosCostos">Porcentaje de otros costos sobre el neto pagado, como fracción.</param>
/// <param name="PrimaDeRiesgo">Prima de riesgo de trabajo, como fracción; <c>null</c> para usar el parámetro general.</param>
public sealed record ConfiguracionDeRazonSocial(
    TipoDeServicio TipoDeServicio,
    bool SubsidioAbsorbido,
    bool AplicaFaltasProporcionales,
    ModalidadDeComision ModalidadDeComision,
    decimal PorcentajeComision,
    ZonaIsn ZonaIsn,
    decimal TasaIva,
    decimal PorcentajeOtrosCostos,
    decimal? PrimaDeRiesgo);

/// <summary>
/// Entidad pagadora (razón social, sindicato, cooperativa o prestador de
/// honorarios) que pertenece a una empresa cliente y con la que se contrata a
/// los trabajadores.
/// </summary>
/// <remarks>
/// El modelo de referencia calcula la nómina "por bloque de razón social": cada
/// bloque tiene su registro patronal, su prima de riesgo y sus banderas de
/// operación. Un empleado puede tener contratos con varias razones sociales de
/// la misma empresa (sueldo mixto).
/// </remarks>
public sealed class RazonSocial
{
    private RazonSocial(
        Guid id,
        Guid empresaId,
        string nombre,
        string rfc,
        string? registroPatronal,
        ZonaSalarioMinimo zona,
        ConfiguracionDeRazonSocial configuracion,
        string? bancoDispersor,
        bool activa,
        DateTimeOffset fechaAlta)
    {
        Id = id;
        EmpresaId = empresaId;
        Nombre = nombre;
        Rfc = rfc;
        RegistroPatronal = registroPatronal;
        Zona = zona;
        Configuracion = configuracion;
        BancoDispersor = bancoDispersor;
        Activa = activa;
        FechaAlta = fechaAlta;
    }

    /// <summary>Obtiene el identificador único.</summary>
    /// <value>Clave primaria.</value>
    public Guid Id { get; }

    /// <summary>Obtiene la empresa cliente propietaria.</summary>
    /// <value>Discriminante de aislamiento.</value>
    public Guid EmpresaId { get; }

    /// <summary>Obtiene el nombre o razón social.</summary>
    /// <value>Texto mostrado en la interfaz y en los reportes.</value>
    public string Nombre { get; private set; }

    /// <summary>Obtiene el RFC.</summary>
    /// <value>Dato sensible: nunca debe escribirse en registros de log.</value>
    public string Rfc { get; private set; }

    /// <summary>Obtiene el registro patronal ante el IMSS.</summary>
    /// <value><c>null</c> para sindicatos, cooperativas o prestadores sin registro.</value>
    public string? RegistroPatronal { get; private set; }

    /// <summary>Obtiene la zona de salario mínimo predeterminada de sus trabajadores.</summary>
    /// <value>Cada contrato puede sobrescribirla.</value>
    public ZonaSalarioMinimo Zona { get; private set; }

    /// <summary>Obtiene las banderas de operación.</summary>
    /// <value>Configuración que alimenta las variables de la razón social en las fórmulas.</value>
    public ConfiguracionDeRazonSocial Configuracion { get; private set; }

    /// <summary>Obtiene el banco desde el que se dispersa la nómina.</summary>
    /// <value>Texto informativo, o <c>null</c>.</value>
    public string? BancoDispersor { get; private set; }

    /// <summary>Indica si la razón social está activa.</summary>
    /// <value><c>false</c> si fue dada de baja.</value>
    public bool Activa { get; private set; }

    /// <summary>Obtiene la fecha de alta.</summary>
    /// <value>Instante en UTC.</value>
    public DateTimeOffset FechaAlta { get; }

    /// <summary>
    /// Da de alta una razón social.
    /// </summary>
    /// <param name="empresaId">Empresa cliente propietaria.</param>
    /// <param name="nombre">Nombre o razón social.</param>
    /// <param name="rfc">RFC.</param>
    /// <param name="registroPatronal">Registro patronal, si aplica.</param>
    /// <param name="zona">Zona de salario mínimo predeterminada.</param>
    /// <param name="configuracion">Banderas de operación.</param>
    /// <param name="bancoDispersor">Banco dispersor, si se conoce.</param>
    /// <param name="momento">Instante del alta, en UTC.</param>
    /// <returns>La razón social creada y activa.</returns>
    /// <exception cref="CatalogoInvalidoException">Se lanza si faltan datos obligatorios.</exception>
    public static RazonSocial Crear(
        Guid empresaId,
        string nombre,
        string rfc,
        string? registroPatronal,
        ZonaSalarioMinimo zona,
        ConfiguracionDeRazonSocial configuracion,
        string? bancoDispersor,
        DateTimeOffset momento)
    {
        Validar(nombre, rfc, zona, configuracion);

        return new RazonSocial(
            Guid.CreateVersion7(),
            empresaId,
            nombre.Trim(),
            rfc.Trim().ToUpperInvariant(),
            Limpiar(registroPatronal),
            zona,
            configuracion,
            Limpiar(bancoDispersor),
            activa: true,
            momento);
    }

    /// <summary>
    /// Reconstruye una razón social a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="empresaId">Empresa propietaria.</param>
    /// <param name="nombre">Nombre.</param>
    /// <param name="rfc">RFC.</param>
    /// <param name="registroPatronal">Registro patronal.</param>
    /// <param name="zona">Zona de salario mínimo.</param>
    /// <param name="configuracion">Banderas de operación.</param>
    /// <param name="bancoDispersor">Banco dispersor.</param>
    /// <param name="activa">Estado.</param>
    /// <param name="fechaAlta">Fecha de alta.</param>
    /// <returns>La entidad rehidratada.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static RazonSocial Rehidratar(
        Guid id,
        Guid empresaId,
        string nombre,
        string rfc,
        string? registroPatronal,
        ZonaSalarioMinimo zona,
        ConfiguracionDeRazonSocial configuracion,
        string? bancoDispersor,
        bool activa,
        DateTimeOffset fechaAlta)
        => new(id, empresaId, nombre, rfc, registroPatronal, zona, configuracion, bancoDispersor, activa, fechaAlta);

    /// <summary>
    /// Actualiza los datos de la razón social.
    /// </summary>
    /// <param name="nombre">Nombre o razón social.</param>
    /// <param name="rfc">RFC.</param>
    /// <param name="registroPatronal">Registro patronal, si aplica.</param>
    /// <param name="zona">Zona de salario mínimo predeterminada.</param>
    /// <param name="configuracion">Banderas de operación.</param>
    /// <param name="bancoDispersor">Banco dispersor.</param>
    /// <param name="activa">Estado.</param>
    /// <exception cref="CatalogoInvalidoException">Se lanza si faltan datos obligatorios.</exception>
    public void Actualizar(
        string nombre,
        string rfc,
        string? registroPatronal,
        ZonaSalarioMinimo zona,
        ConfiguracionDeRazonSocial configuracion,
        string? bancoDispersor,
        bool activa)
    {
        Validar(nombre, rfc, zona, configuracion);

        Nombre = nombre.Trim();
        Rfc = rfc.Trim().ToUpperInvariant();
        RegistroPatronal = Limpiar(registroPatronal);
        Zona = zona;
        Configuracion = configuracion;
        BancoDispersor = Limpiar(bancoDispersor);
        Activa = activa;
    }

    private static void Validar(string nombre, string rfc, ZonaSalarioMinimo zona, ConfiguracionDeRazonSocial configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new CatalogoInvalidoException("El nombre de la razón social es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(rfc))
        {
            throw new CatalogoInvalidoException("El RFC de la razón social es obligatorio.");
        }

        if (zona == ZonaSalarioMinimo.NoEspecificado)
        {
            throw new CatalogoInvalidoException("Debe indicarse la zona de salario mínimo.");
        }

        if (configuracion.TipoDeServicio == TipoDeServicio.NoEspecificado)
        {
            throw new CatalogoInvalidoException("Debe indicarse el tipo de servicio (Nómina o Maquila).");
        }

        if (configuracion.ModalidadDeComision == ModalidadDeComision.NoEspecificado)
        {
            throw new CatalogoInvalidoException("Debe indicarse la modalidad de comisión.");
        }

        if (configuracion.PorcentajeComision is < 0 or > 1
            || configuracion.TasaIva is < 0 or > 1
            || configuracion.PorcentajeOtrosCostos is < 0 or > 1
            || configuracion.PrimaDeRiesgo is < 0 or > 1)
        {
            throw new CatalogoInvalidoException("Los porcentajes deben expresarse como fracción entre 0 y 1.");
        }
    }

    private static string? Limpiar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
