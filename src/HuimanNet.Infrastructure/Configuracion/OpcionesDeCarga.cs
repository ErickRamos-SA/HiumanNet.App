namespace HuimanNet.Infrastructure.Configuracion;

/// <summary>
/// Opciones de la política de carga de archivos, enlazadas desde configuración.
/// </summary>
/// <remarks>
/// Se traducen a <see cref="Domain.Services.PoliticaDeCarga"/> en el registro de
/// dependencias. Cambiar la lista blanca o el tope de tamaño no requiere
/// recompilar: es una decisión operativa, no de código.
/// </remarks>
public sealed class OpcionesDeCarga
{
    /// <summary>Nombre de la sección de configuración que enlaza estas opciones.</summary>
    public const string Seccion = "Carga";

    /// <summary>
    /// Obtiene o establece las extensiones aceptadas.
    /// </summary>
    /// <value>Lista blanca con punto inicial; cualquier extensión no enumerada se rechaza.</value>
    public string[] ExtensionesPermitidas { get; set; } =
        [".xlsx", ".xls", ".csv", ".pdf", ".txt", ".zip"];

    /// <summary>
    /// Obtiene o establece el tamaño máximo por archivo, en megabytes.
    /// </summary>
    /// <value>50 MB por defecto.</value>
    public int TamanoMaximoMegabytes { get; set; } = 50;
}
