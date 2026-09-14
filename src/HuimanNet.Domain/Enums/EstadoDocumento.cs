namespace HuimanNet.Domain.Enums;

/// <summary>
/// Ciclo de vida de un documento desde que se solicita su carga hasta que
/// queda disponible para descarga.
/// </summary>
/// <remarks>
/// Regla de seguridad no negociable: un documento sólo es descargable en estado
/// <see cref="Disponible"/>, es decir, después de que el escaneo de malware lo
/// declare limpio (ARQUITECTURA.md §6.1).
/// </remarks>
public enum EstadoDocumento
{
    /// <summary>Valor no especificado. Nunca debe persistirse.</summary>
    NoEspecificado = 0,

    /// <summary>Registrado en la base de datos; se emitió el SAS de escritura pero el archivo aún no se confirma.</summary>
    Pendiente = 1,

    /// <summary>Archivo cargado en Blob Storage; el escaneo de malware está en curso.</summary>
    Escaneando = 2,

    /// <summary>Escaneo superado. Es el único estado en el que el documento puede descargarse.</summary>
    Disponible = 3,

    /// <summary>El escaneo detectó contenido malicioso; el archivo fue movido a cuarentena.</summary>
    EnCuarentena = 4,

    /// <summary>La carga fue abandonada o rechazada antes de completarse.</summary>
    Descartado = 5,
}
