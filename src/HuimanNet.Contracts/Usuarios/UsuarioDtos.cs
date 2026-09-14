using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Usuarios;

/// <summary>
/// Permiso personalizado de un usuario.
/// </summary>
/// <param name="Accion">Acción.</param>
/// <param name="Habilitado">Si se concede o se retira.</param>
public sealed record PermisoDto(AccionDelSistema Accion, bool Habilitado);

/// <summary>
/// Proyección de lectura de un usuario para la administración.
/// </summary>
/// <param name="Id">Identificador local.</param>
/// <param name="NombreCompleto">Nombre.</param>
/// <param name="Correo">Correo.</param>
/// <param name="Rol">Rol.</param>
/// <param name="EmpresaId">Empresa, si aplica.</param>
/// <param name="EmpresaRazonSocial">Razón social de la empresa, si aplica.</param>
/// <param name="Activo">Estado.</param>
/// <param name="Idioma">Idioma preferido.</param>
/// <param name="TieneContrasenaLocal">Si puede iniciar sesión con contraseña.</param>
/// <param name="RequiereCambioDeContrasena">Si debe cambiar la contraseña.</param>
/// <param name="FechaAlta">Fecha de alta, en UTC.</param>
/// <param name="Permisos">Permisos personalizados.</param>
/// <param name="AccionesEfectivas">Acciones habilitadas tras combinar rol y permisos.</param>
/// <param name="EmpresasAdicionales">Otras empresas, además de la principal, a las que tiene acceso un usuario de empresa cliente.</param>
public sealed record UsuarioDto(
    Guid Id,
    string NombreCompleto,
    string Correo,
    RolUsuario Rol,
    Guid? EmpresaId,
    string? EmpresaRazonSocial,
    bool Activo,
    Idioma Idioma,
    bool TieneContrasenaLocal,
    bool RequiereCambioDeContrasena,
    DateTimeOffset FechaAlta,
    IReadOnlyList<PermisoDto> Permisos,
    IReadOnlyList<AccionDelSistema> AccionesEfectivas,
    IReadOnlyList<Guid> EmpresasAdicionales);

/// <summary>
/// Petición para crear o actualizar un usuario.
/// </summary>
/// <param name="NombreCompleto">Nombre.</param>
/// <param name="Correo">Correo; nombre de inicio de sesión en modo local.</param>
/// <param name="Rol">Rol.</param>
/// <param name="EmpresaId">Empresa principal, obligatoria para el rol de empresa cliente.</param>
/// <param name="Activo">Estado.</param>
/// <param name="Idioma">Idioma preferido.</param>
/// <param name="ContrasenaInicial">Contraseña inicial en modo local; se ignora al actualizar.</param>
/// <param name="Permisos">Permisos personalizados.</param>
/// <param name="EmpresasAdicionales">
/// Otras empresas a las que tiene acceso un usuario de empresa cliente; se
/// ignoran para los roles transversales, que operan sobre todas.
/// </param>
public sealed record GuardarUsuarioRequest(
    string NombreCompleto,
    string Correo,
    RolUsuario Rol,
    Guid? EmpresaId,
    bool Activo,
    Idioma Idioma,
    string? ContrasenaInicial,
    IReadOnlyList<PermisoDto> Permisos,
    IReadOnlyList<Guid>? EmpresasAdicionales = null);

/// <summary>
/// Petición del administrador para restablecer la contraseña de un usuario.
/// </summary>
/// <param name="NuevaContrasena">Contraseña temporal; el usuario deberá cambiarla.</param>
public sealed record RestablecerContrasenaRequest(string NuevaContrasena);

/// <summary>
/// Indicadores del panel de inicio.
/// </summary>
/// <param name="EmpleadosActivos">Empleados activos en el ámbito del usuario.</param>
/// <param name="PeriodosAbiertos">Períodos que admiten cargas.</param>
/// <param name="DocumentosDisponibles">Documentos descargables en períodos no cerrados.</param>
/// <param name="CorridasPorCotejar">Corridas calculadas y aún no cotejadas.</param>
/// <param name="Pendientes">Tareas sugeridas para el usuario.</param>
public sealed record ResumenDeInicioDto(
    int EmpleadosActivos,
    int PeriodosAbiertos,
    int DocumentosDisponibles,
    int CorridasPorCotejar,
    IReadOnlyList<PendienteDto> Pendientes);

/// <summary>
/// Tarea sugerida en el panel de inicio.
/// </summary>
/// <param name="Clave">Clave estable para localizar el texto en el cliente.</param>
/// <param name="Detalle">Detalle específico (por ejemplo, el nombre del período).</param>
/// <param name="Ruta">Ruta de la pantalla relacionada.</param>
public sealed record PendienteDto(string Clave, string Detalle, string Ruta);
