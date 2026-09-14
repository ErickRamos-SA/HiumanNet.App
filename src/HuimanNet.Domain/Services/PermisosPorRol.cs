using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;

namespace HuimanNet.Domain.Services;

/// <summary>
/// Acciones que cada rol concede por defecto, antes de aplicar los permisos
/// personalizados del usuario, y las que ningún permiso puede darle a un rol.
/// </summary>
/// <remarks>
/// Es la única matriz de permisos del sistema: la API, la web y la app móvil
/// la consultan a través de <see cref="PoliticaDeAcceso"/>. El administrador
/// puede ampliarla o recortarla por usuario, pero no editarla en bloque: un
/// cambio de política de roles es una decisión de producto y se hace aquí.
/// <para>
/// La separación de funciones es un tope, no una sugerencia: procesar la
/// nómina (calcular, reprocesar, cotejar, aprobar), publicar resultados,
/// gestionar períodos y administrar el sistema son acciones exclusivas de
/// nómina y administración. Un usuario de empresa cliente aporta archivos e
/// incidencias de su empresa y consulta los cálculos en solo lectura; ningún
/// permiso personalizado lo cambia.
/// </para>
/// </remarks>
public static class PermisosPorRol
{
    private static readonly AccionDelSistema[] DeClienteEmpresa =
    [
        AccionDelSistema.CargarDocumentos,
        AccionDelSistema.DescargarDocumentos,
        AccionDelSistema.CapturarIncidencias,
        AccionDelSistema.ConsultarNomina,
        AccionDelSistema.ConsultarBitacora,
    ];

    private static readonly AccionDelSistema[] DeOperadorNomina =
    [
        AccionDelSistema.DescargarDocumentos,
        AccionDelSistema.PublicarResultados,
        AccionDelSistema.GestionarPeriodos,
        AccionDelSistema.CapturarIncidencias,
        AccionDelSistema.CalcularNomina,
        AccionDelSistema.ConsultarNomina,
        AccionDelSistema.CotejarNomina,
        AccionDelSistema.AprobarNomina,
        AccionDelSistema.AdministrarEmpleados,
        AccionDelSistema.ConsultarBitacora,
        AccionDelSistema.ConsultarExplicacionDeCalculos,
    ];

    private static readonly AccionDelSistema[] DeAdministrador =
        [.. Enum.GetValues<AccionDelSistema>().Where(static a => a != AccionDelSistema.NoEspecificado)];

    /// <summary>
    /// Acciones reservadas a los roles transversales (nómina y administración).
    /// </summary>
    private static readonly HashSet<AccionDelSistema> ExclusivasDeRolesTransversales =
    [
        AccionDelSistema.PublicarResultados,
        AccionDelSistema.GestionarPeriodos,
        AccionDelSistema.CalcularNomina,
        AccionDelSistema.CotejarNomina,
        AccionDelSistema.AprobarNomina,
        AccionDelSistema.AdministrarEmpresas,
        AccionDelSistema.AdministrarUsuarios,
        AccionDelSistema.AdministrarCatalogosDeCalculo,
    ];

    /// <summary>
    /// Obtiene las acciones que un rol concede por defecto.
    /// </summary>
    /// <param name="rol">Rol consultado.</param>
    /// <returns>Lista de sólo lectura; vacía para un rol no especificado.</returns>
    public static IReadOnlyList<AccionDelSistema> Predeterminadas(RolUsuario rol) => rol switch
    {
        RolUsuario.ClienteEmpresa => DeClienteEmpresa,
        RolUsuario.OperadorNomina => DeOperadorNomina,
        RolUsuario.Administrador => DeAdministrador,
        _ => [],
    };

    /// <summary>
    /// Indica si una acción puede concederse a un rol, por omisión o con un
    /// permiso personalizado.
    /// </summary>
    /// <param name="rol">Rol del usuario.</param>
    /// <param name="accion">Acción consultada.</param>
    /// <returns>
    /// <c>false</c> para las acciones exclusivas de nómina y administración
    /// cuando el rol es de empresa cliente; <c>true</c> en el resto de los casos.
    /// </returns>
    public static bool PuedeConcederse(RolUsuario rol, AccionDelSistema accion)
        => accion != AccionDelSistema.NoEspecificado && rol switch
        {
            RolUsuario.OperadorNomina or RolUsuario.Administrador => true,
            RolUsuario.ClienteEmpresa => !ExclusivasDeRolesTransversales.Contains(accion),
            _ => false,
        };

    /// <summary>
    /// Calcula el conjunto efectivo de acciones de un usuario.
    /// </summary>
    /// <param name="rol">Rol del usuario.</param>
    /// <param name="personalizados">Permisos personalizados que ajustan los del rol.</param>
    /// <returns>Conjunto de acciones habilitadas.</returns>
    public static IReadOnlySet<AccionDelSistema> Efectivas(
        RolUsuario rol, IEnumerable<PermisoDeUsuario>? personalizados)
    {
        var efectivas = new HashSet<AccionDelSistema>(Predeterminadas(rol));

        if (personalizados is null)
        {
            return efectivas;
        }

        foreach (PermisoDeUsuario permiso in personalizados)
        {
            if (permiso.Habilitado)
            {
                // Un permiso guardado antes de existir el tope, o insertado a mano
                // en la base de datos, tampoco lo supera.
                if (PuedeConcederse(rol, permiso.Accion))
                {
                    efectivas.Add(permiso.Accion);
                }
            }
            else
            {
                efectivas.Remove(permiso.Accion);
            }
        }

        return efectivas;
    }
}
