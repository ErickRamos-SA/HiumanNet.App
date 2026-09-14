using HuimanNet.Domain.Enums;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Permiso personalizado de un usuario: habilita o deshabilita una acción con
/// independencia de lo que su rol concede por defecto.
/// </summary>
/// <param name="Accion">Acción afectada.</param>
/// <param name="Habilitado"><c>true</c> para conceder; <c>false</c> para retirar.</param>
public sealed record PermisoDeUsuario(AccionDelSistema Accion, bool Habilitado);

/// <summary>
/// Usuario del portal. Se autentica con Microsoft Entra o, cuando la
/// instalación opera en modo local, con usuario y contraseña gestionados por el
/// administrador.
/// </summary>
/// <remarks>
/// En modo Entra esta entidad es la proyección local de la identidad externa;
/// en modo local es la identidad misma, con la contraseña almacenada como
/// <i>hash</i> (nunca en claro). En ambos modos el rol, las empresas y los
/// permisos personalizados los decide el administrador desde el portal.
/// <para>
/// Un usuario de empresa cliente pertenece a una <b>empresa principal</b>, con
/// la que entra por omisión, y puede tener <b>empresas adicionales</b>: sólo
/// ve y opera esas empresas. El operador de nómina y el administrador no
/// pertenecen a ninguna porque operan sobre todas.
/// </para>
/// </remarks>
public sealed class Usuario
{
    private readonly List<PermisoDeUsuario> _permisos;
    private readonly List<Guid> _empresasAdicionales;

    private Usuario(
        Guid id,
        string identificadorExterno,
        string nombreCompleto,
        string correo,
        RolUsuario rol,
        Guid? empresaId,
        bool activo,
        DateTimeOffset fechaAlta,
        string? hashContrasena,
        bool requiereCambioDeContrasena,
        Idioma idioma,
        IEnumerable<PermisoDeUsuario> permisos,
        IEnumerable<Guid>? empresasAdicionales)
    {
        Id = id;
        IdentificadorExterno = identificadorExterno;
        NombreCompleto = nombreCompleto;
        Correo = correo;
        Rol = rol;
        EmpresaId = empresaId;
        Activo = activo;
        FechaAlta = fechaAlta;
        HashContrasena = hashContrasena;
        RequiereCambioDeContrasena = requiereCambioDeContrasena;
        Idioma = idioma;
        _permisos = [.. permisos];
        _empresasAdicionales = Normalizar(rol, empresaId, empresasAdicionales);
    }

    /// <summary>Prefijo del identificador externo de los usuarios creados en modo local.</summary>
    public const string PrefijoLocal = "local:";

    /// <summary>
    /// Obtiene el identificador único del usuario dentro de HuimanNet.
    /// </summary>
    /// <value>Clave primaria local; no coincide con el identificador de Entra.</value>
    public Guid Id { get; }

    /// <summary>
    /// Obtiene el identificador del sujeto en el proveedor de identidad.
    /// </summary>
    /// <value>
    /// Valor del <i>claim</i> <c>oid</c> o <c>sub</c> en modo Entra; en modo local,
    /// <c>local:</c> seguido del correo.
    /// </value>
    public string IdentificadorExterno { get; private set; }

    /// <summary>Obtiene el nombre completo del usuario.</summary>
    /// <value>Texto mostrado en la interfaz y en la bitácora.</value>
    public string NombreCompleto { get; private set; }

    /// <summary>Obtiene la dirección de correo del usuario.</summary>
    /// <value>Destino de las notificaciones y nombre de inicio de sesión en modo local.</value>
    public string Correo { get; private set; }

    /// <summary>Obtiene el rol funcional del usuario.</summary>
    /// <value>Determina las acciones concedidas por defecto.</value>
    public RolUsuario Rol { get; private set; }

    /// <summary>Obtiene la empresa principal del usuario.</summary>
    /// <value>
    /// Obligatoria para <see cref="RolUsuario.ClienteEmpresa"/>: es la empresa con
    /// la que entra por omisión. <c>null</c> para el operador de nómina y el
    /// administrador, que operan sobre todas las empresas.
    /// </value>
    public Guid? EmpresaId { get; private set; }

    /// <summary>Obtiene las otras empresas a las que tiene acceso un usuario de empresa cliente.</summary>
    /// <value>Sin repetir la principal; vacía para los roles transversales.</value>
    public IReadOnlyList<Guid> EmpresasAdicionales => _empresasAdicionales;

    /// <summary>Obtiene todas las empresas en las que opera un usuario de empresa cliente.</summary>
    /// <value>
    /// La principal primero y después las adicionales; vacía para los roles
    /// transversales, que operan sobre todas.
    /// </value>
    public IReadOnlyList<Guid> Empresas => EmpresaId is { } principal ? [principal, .. _empresasAdicionales] : [];

    /// <summary>Indica si el usuario puede operar en el portal.</summary>
    /// <value><c>true</c> si está activo; en caso contrario, <c>false</c>.</value>
    public bool Activo { get; private set; }

    /// <summary>Obtiene la fecha de alta del usuario.</summary>
    /// <value>Instante en UTC.</value>
    public DateTimeOffset FechaAlta { get; }

    /// <summary>Obtiene el <i>hash</i> de la contraseña local.</summary>
    /// <value><c>null</c> para usuarios que sólo se autentican con Entra.</value>
    public string? HashContrasena { get; private set; }

    /// <summary>Indica si el usuario debe cambiar su contraseña en el próximo inicio de sesión.</summary>
    /// <value><c>true</c> tras un alta o un restablecimiento por el administrador.</value>
    public bool RequiereCambioDeContrasena { get; private set; }

    /// <summary>Obtiene el idioma preferido de la interfaz.</summary>
    /// <value>Español por defecto.</value>
    public Idioma Idioma { get; private set; }

    /// <summary>Obtiene los permisos personalizados que ajustan los del rol.</summary>
    /// <value>Lista de sólo lectura; vacía si el usuario usa exactamente los permisos de su rol.</value>
    public IReadOnlyList<PermisoDeUsuario> Permisos => _permisos;

    /// <summary>Indica si el usuario puede iniciar sesión con contraseña.</summary>
    /// <value><c>true</c> cuando tiene un <i>hash</i> de contraseña almacenado.</value>
    public bool TieneContrasenaLocal => !string.IsNullOrEmpty(HashContrasena);

    /// <summary>
    /// Da de alta un usuario en el portal.
    /// </summary>
    /// <param name="identificadorExterno">Identificador del sujeto en el proveedor de identidad.</param>
    /// <param name="nombreCompleto">Nombre para mostrar.</param>
    /// <param name="correo">Dirección de correo.</param>
    /// <param name="rol">Rol funcional asignado.</param>
    /// <param name="empresaId">Empresa principal, si el rol lo exige.</param>
    /// <param name="momento">Instante del alta, en UTC.</param>
    /// <param name="idioma">Idioma preferido.</param>
    /// <returns>El usuario recién creado, activo por defecto y sin empresas adicionales.</returns>
    /// <exception cref="ArgumentException">Se lanza si algún texto obligatorio está vacío.</exception>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si un usuario de rol <see cref="RolUsuario.ClienteEmpresa"/> no indica empresa.
    /// </exception>
    public static Usuario Crear(
        string identificadorExterno,
        string nombreCompleto,
        string correo,
        RolUsuario rol,
        Guid? empresaId,
        DateTimeOffset momento,
        Idioma idioma = Idioma.Espanol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identificadorExterno);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreCompleto);
        ArgumentException.ThrowIfNullOrWhiteSpace(correo);
        ValidarRolYEmpresa(rol, empresaId);

        return new Usuario(
            Guid.CreateVersion7(),
            identificadorExterno.Trim(),
            nombreCompleto.Trim(),
            correo.Trim().ToLowerInvariant(),
            rol,
            rol == RolUsuario.ClienteEmpresa ? empresaId : null,
            activo: true,
            momento,
            hashContrasena: null,
            requiereCambioDeContrasena: false,
            idioma,
            [],
            empresasAdicionales: null);
    }

    /// <summary>
    /// Da de alta un usuario que se autentica con contraseña local.
    /// </summary>
    /// <param name="nombreCompleto">Nombre para mostrar.</param>
    /// <param name="correo">Correo, que actúa como nombre de inicio de sesión.</param>
    /// <param name="rol">Rol funcional.</param>
    /// <param name="empresaId">Empresa principal, si el rol lo exige.</param>
    /// <param name="hashContrasena">Hash de la contraseña inicial.</param>
    /// <param name="momento">Instante del alta, en UTC.</param>
    /// <param name="idioma">Idioma preferido.</param>
    /// <returns>El usuario, marcado para cambiar la contraseña en su primer acceso.</returns>
    public static Usuario CrearLocal(
        string nombreCompleto,
        string correo,
        RolUsuario rol,
        Guid? empresaId,
        string hashContrasena,
        DateTimeOffset momento,
        Idioma idioma = Idioma.Espanol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correo);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashContrasena);

        Usuario usuario = Crear(
            PrefijoLocal + correo.Trim().ToLowerInvariant(), nombreCompleto, correo, rol, empresaId, momento, idioma);

        usuario.HashContrasena = hashContrasena;
        usuario.RequiereCambioDeContrasena = true;
        return usuario;
    }

    /// <summary>
    /// Reconstruye un usuario a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador local.</param>
    /// <param name="identificadorExterno">Identificador en el proveedor de identidad.</param>
    /// <param name="nombreCompleto">Nombre para mostrar.</param>
    /// <param name="correo">Correo.</param>
    /// <param name="rol">Rol funcional.</param>
    /// <param name="empresaId">Empresa principal, si aplica.</param>
    /// <param name="activo">Estado de actividad.</param>
    /// <param name="fechaAlta">Fecha de alta en UTC.</param>
    /// <param name="hashContrasena">Hash de contraseña local, si existe.</param>
    /// <param name="requiereCambioDeContrasena">Si debe cambiar la contraseña.</param>
    /// <param name="idioma">Idioma preferido.</param>
    /// <param name="permisos">Permisos personalizados.</param>
    /// <param name="empresasAdicionales">Empresas adicionales, o <c>null</c> si no tiene.</param>
    /// <returns>La entidad rehidratada, sin ejecutar reglas de creación.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static Usuario Rehidratar(
        Guid id,
        string identificadorExterno,
        string nombreCompleto,
        string correo,
        RolUsuario rol,
        Guid? empresaId,
        bool activo,
        DateTimeOffset fechaAlta,
        string? hashContrasena,
        bool requiereCambioDeContrasena,
        Idioma idioma,
        IEnumerable<PermisoDeUsuario> permisos,
        IEnumerable<Guid>? empresasAdicionales = null)
        => new(id, identificadorExterno, nombreCompleto, correo, rol, empresaId, activo, fechaAlta,
            hashContrasena, requiereCambioDeContrasena, idioma, permisos, empresasAdicionales);

    /// <summary>
    /// Indica si el usuario pertenece a una empresa, como principal o adicional.
    /// </summary>
    /// <param name="empresaId">Empresa consultada.</param>
    /// <returns><c>true</c> si la empresa es una de las suyas; siempre <c>false</c> para los roles transversales.</returns>
    public bool PerteneceA(Guid empresaId) => EmpresaId == empresaId || _empresasAdicionales.Contains(empresaId);

    /// <summary>
    /// Actualiza el perfil del usuario con los datos más recientes.
    /// </summary>
    /// <param name="nombreCompleto">Nombre para mostrar.</param>
    /// <param name="correo">Dirección de correo.</param>
    /// <exception cref="ArgumentException">Se lanza si algún valor está vacío.</exception>
    public void ActualizarPerfil(string nombreCompleto, string correo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreCompleto);
        ArgumentException.ThrowIfNullOrWhiteSpace(correo);

        NombreCompleto = nombreCompleto.Trim();
        Correo = correo.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Cambia el rol funcional y la empresa principal del usuario.
    /// </summary>
    /// <param name="rol">Nuevo rol.</param>
    /// <param name="empresaId">Empresa principal, obligatoria para el rol de empresa cliente.</param>
    /// <exception cref="InvalidOperationException">
    /// Se lanza al asignar <see cref="RolUsuario.ClienteEmpresa"/> sin empresa.
    /// </exception>
    /// <remarks>
    /// Un rol transversal pierde sus empresas; la empresa cliente conserva las
    /// adicionales que no coincidan con la nueva principal.
    /// </remarks>
    public void AsignarRol(RolUsuario rol, Guid? empresaId)
    {
        ValidarRolYEmpresa(rol, empresaId);
        Rol = rol;
        EmpresaId = rol == RolUsuario.ClienteEmpresa ? empresaId : null;
        AsignarEmpresasAdicionales([.. _empresasAdicionales]);
    }

    /// <summary>
    /// Cambia el rol funcional del usuario conservando su empresa.
    /// </summary>
    /// <param name="rol">Nuevo rol.</param>
    /// <exception cref="InvalidOperationException">
    /// Se lanza al asignar <see cref="RolUsuario.ClienteEmpresa"/> a un usuario sin empresa.
    /// </exception>
    public void CambiarRol(RolUsuario rol) => AsignarRol(rol, EmpresaId);

    /// <summary>
    /// Sustituye las empresas adicionales de un usuario de empresa cliente.
    /// </summary>
    /// <param name="empresas">Empresas, además de la principal, en las que opera.</param>
    /// <remarks>
    /// Se descartan los duplicados y la propia empresa principal. Para los roles
    /// transversales la lista queda vacía: ya operan sobre todas las empresas.
    /// </remarks>
    public void AsignarEmpresasAdicionales(IEnumerable<Guid> empresas)
    {
        ArgumentNullException.ThrowIfNull(empresas);

        List<Guid> limpias = Normalizar(Rol, EmpresaId, empresas);
        _empresasAdicionales.Clear();
        _empresasAdicionales.AddRange(limpias);
    }

    /// <summary>
    /// Establece o sustituye la contraseña local.
    /// </summary>
    /// <param name="hashContrasena">Hash de la nueva contraseña.</param>
    /// <param name="requiereCambio"><c>true</c> si el usuario debe cambiarla en su próximo acceso.</param>
    /// <exception cref="ArgumentException">Se lanza si el hash está vacío.</exception>
    public void EstablecerContrasena(string hashContrasena, bool requiereCambio)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashContrasena);
        HashContrasena = hashContrasena;
        RequiereCambioDeContrasena = requiereCambio;
    }

    /// <summary>
    /// Enlaza el usuario pre-aprovisionado por el administrador con su identidad
    /// real en el proveedor externo, la primera vez que inicia sesión.
    /// </summary>
    /// <param name="identificadorExterno">Valor del <i>claim</i> <c>oid</c> o <c>sub</c>.</param>
    /// <exception cref="ArgumentException">Se lanza si el identificador está vacío.</exception>
    public void VincularIdentidadExterna(string identificadorExterno)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identificadorExterno);
        IdentificadorExterno = identificadorExterno.Trim();
    }

    /// <summary>
    /// Cambia el idioma preferido.
    /// </summary>
    /// <param name="idioma">Nuevo idioma.</param>
    public void CambiarIdioma(Idioma idioma) => Idioma = idioma;

    /// <summary>
    /// Sustituye los permisos personalizados del usuario.
    /// </summary>
    /// <param name="permisos">Nuevos permisos; una entrada por acción.</param>
    public void ReemplazarPermisos(IEnumerable<PermisoDeUsuario> permisos)
    {
        ArgumentNullException.ThrowIfNull(permisos);

        _permisos.Clear();

        foreach (PermisoDeUsuario permiso in permisos)
        {
            if (permiso.Accion == AccionDelSistema.NoEspecificado)
            {
                continue;
            }

            _permisos.RemoveAll(p => p.Accion == permiso.Accion);
            _permisos.Add(permiso);
        }
    }

    /// <summary>
    /// Desactiva el usuario, impidiendo cualquier operación posterior.
    /// </summary>
    public void Desactivar() => Activo = false;

    /// <summary>
    /// Reactiva un usuario desactivado.
    /// </summary>
    public void Activar() => Activo = true;

    private static List<Guid> Normalizar(RolUsuario rol, Guid? principal, IEnumerable<Guid>? empresas)
        => rol != RolUsuario.ClienteEmpresa || empresas is null
            ? []
            : [.. empresas.Where(e => e != Guid.Empty && e != principal).Distinct()];

    private static void ValidarRolYEmpresa(RolUsuario rol, Guid? empresaId)
    {
        if (rol == RolUsuario.NoEspecificado || !Enum.IsDefined(rol))
        {
            throw new InvalidOperationException("Debe indicarse un rol válido para el usuario.");
        }

        if (rol == RolUsuario.ClienteEmpresa && empresaId is null)
        {
            throw new InvalidOperationException(
                "Un usuario de empresa cliente debe estar vinculado a una empresa.");
        }
    }
}
