namespace HuimanNet.Domain.Enums;

/// <summary>
/// Acciones registradas en la bitácora de auditoría.
/// </summary>
/// <remarks>
/// En un portal de datos de nómina, saber <b>quién descargó</b> es tan relevante
/// como saber quién subió (ARQUITECTURA.md §6.3).
/// </remarks>
public enum AccionAuditada
{
    /// <summary>Valor no especificado. Nunca debe persistirse.</summary>
    NoEspecificado = 0,

    /// <summary>Se emitió un SAS de escritura para cargar un documento.</summary>
    SolicitudDeCarga = 1,

    /// <summary>El usuario confirmó que la carga del archivo terminó.</summary>
    CargaConfirmada = 2,

    /// <summary>Se emitió un SAS de lectura para descargar un documento.</summary>
    Descarga = 3,

    /// <summary>El escaneo de malware devolvió su veredicto.</summary>
    ResultadoDeEscaneo = 4,

    /// <summary>Cambió el estado del período de carga.</summary>
    CambioEstadoPeriodo = 5,

    /// <summary>Se denegó el acceso a un recurso por falta de permisos o por pertenecer a otra empresa.</summary>
    AccesoDenegado = 6,

    /// <summary>Se ejecutó una corrida de cálculo de nómina.</summary>
    CalculoDeNomina = 7,

    /// <summary>Se cotejó una corrida contra el resultado manual.</summary>
    CotejoDeNomina = 8,

    /// <summary>Se modificó un catálogo (empresas, razones sociales, empleados, parámetros, tablas o conceptos).</summary>
    AdministracionDeCatalogo = 9,

    /// <summary>Se creó o modificó un usuario o sus permisos.</summary>
    AdministracionDeUsuario = 10,

    /// <summary>Un usuario inició sesión con credenciales locales.</summary>
    InicioDeSesion = 11,

    /// <summary>Se capturaron o importaron incidencias de un período.</summary>
    CapturaDeIncidencias = 12,

    /// <summary>Cambió el estado de una corrida de nómina.</summary>
    CambioEstadoCorrida = 13,
}
