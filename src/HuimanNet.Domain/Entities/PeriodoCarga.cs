using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.ValueObjects;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Período de intercambio de documentos de una empresa cliente. Agrupa los
/// documentos de un ciclo de nómina y gobierna qué operaciones se permiten en
/// cada momento.
/// </summary>
/// <remarks>
/// Las transiciones de estado sólo avanzan; consulte <see cref="EstadoPeriodo"/>.
/// </remarks>
public sealed class PeriodoCarga
{
    private PeriodoCarga(
        Guid id,
        Guid empresaId,
        PeriodoCalendario calendario,
        string descripcion,
        EstadoPeriodo estado,
        DateTimeOffset fechaApertura,
        DateTimeOffset? fechaLimiteCarga,
        DateTimeOffset? fechaCierre)
    {
        Id = id;
        EmpresaId = empresaId;
        Calendario = calendario;
        Descripcion = descripcion;
        Estado = estado;
        FechaApertura = fechaApertura;
        FechaLimiteCarga = fechaLimiteCarga;
        FechaCierre = fechaCierre;
    }

    /// <summary>
    /// Obtiene el identificador único del período.
    /// </summary>
    /// <value>Clave primaria del período.</value>
    public Guid Id { get; }

    /// <summary>
    /// Obtiene la empresa propietaria del período.
    /// </summary>
    /// <value>Discriminante de aislamiento: filtra toda consulta de documentos.</value>
    public Guid EmpresaId { get; }

    /// <summary>
    /// Obtiene la posición del período en el calendario de nómina.
    /// </summary>
    /// <value>Año, mes y consecutivo dentro del mes.</value>
    public PeriodoCalendario Calendario { get; }

    /// <summary>
    /// Obtiene la descripción legible del período.
    /// </summary>
    /// <value>Por ejemplo <c>"Segunda quincena de agosto 2026"</c>.</value>
    public string Descripcion { get; private set; }

    /// <summary>
    /// Obtiene el estado del ciclo de intercambio.
    /// </summary>
    /// <value>Estado vigente; determina qué cargas y descargas se permiten.</value>
    public EstadoPeriodo Estado { get; private set; }

    /// <summary>
    /// Obtiene el instante de apertura del período.
    /// </summary>
    /// <value>Instante en UTC.</value>
    public DateTimeOffset FechaApertura { get; }

    /// <summary>
    /// Obtiene la fecha límite para que el cliente cargue documentos.
    /// </summary>
    /// <value>Instante en UTC, o <c>null</c> si no se fijó un límite.</value>
    public DateTimeOffset? FechaLimiteCarga { get; private set; }

    /// <summary>
    /// Obtiene el instante de cierre del período.
    /// </summary>
    /// <value>Instante en UTC, o <c>null</c> mientras el período siga abierto.</value>
    public DateTimeOffset? FechaCierre { get; private set; }

    /// <summary>
    /// Indica si el período admite cargas del rol de empresa cliente.
    /// </summary>
    /// <value><c>true</c> en los estados <c>Abierto</c> y <c>Recibido</c>.</value>
    public bool AdmiteCargaDelCliente
        => Estado is EstadoPeriodo.Abierto or EstadoPeriodo.Recibido;

    /// <summary>
    /// Indica si el período admite la publicación de resultados y ajustes por el operador.
    /// </summary>
    /// <value><c>true</c> en los estados <c>EnProceso</c> y <c>ResultadosDisponibles</c>.</value>
    public bool AdmiteCargaDelOperador
        => Estado is EstadoPeriodo.EnProceso or EstadoPeriodo.ResultadosDisponibles;

    /// <summary>
    /// Indica si el período admite calcular o reprocesar la nómina.
    /// </summary>
    /// <value>
    /// <c>true</c> desde que hay al menos un archivo disponible de la empresa
    /// (<c>Recibido</c>) hasta que el período se cierra.
    /// </value>
    public bool AdmiteCalculo
        => Estado is EstadoPeriodo.Recibido or EstadoPeriodo.EnProceso or EstadoPeriodo.ResultadosDisponibles;

    /// <summary>
    /// Abre un nuevo período de carga para una empresa.
    /// </summary>
    /// <param name="empresaId">Empresa propietaria del período.</param>
    /// <param name="calendario">Posición del período en el calendario.</param>
    /// <param name="descripcion">Descripción legible.</param>
    /// <param name="momento">Instante de apertura, en UTC.</param>
    /// <param name="fechaLimiteCarga">Fecha límite opcional para las cargas del cliente.</param>
    /// <returns>El período recién abierto.</returns>
    /// <exception cref="ArgumentException">Se lanza si la descripción está vacía.</exception>
    public static PeriodoCarga Abrir(
        Guid empresaId,
        PeriodoCalendario calendario,
        string descripcion,
        DateTimeOffset momento,
        DateTimeOffset? fechaLimiteCarga = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descripcion);

        return new PeriodoCarga(
            Guid.CreateVersion7(),
            empresaId,
            calendario,
            descripcion.Trim(),
            EstadoPeriodo.Abierto,
            momento,
            fechaLimiteCarga,
            fechaCierre: null);
    }

    /// <summary>
    /// Reconstruye un período a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador único.</param>
    /// <param name="empresaId">Empresa propietaria.</param>
    /// <param name="calendario">Posición en el calendario.</param>
    /// <param name="descripcion">Descripción legible.</param>
    /// <param name="estado">Estado del ciclo.</param>
    /// <param name="fechaApertura">Instante de apertura en UTC.</param>
    /// <param name="fechaLimiteCarga">Fecha límite de carga, si existe.</param>
    /// <param name="fechaCierre">Instante de cierre, si existe.</param>
    /// <returns>La entidad rehidratada, sin ejecutar reglas de transición.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static PeriodoCarga Rehidratar(
        Guid id,
        Guid empresaId,
        PeriodoCalendario calendario,
        string descripcion,
        EstadoPeriodo estado,
        DateTimeOffset fechaApertura,
        DateTimeOffset? fechaLimiteCarga,
        DateTimeOffset? fechaCierre)
        => new(id, empresaId, calendario, descripcion, estado, fechaApertura, fechaLimiteCarga, fechaCierre);

    /// <summary>
    /// Ajusta la fecha límite de carga del cliente.
    /// </summary>
    /// <param name="fechaLimite">Nueva fecha límite en UTC, o <c>null</c> para retirarla.</param>
    public void FijarFechaLimite(DateTimeOffset? fechaLimite) => FechaLimiteCarga = fechaLimite;

    /// <summary>
    /// Registra que llegó el primer documento del cliente y avanza a <c>Recibido</c>.
    /// </summary>
    /// <remarks>Es idempotente: si el período ya avanzó más allá, no hace nada.</remarks>
    public void MarcarRecepcion()
    {
        if (Estado == EstadoPeriodo.Abierto)
        {
            Estado = EstadoPeriodo.Recibido;
        }
    }

    /// <summary>
    /// Avanza el período al estado indicado, validando que la transición sea legal.
    /// </summary>
    /// <param name="nuevoEstado">Estado destino.</param>
    /// <param name="momento">Instante de la transición, en UTC.</param>
    /// <exception cref="PeriodoCerradoException">
    /// Se lanza si la transición implica retroceder o si el período ya está cerrado.
    /// </exception>
    public void CambiarEstado(EstadoPeriodo nuevoEstado, DateTimeOffset momento)
    {
        if (nuevoEstado == EstadoPeriodo.NoEspecificado)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nuevoEstado), "No se puede transicionar a un estado no especificado.");
        }

        if (nuevoEstado <= Estado)
        {
            throw new PeriodoCerradoException(Id, Estado);
        }

        Estado = nuevoEstado;

        if (nuevoEstado == EstadoPeriodo.Cerrado)
        {
            FechaCierre = momento;
        }
    }

    /// <summary>
    /// Comprueba que el período admite calcular o reprocesar la nómina.
    /// </summary>
    /// <exception cref="NominaInvalidaException">
    /// Se lanza si el período aún no tiene archivos de la empresa o si ya está cerrado.
    /// </exception>
    /// <remarks>
    /// El cálculo es una acción manual posterior a la carga de los archivos del
    /// período. Si la empresa captura sus incidencias en el sistema sin enviar
    /// archivos, nómina puede avanzar el período a <c>EnProceso</c> para habilitarlo.
    /// </remarks>
    public void GarantizarQueAdmiteCalculo()
    {
        if (AdmiteCalculo)
        {
            return;
        }

        throw new NominaInvalidaException(Estado == EstadoPeriodo.Cerrado
            ? "El período está cerrado y no admite nuevos cálculos."
            : "El período todavía no tiene archivos de la empresa: el cálculo se habilita cuando llega el primer archivo del período o cuando nómina inicia el proceso.");
    }

    /// <summary>
    /// Comprueba que el período admite una carga del tipo de documento indicado.
    /// </summary>
    /// <param name="tipo">Tipo de documento que se pretende cargar.</param>
    /// <exception cref="PeriodoCerradoException">
    /// Se lanza si el estado actual no permite cargar ese tipo de documento.
    /// </exception>
    public void GarantizarQueAdmiteCarga(TipoDocumento tipo)
    {
        bool admitido = tipo switch
        {
            TipoDocumento.Incidencia or TipoDocumento.DatosEmpleado => AdmiteCargaDelCliente,
            TipoDocumento.Resultado or TipoDocumento.Ajuste => AdmiteCargaDelOperador,
            _ => false,
        };

        if (!admitido)
        {
            throw new PeriodoCerradoException(Id, Estado);
        }
    }
}
