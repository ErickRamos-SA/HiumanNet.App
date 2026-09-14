using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Datos personales de un empleado.
/// </summary>
/// <param name="Nombre">Nombre o nombres.</param>
/// <param name="ApellidoPaterno">Primer apellido.</param>
/// <param name="ApellidoMaterno">Segundo apellido, o <c>null</c>.</param>
/// <param name="Rfc">RFC, o <c>null</c> si aún no se captura.</param>
/// <param name="Curp">CURP, o <c>null</c>.</param>
/// <param name="Nss">Número de seguridad social, o <c>null</c>.</param>
/// <param name="FechaNacimiento">Fecha de nacimiento, o <c>null</c>.</param>
/// <param name="Correo">Correo personal, o <c>null</c>.</param>
/// <param name="Telefono">Teléfono, o <c>null</c>.</param>
/// <remarks>RFC, CURP y NSS son datos sensibles: nunca deben escribirse en registros de log.</remarks>
public sealed record DatosPersonales(
    string Nombre,
    string ApellidoPaterno,
    string? ApellidoMaterno,
    string? Rfc,
    string? Curp,
    string? Nss,
    DateOnly? FechaNacimiento,
    string? Correo,
    string? Telefono);

/// <summary>
/// Persona que trabaja para una empresa cliente. Puede tener uno o varios
/// contratos, cada uno con una razón social y un esquema de pago.
/// </summary>
/// <remarks>
/// El empleado pertenece a la empresa cliente (unidad de aislamiento). Sus
/// contratos determinan con qué razones sociales y bajo qué esquema se le
/// paga; el motor calcula cada contrato por separado y el sistema consolida
/// el total por persona.
/// </remarks>
public sealed class Empleado
{
    /// <summary>Longitud máxima de la clave del empleado.</summary>
    public const int LongitudMaximaClave = 20;

    private Empleado(
        Guid id,
        Guid empresaId,
        string clave,
        DatosPersonales datos,
        bool activo,
        DateTimeOffset fechaAlta)
    {
        Id = id;
        EmpresaId = empresaId;
        Clave = clave;
        Datos = datos;
        Activo = activo;
        FechaAlta = fechaAlta;
    }

    /// <summary>Obtiene el identificador único.</summary>
    /// <value>Clave primaria.</value>
    public Guid Id { get; }

    /// <summary>Obtiene la empresa cliente a la que pertenece.</summary>
    /// <value>Discriminante de aislamiento.</value>
    public Guid EmpresaId { get; }

    /// <summary>Obtiene la clave del empleado dentro de la empresa.</summary>
    /// <value>Identificador que usan los archivos de incidencias y de resultados para referirse al trabajador.</value>
    public string Clave { get; private set; }

    /// <summary>Obtiene los datos personales.</summary>
    /// <value>Objeto de valor inmutable.</value>
    public DatosPersonales Datos { get; private set; }

    /// <summary>Indica si el empleado está activo.</summary>
    /// <value><c>false</c> si fue dado de baja de la empresa.</value>
    public bool Activo { get; private set; }

    /// <summary>Obtiene la fecha de alta en el sistema.</summary>
    /// <value>Instante en UTC.</value>
    public DateTimeOffset FechaAlta { get; }

    /// <summary>Obtiene el nombre completo para mostrar.</summary>
    /// <value>Nombre y apellidos separados por espacio.</value>
    public string NombreCompleto => string.IsNullOrWhiteSpace(Datos.ApellidoMaterno)
        ? $"{Datos.Nombre} {Datos.ApellidoPaterno}"
        : $"{Datos.Nombre} {Datos.ApellidoPaterno} {Datos.ApellidoMaterno}";

    /// <summary>
    /// Da de alta un empleado.
    /// </summary>
    /// <param name="empresaId">Empresa cliente.</param>
    /// <param name="clave">Clave del empleado dentro de la empresa.</param>
    /// <param name="datos">Datos personales.</param>
    /// <param name="momento">Instante del alta, en UTC.</param>
    /// <returns>El empleado creado y activo.</returns>
    /// <exception cref="CatalogoInvalidoException">Se lanza si la clave o los datos obligatorios están vacíos.</exception>
    public static Empleado Crear(Guid empresaId, string clave, DatosPersonales datos, DateTimeOffset momento)
        => new(Guid.CreateVersion7(), empresaId, NormalizarClave(clave), Normalizar(datos), activo: true, momento);

    /// <summary>
    /// Reconstruye un empleado a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="empresaId">Empresa.</param>
    /// <param name="clave">Clave.</param>
    /// <param name="datos">Datos personales.</param>
    /// <param name="activo">Estado.</param>
    /// <param name="fechaAlta">Fecha de alta.</param>
    /// <returns>La entidad rehidratada.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static Empleado Rehidratar(
        Guid id, Guid empresaId, string clave, DatosPersonales datos, bool activo, DateTimeOffset fechaAlta)
        => new(id, empresaId, clave, datos, activo, fechaAlta);

    /// <summary>
    /// Actualiza la clave y los datos personales.
    /// </summary>
    /// <param name="clave">Clave del empleado.</param>
    /// <param name="datos">Datos personales.</param>
    /// <param name="activo">Estado.</param>
    /// <exception cref="CatalogoInvalidoException">Se lanza si la clave o los datos obligatorios están vacíos.</exception>
    public void Actualizar(string clave, DatosPersonales datos, bool activo)
    {
        Clave = NormalizarClave(clave);
        Datos = Normalizar(datos);
        Activo = activo;
    }

    /// <summary>
    /// Normaliza la clave del empleado.
    /// </summary>
    /// <param name="clave">Clave aportada.</param>
    /// <returns>La clave recortada.</returns>
    /// <exception cref="CatalogoInvalidoException">Se lanza si la clave está vacía o es demasiado larga.</exception>
    public static string NormalizarClave(string? clave)
    {
        if (string.IsNullOrWhiteSpace(clave))
        {
            throw new CatalogoInvalidoException("La clave del empleado es obligatoria.");
        }

        string limpia = clave.Trim();

        return limpia.Length > LongitudMaximaClave
            ? throw new CatalogoInvalidoException($"La clave del empleado no puede exceder {LongitudMaximaClave} caracteres.")
            : limpia;
    }

    private static DatosPersonales Normalizar(DatosPersonales datos)
    {
        ArgumentNullException.ThrowIfNull(datos);

        if (string.IsNullOrWhiteSpace(datos.Nombre) || string.IsNullOrWhiteSpace(datos.ApellidoPaterno))
        {
            throw new CatalogoInvalidoException("El nombre y el primer apellido del empleado son obligatorios.");
        }

        return datos with
        {
            Nombre = datos.Nombre.Trim(),
            ApellidoPaterno = datos.ApellidoPaterno.Trim(),
            ApellidoMaterno = Limpiar(datos.ApellidoMaterno),
            Rfc = Limpiar(datos.Rfc)?.ToUpperInvariant(),
            Curp = Limpiar(datos.Curp)?.ToUpperInvariant(),
            Nss = Limpiar(datos.Nss),
            Correo = Limpiar(datos.Correo)?.ToLowerInvariant(),
            Telefono = Limpiar(datos.Telefono),
        };
    }

    private static string? Limpiar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
