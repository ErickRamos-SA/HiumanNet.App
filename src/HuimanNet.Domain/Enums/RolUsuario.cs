namespace HuimanNet.Domain.Enums;

/// <summary>
/// Rol funcional de un usuario dentro del portal.
/// </summary>
/// <remarks>
/// Lo asigna el administrador desde el portal y se guarda en la base de datos,
/// que es la fuente de verdad: el token (Entra o local) sólo autentica.
/// Determina qué operaciones y qué tipos de documento puede manipular el
/// usuario; consulte <see cref="Services.PoliticaDeAcceso"/> y
/// <see cref="Services.PermisosPorRol"/>.
/// </remarks>
public enum RolUsuario
{
    /// <summary>Valor no especificado. Nunca debe persistirse.</summary>
    NoEspecificado = 0,

    /// <summary>Usuario de una empresa cliente. Sube incidencias y datos de empleados; descarga resultados de <b>su</b> empresa.</summary>
    ClienteEmpresa = 1,

    /// <summary>Operador de nómina. Descarga lo que suben los clientes y publica resultados y ajustes.</summary>
    OperadorNomina = 2,

    /// <summary>Administrador. Gestiona empresas, usuarios y consulta la bitácora de auditoría.</summary>
    Administrador = 3,
}
