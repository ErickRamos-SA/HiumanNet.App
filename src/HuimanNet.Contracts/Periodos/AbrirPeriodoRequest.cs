namespace HuimanNet.Contracts.Periodos;

/// <summary>
/// Petición para abrir un período de carga a una empresa cliente.
/// </summary>
/// <param name="EmpresaId">Empresa para la que se abre el período.</param>
/// <param name="Anio">Año del período.</param>
/// <param name="Mes">Mes del período, de 1 a 12.</param>
/// <param name="Consecutivo">Número de período dentro del mes, empezando en 1.</param>
/// <param name="Descripcion">Descripción legible, por ejemplo <c>"Segunda quincena de agosto"</c>.</param>
/// <param name="FechaLimiteCarga">Fecha límite opcional para las cargas del cliente, en UTC.</param>
public sealed record AbrirPeriodoRequest(
    Guid EmpresaId,
    int Anio,
    int Mes,
    int Consecutivo,
    string Descripcion,
    DateTimeOffset? FechaLimiteCarga = null);
