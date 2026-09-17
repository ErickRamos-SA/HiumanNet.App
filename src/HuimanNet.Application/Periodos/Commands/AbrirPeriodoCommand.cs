namespace HuimanNet.Application.Periodos.Commands;

/// <summary>
/// Abre un período de carga para una empresa cliente.
/// </summary>
/// <param name="EmpresaId">Empresa para la que se abre el período.</param>
/// <param name="Anio">Año del período.</param>
/// <param name="Mes">Mes del período, de 1 a 12.</param>
/// <param name="Consecutivo">Número de período dentro del mes.</param>
/// <param name="Descripcion">Descripción legible del período.</param>
/// <param name="FechaLimiteCarga">Fecha límite opcional para las cargas del cliente.</param>
public sealed record AbrirPeriodoCommand(
    Guid EmpresaId,
    int Anio,
    int Mes,
    int Consecutivo,
    string Descripcion,
    DateTimeOffset? FechaLimiteCarga);
