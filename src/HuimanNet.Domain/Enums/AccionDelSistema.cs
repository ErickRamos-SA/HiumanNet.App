namespace HuimanNet.Domain.Enums;

/// <summary>
/// Acciones del sistema que el administrador puede habilitar o deshabilitar
/// por usuario, además de las que su rol concede por defecto.
/// </summary>
/// <remarks>
/// Cada rol tiene un conjunto predeterminado de acciones
/// (<see cref="Services.PermisosPorRol"/>). El administrador puede ampliarlo o
/// recortarlo para un usuario concreto; la matriz efectiva la resuelve
/// <see cref="Services.PoliticaDeAcceso"/>.
/// </remarks>
public enum AccionDelSistema
{
    /// <summary>Valor no especificado. Nunca debe persistirse.</summary>
    NoEspecificado = 0,

    /// <summary>Cargar documentos de incidencias y datos de empleados.</summary>
    CargarDocumentos = 1,

    /// <summary>Descargar documentos del portal.</summary>
    DescargarDocumentos = 2,

    /// <summary>Publicar archivos de resultado y ajustes.</summary>
    PublicarResultados = 3,

    /// <summary>Abrir períodos y cambiar su estado.</summary>
    GestionarPeriodos = 4,

    /// <summary>Capturar o importar incidencias del período.</summary>
    CapturarIncidencias = 5,

    /// <summary>Ejecutar el cálculo de nómina del sistema.</summary>
    CalcularNomina = 6,

    /// <summary>Consultar los resultados calculados por el sistema.</summary>
    ConsultarNomina = 7,

    /// <summary>Cotejar el cálculo del sistema contra el resultado manual.</summary>
    CotejarNomina = 8,

    /// <summary>Aprobar o descartar una corrida de nómina.</summary>
    AprobarNomina = 9,

    /// <summary>Administrar el catálogo de empresas y razones sociales.</summary>
    AdministrarEmpresas = 10,

    /// <summary>Administrar empleados y contratos.</summary>
    AdministrarEmpleados = 11,

    /// <summary>Administrar usuarios y sus permisos.</summary>
    AdministrarUsuarios = 12,

    /// <summary>Administrar parámetros, tablas y fórmulas del cálculo.</summary>
    AdministrarCatalogosDeCalculo = 13,

    /// <summary>Consultar la bitácora de auditoría.</summary>
    ConsultarBitacora = 14,

    /// <summary>Consultar la explicación de los cálculos.</summary>
    ConsultarExplicacionDeCalculos = 15,
}
