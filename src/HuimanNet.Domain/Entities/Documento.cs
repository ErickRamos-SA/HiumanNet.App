using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.ValueObjects;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Documento intercambiado a través del portal. Es la raíz de agregado central
/// de la versión 1: gobierna su propio ciclo de vida, desde la solicitud de
/// carga hasta que queda disponible o en cuarentena.
/// </summary>
/// <remarks>
/// El contenido binario nunca atraviesa el servidor: esta entidad sólo mantiene
/// los metadatos y el <see cref="RutaBlob"/> generado por el sistema. El cliente
/// sube y descarga directo contra Blob Storage con una URL SAS de mínimo
/// privilegio (ARQUITECTURA.md §3.3).
/// </remarks>
public sealed class Documento
{
    /// <summary>
    /// Inicializa una instancia con valores ya validados. Sólo la usan las
    /// fábricas y <see cref="Rehidratar"/>.
    /// </summary>
    /// <inheritdoc cref="Rehidratar" path="/param"/>
    private Documento(
        Guid id,
        Guid empresaId,
        Guid periodoId,
        TipoDocumento tipo,
        NombreArchivo nombreOriginal,
        string rutaBlob,
        TamanoArchivo tamano,
        EstadoDocumento estado,
        HuellaArchivo huella,
        Guid cargadoPorUsuarioId,
        DateTimeOffset fechaSolicitud,
        DateTimeOffset? fechaCargaConfirmada,
        DateTimeOffset? fechaEscaneo,
        string? motivoCuarentena)
    {
        Id = id;
        EmpresaId = empresaId;
        PeriodoId = periodoId;
        Tipo = tipo;
        NombreOriginal = nombreOriginal;
        RutaBlob = rutaBlob;
        Tamano = tamano;
        Estado = estado;
        Huella = huella;
        CargadoPorUsuarioId = cargadoPorUsuarioId;
        FechaSolicitud = fechaSolicitud;
        FechaCargaConfirmada = fechaCargaConfirmada;
        FechaEscaneo = fechaEscaneo;
        MotivoCuarentena = motivoCuarentena;
    }

    /// <summary>
    /// Obtiene el identificador único del documento.
    /// </summary>
    /// <value>GUID versión 7: ordenable por tiempo, útil como clave agrupada en SQL Server.</value>
    public Guid Id { get; }

    /// <summary>
    /// Obtiene la empresa propietaria del documento.
    /// </summary>
    /// <value>Discriminante de aislamiento: toda consulta debe filtrar por este valor.</value>
    public Guid EmpresaId { get; }

    /// <summary>
    /// Obtiene el período al que pertenece el documento.
    /// </summary>
    /// <value>Identificador del <see cref="PeriodoCarga"/> asociado.</value>
    public Guid PeriodoId { get; }

    /// <summary>
    /// Obtiene el tipo funcional del documento.
    /// </summary>
    /// <value>Determina la dirección del flujo y quién puede cargarlo.</value>
    public TipoDocumento Tipo { get; }

    /// <summary>
    /// Obtiene el nombre original aportado por el usuario.
    /// </summary>
    /// <value>Metadato de presentación; no interviene en la ruta de almacenamiento.</value>
    public NombreArchivo NombreOriginal { get; }

    /// <summary>
    /// Obtiene la ruta del blob generada por el sistema.
    /// </summary>
    /// <value>
    /// Formato <c>{empresaId}/{periodoId}/{tipo}/{guid}{extensión}</c>. Al basarse
    /// en identificadores generados por el servidor, elimina el riesgo de
    /// <i>path traversal</i> y de colisiones de nombre.
    /// </value>
    public string RutaBlob { get; }

    /// <summary>
    /// Obtiene el tamaño del archivo.
    /// </summary>
    /// <value>Declarado al solicitar la carga y reemplazado por el real al confirmarla.</value>
    public TamanoArchivo Tamano { get; private set; }

    /// <summary>
    /// Obtiene el estado del documento en su ciclo de vida.
    /// </summary>
    /// <value>Sólo <see cref="EstadoDocumento.Disponible"/> habilita la descarga.</value>
    public EstadoDocumento Estado { get; private set; }

    /// <summary>
    /// Obtiene la huella criptográfica del contenido.
    /// </summary>
    /// <value>Vacía hasta que se confirma la carga.</value>
    public HuellaArchivo Huella { get; private set; }

    /// <summary>
    /// Obtiene el usuario que solicitó la carga.
    /// </summary>
    /// <value>Identificador local del <see cref="Usuario"/> responsable.</value>
    public Guid CargadoPorUsuarioId { get; }

    /// <summary>
    /// Obtiene el instante en que se solicitó la carga y se emitió el SAS de escritura.
    /// </summary>
    /// <value>Instante en UTC.</value>
    public DateTimeOffset FechaSolicitud { get; }

    /// <summary>
    /// Obtiene el instante en que el cliente confirmó que terminó de subir el archivo.
    /// </summary>
    /// <value>Instante en UTC, o <c>null</c> si la carga no se ha confirmado.</value>
    public DateTimeOffset? FechaCargaConfirmada { get; private set; }

    /// <summary>
    /// Obtiene el instante en que se registró el veredicto del escaneo de malware.
    /// </summary>
    /// <value>Instante en UTC, o <c>null</c> si aún no hay veredicto.</value>
    public DateTimeOffset? FechaEscaneo { get; private set; }

    /// <summary>
    /// Obtiene el motivo por el que el documento fue puesto en cuarentena.
    /// </summary>
    /// <value>Descripción del veredicto del antimalware, o <c>null</c> si no aplica.</value>
    public string? MotivoCuarentena { get; private set; }

    /// <summary>
    /// Indica si el documento puede descargarse.
    /// </summary>
    /// <value><c>true</c> únicamente en estado <see cref="EstadoDocumento.Disponible"/>.</value>
    public bool EsDescargable => Estado == EstadoDocumento.Disponible;

    /// <summary>
    /// Indica si el documento lo aporta la empresa cliente.
    /// </summary>
    /// <value><c>true</c> para incidencias y datos de empleados.</value>
    public bool EsAportadoPorCliente
        => Tipo is TipoDocumento.Incidencia or TipoDocumento.DatosEmpleado;

    /// <summary>
    /// Registra la solicitud de carga de un documento y reserva su ruta en Blob Storage.
    /// </summary>
    /// <param name="empresaId">Empresa propietaria, tomada del token del solicitante.</param>
    /// <param name="periodoId">Período al que se asocia el documento.</param>
    /// <param name="tipo">Tipo funcional del documento.</param>
    /// <param name="nombreOriginal">Nombre de archivo aportado por el usuario.</param>
    /// <param name="tamano">Tamaño declarado del archivo.</param>
    /// <param name="cargadoPorUsuarioId">Usuario que solicita la carga.</param>
    /// <param name="momento">Instante de la solicitud, en UTC.</param>
    /// <returns>El documento en estado <see cref="EstadoDocumento.Pendiente"/>.</returns>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si el tipo de documento no está especificado.
    /// </exception>
    public static Documento Solicitar(
        Guid empresaId,
        Guid periodoId,
        TipoDocumento tipo,
        NombreArchivo nombreOriginal,
        TamanoArchivo tamano,
        Guid cargadoPorUsuarioId,
        DateTimeOffset momento)
    {
        if (tipo == TipoDocumento.NoEspecificado)
        {
            throw new DocumentoInvalidoException("Debe indicarse el tipo de documento.");
        }

        Guid id = Guid.CreateVersion7();
        string rutaBlob = ConstruirRutaBlob(empresaId, periodoId, tipo, id, nombreOriginal.Extension);

        return new Documento(
            id,
            empresaId,
            periodoId,
            tipo,
            nombreOriginal,
            rutaBlob,
            tamano,
            EstadoDocumento.Pendiente,
            huella: default,
            cargadoPorUsuarioId,
            momento,
            fechaCargaConfirmada: null,
            fechaEscaneo: null,
            motivoCuarentena: null);
    }

    /// <summary>
    /// Reconstruye un documento a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador único.</param>
    /// <param name="empresaId">Empresa propietaria.</param>
    /// <param name="periodoId">Período asociado.</param>
    /// <param name="tipo">Tipo funcional.</param>
    /// <param name="nombreOriginal">Nombre original del archivo.</param>
    /// <param name="rutaBlob">Ruta del blob generada por el sistema.</param>
    /// <param name="tamano">Tamaño del archivo.</param>
    /// <param name="estado">Estado del ciclo de vida.</param>
    /// <param name="huella">Huella criptográfica, si ya se calculó.</param>
    /// <param name="cargadoPorUsuarioId">Usuario que cargó el documento.</param>
    /// <param name="fechaSolicitud">Instante de la solicitud, en UTC.</param>
    /// <param name="fechaCargaConfirmada">Instante de confirmación de carga, si existe.</param>
    /// <param name="fechaEscaneo">Instante del veredicto de escaneo, si existe.</param>
    /// <param name="motivoCuarentena">Motivo de cuarentena, si aplica.</param>
    /// <returns>La entidad rehidratada, sin ejecutar reglas de transición.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static Documento Rehidratar(
        Guid id,
        Guid empresaId,
        Guid periodoId,
        TipoDocumento tipo,
        NombreArchivo nombreOriginal,
        string rutaBlob,
        TamanoArchivo tamano,
        EstadoDocumento estado,
        HuellaArchivo huella,
        Guid cargadoPorUsuarioId,
        DateTimeOffset fechaSolicitud,
        DateTimeOffset? fechaCargaConfirmada,
        DateTimeOffset? fechaEscaneo,
        string? motivoCuarentena)
        => new(
            id, empresaId, periodoId, tipo, nombreOriginal, rutaBlob, tamano, estado, huella,
            cargadoPorUsuarioId, fechaSolicitud, fechaCargaConfirmada, fechaEscaneo, motivoCuarentena);

    /// <summary>
    /// Confirma que el archivo terminó de subirse y lo deja a la espera del escaneo.
    /// </summary>
    /// <param name="tamanoReal">Tamaño real reportado por Blob Storage.</param>
    /// <param name="huella">Huella criptográfica del contenido cargado.</param>
    /// <param name="momento">Instante de la confirmación, en UTC.</param>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si el documento no está en estado <see cref="EstadoDocumento.Pendiente"/>.
    /// </exception>
    public void ConfirmarCarga(TamanoArchivo tamanoReal, HuellaArchivo huella, DateTimeOffset momento)
    {
        if (Estado != EstadoDocumento.Pendiente)
        {
            throw new DocumentoInvalidoException(
                $"Sólo puede confirmarse la carga de un documento pendiente; su estado es '{Estado}'.");
        }

        Tamano = tamanoReal;
        Huella = huella;
        Estado = EstadoDocumento.Escaneando;
        FechaCargaConfirmada = momento;
    }

    /// <summary>
    /// Registra un veredicto de escaneo limpio y habilita la descarga.
    /// </summary>
    /// <param name="momento">Instante del veredicto, en UTC.</param>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si el documento no está en estado <see cref="EstadoDocumento.Escaneando"/>.
    /// </exception>
    public void MarcarDisponible(DateTimeOffset momento)
    {
        if (Estado != EstadoDocumento.Escaneando)
        {
            throw new DocumentoInvalidoException(
                $"Sólo un documento en escaneo puede marcarse como disponible; su estado es '{Estado}'.");
        }

        Estado = EstadoDocumento.Disponible;
        FechaEscaneo = momento;
        MotivoCuarentena = null;
    }

    /// <summary>
    /// Registra un veredicto de escaneo positivo en malware y bloquea la descarga.
    /// </summary>
    /// <param name="motivo">Descripción del hallazgo del antimalware.</param>
    /// <param name="momento">Instante del veredicto, en UTC.</param>
    /// <exception cref="ArgumentException">Se lanza si <paramref name="motivo"/> está vacío.</exception>
    public void MarcarEnCuarentena(string motivo, DateTimeOffset momento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);

        Estado = EstadoDocumento.EnCuarentena;
        FechaEscaneo = momento;
        MotivoCuarentena = motivo.Trim();
    }

    /// <summary>
    /// Comprueba que el documento está en condiciones de descargarse.
    /// </summary>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si el documento aún no superó el escaneo de malware o está en cuarentena.
    /// </exception>
    public void GarantizarQueEsDescargable()
    {
        if (!EsDescargable)
        {
            throw new DocumentoInvalidoException(
                $"El documento no está disponible para descarga; su estado es '{Estado}'.");
        }
    }

    /// <summary>
    /// Genera la ruta del blob con identificadores del sistema, nunca con el
    /// nombre que sube el usuario.
    /// </summary>
    /// <param name="empresaId">Empresa propietaria; es el primer segmento, para aislar por empresa.</param>
    /// <param name="periodoId">Período de carga.</param>
    /// <param name="tipo">Tipo de documento.</param>
    /// <param name="documentoId">Identificador del documento.</param>
    /// <param name="extension">Extensión del archivo original, con el punto.</param>
    /// <returns>La ruta relativa dentro del contenedor.</returns>
    private static string ConstruirRutaBlob(
        Guid empresaId, Guid periodoId, TipoDocumento tipo, Guid documentoId, string extension)
        => $"{empresaId:N}/{periodoId:N}/{tipo.ToString().ToLowerInvariant()}/{documentoId:N}{extension}";
}
