namespace HuimanNet.Domain.Entities;

/// <summary>
/// Empresa cliente cuya nómina opera HuimanNet. Es la unidad de aislamiento del
/// portal: ningún dato cruza la frontera de una empresa.
/// </summary>
/// <remarks>
/// Toda consulta a documentos, períodos y bitácora debe filtrar por
/// <see cref="Id"/> tomado del token del solicitante, nunca de la petición.
/// </remarks>
public sealed class Empresa
{
    private Empresa(
        Guid id,
        string razonSocial,
        string identificadorFiscal,
        string prefijoContenedor,
        bool activa,
        DateTimeOffset fechaAlta)
    {
        Id = id;
        RazonSocial = razonSocial;
        IdentificadorFiscal = identificadorFiscal;
        PrefijoContenedor = prefijoContenedor;
        Activa = activa;
        FechaAlta = fechaAlta;
    }

    /// <summary>
    /// Obtiene el identificador único de la empresa.
    /// </summary>
    /// <value>Clave primaria y discriminante de aislamiento entre empresas.</value>
    public Guid Id { get; }

    /// <summary>
    /// Obtiene la razón social de la empresa.
    /// </summary>
    /// <value>Nombre legal mostrado en la interfaz.</value>
    public string RazonSocial { get; private set; }

    /// <summary>
    /// Obtiene el identificador fiscal de la empresa.
    /// </summary>
    /// <value>Dato sensible: nunca debe escribirse en los registros de log.</value>
    public string IdentificadorFiscal { get; private set; }

    /// <summary>
    /// Obtiene el prefijo con el que se nombran los blobs de la empresa.
    /// </summary>
    /// <value>Segmento inicial de la ruta en Blob Storage, derivado del <see cref="Id"/>.</value>
    public string PrefijoContenedor { get; }

    /// <summary>
    /// Obtiene un valor que indica si la empresa puede operar en el portal.
    /// </summary>
    /// <value><c>true</c> si está activa; <c>false</c> si fue dada de baja.</value>
    public bool Activa { get; private set; }

    /// <summary>
    /// Obtiene la fecha de alta de la empresa en el sistema.
    /// </summary>
    /// <value>Instante en UTC.</value>
    public DateTimeOffset FechaAlta { get; }

    /// <summary>
    /// Da de alta una nueva empresa cliente.
    /// </summary>
    /// <param name="razonSocial">Razón social de la empresa.</param>
    /// <param name="identificadorFiscal">Identificador fiscal (RFC, NIT, CUIT, según país).</param>
    /// <param name="momento">Instante del alta, en UTC.</param>
    /// <returns>La empresa recién creada, activa por defecto.</returns>
    /// <exception cref="ArgumentException">
    /// Se lanza si la razón social o el identificador fiscal están vacíos.
    /// </exception>
    public static Empresa Crear(string razonSocial, string identificadorFiscal, DateTimeOffset momento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(razonSocial);
        ArgumentException.ThrowIfNullOrWhiteSpace(identificadorFiscal);

        Guid id = Guid.CreateVersion7();

        return new Empresa(
            id,
            razonSocial.Trim(),
            identificadorFiscal.Trim().ToUpperInvariant(),
            $"emp-{id:N}",
            activa: true,
            momento);
    }

    /// <summary>
    /// Reconstruye una empresa a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador único.</param>
    /// <param name="razonSocial">Razón social.</param>
    /// <param name="identificadorFiscal">Identificador fiscal.</param>
    /// <param name="prefijoContenedor">Prefijo de blobs.</param>
    /// <param name="activa">Estado de actividad.</param>
    /// <param name="fechaAlta">Fecha de alta en UTC.</param>
    /// <returns>La entidad rehidratada, sin ejecutar reglas de creación.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static Empresa Rehidratar(
        Guid id,
        string razonSocial,
        string identificadorFiscal,
        string prefijoContenedor,
        bool activa,
        DateTimeOffset fechaAlta)
        => new(id, razonSocial, identificadorFiscal, prefijoContenedor, activa, fechaAlta);

    /// <summary>
    /// Actualiza los datos administrativos de la empresa.
    /// </summary>
    /// <param name="razonSocial">Nueva razón social.</param>
    /// <param name="identificadorFiscal">Nuevo identificador fiscal.</param>
    /// <exception cref="ArgumentException">
    /// Se lanza si alguno de los valores está vacío.
    /// </exception>
    public void ActualizarDatos(string razonSocial, string identificadorFiscal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(razonSocial);
        ArgumentException.ThrowIfNullOrWhiteSpace(identificadorFiscal);

        RazonSocial = razonSocial.Trim();
        IdentificadorFiscal = identificadorFiscal.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Desactiva la empresa: sus usuarios dejan de poder operar en el portal.
    /// </summary>
    /// <remarks>Los documentos se conservan por la política de retención legal.</remarks>
    public void Desactivar() => Activa = false;

    /// <summary>
    /// Reactiva una empresa previamente dada de baja.
    /// </summary>
    public void Activar() => Activa = true;
}
