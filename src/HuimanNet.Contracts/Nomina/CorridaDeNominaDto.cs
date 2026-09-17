using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Nomina;

/// <summary>
/// Proyección de lectura de una corrida de nómina con sus totales.
/// </summary>
/// <param name="Id">Identificador único.</param>
/// <param name="EmpresaId">Empresa cliente.</param>
/// <param name="PeriodoId">Período calculado.</param>
/// <param name="PeriodoClave">Clave canónica del período.</param>
/// <param name="PeriodoDescripcion">Descripción del período.</param>
/// <param name="Numero">Número consecutivo dentro del período.</param>
/// <param name="Estado">Estado de la corrida.</param>
/// <param name="FechaDeReferencia">Fecha con la que se resolvieron las vigencias.</param>
/// <param name="FechaCalculo">Instante del cálculo, en UTC.</param>
/// <param name="CalculadaPor">Nombre del usuario que calculó.</param>
/// <param name="Trabajadores">Contratos calculados.</param>
/// <param name="Bruto">Suma del bruto de incidencias.</param>
/// <param name="Percepciones">Suma de percepciones.</param>
/// <param name="Deducciones">Suma de deducciones.</param>
/// <param name="Neto">Suma del neto pagado.</param>
/// <param name="Isr">Suma del ISR.</param>
/// <param name="ImssTrabajador">Suma de la cuota obrera.</param>
/// <param name="ImssPatronal">Suma de cuotas patronales.</param>
/// <param name="Infonavit">Suma de INFONAVIT patronal.</param>
/// <param name="Isn">Suma del ISN.</param>
/// <param name="ComplementoSindical">Suma del complemento sindical.</param>
/// <param name="Facturable">Suma de la base facturable.</param>
/// <param name="Comision">Suma de comisiones.</param>
/// <param name="CostoTotal">Suma del costo total antes de IVA.</param>
/// <param name="DuracionMs">Duración del cálculo en milisegundos.</param>
/// <param name="Observaciones">Nota del usuario.</param>
/// <param name="Advertencias">Advertencias del motor, una por línea.</param>
public sealed record CorridaDeNominaDto(
    Guid Id,
    Guid EmpresaId,
    Guid PeriodoId,
    string PeriodoClave,
    string PeriodoDescripcion,
    int Numero,
    EstadoDeCorrida Estado,
    DateOnly FechaDeReferencia,
    DateTimeOffset FechaCalculo,
    string CalculadaPor,
    int Trabajadores,
    decimal Bruto,
    decimal Percepciones,
    decimal Deducciones,
    decimal Neto,
    decimal Isr,
    decimal ImssTrabajador,
    decimal ImssPatronal,
    decimal Infonavit,
    decimal Isn,
    decimal ComplementoSindical,
    decimal Facturable,
    decimal Comision,
    decimal CostoTotal,
    long DuracionMs,
    string? Observaciones,
    string? Advertencias);
