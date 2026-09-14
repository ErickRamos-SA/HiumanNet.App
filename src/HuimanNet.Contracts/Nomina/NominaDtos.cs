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

/// <summary>
/// Resumen del resultado de un contrato dentro de una corrida.
/// </summary>
/// <param name="Id">Identificador del resultado.</param>
/// <param name="CorridaId">Corrida.</param>
/// <param name="ContratoId">Contrato.</param>
/// <param name="EmpleadoId">Empleado.</param>
/// <param name="RazonSocialId">Razón social.</param>
/// <param name="RazonSocialNombre">Nombre de la razón social.</param>
/// <param name="Esquema">Esquema de pago.</param>
/// <param name="ClaveEmpleado">Clave del empleado.</param>
/// <param name="NombreEmpleado">Nombre del empleado.</param>
/// <param name="TipoDeMovimiento">Ordinaria o finiquito.</param>
/// <param name="Bruto">Bruto de incidencias.</param>
/// <param name="TotalPercepciones">Percepciones del recibo.</param>
/// <param name="TotalDeducciones">Deducciones del recibo.</param>
/// <param name="Neto">Neto pagado.</param>
/// <param name="Isr">ISR retenido.</param>
/// <param name="Subsidio">Subsidio entregado.</param>
/// <param name="ImssTrabajador">Cuota obrera.</param>
/// <param name="ImssPatronal">Cuotas patronales.</param>
/// <param name="InfonavitPatronal">INFONAVIT patronal.</param>
/// <param name="InfonavitTrabajador">INFONAVIT trabajador.</param>
/// <param name="Fonacot">FONACOT.</param>
/// <param name="Isn">ISN.</param>
/// <param name="ComplementoSindical">Complemento sindical.</param>
/// <param name="Facturable">Base facturable.</param>
/// <param name="Comision">Comisión.</param>
/// <param name="CostoTotal">Costo total antes de IVA.</param>
/// <param name="CostoIsr">ISR trasladado en la factura.</param>
/// <param name="CostoImss">Cuotas IMSS trasladadas en la factura.</param>
/// <param name="CostoInfonavit">Retiro, cesantía e INFONAVIT trasladados en la factura.</param>
/// <param name="CostoOtros">Otros costos facturables.</param>
/// <param name="Advertencia">Advertencia del motor, o <c>null</c>.</param>
public sealed record ResultadoDeNominaDto(
    Guid Id,
    Guid CorridaId,
    Guid ContratoId,
    Guid EmpleadoId,
    Guid RazonSocialId,
    string RazonSocialNombre,
    EsquemaDePago Esquema,
    string ClaveEmpleado,
    string NombreEmpleado,
    TipoDeMovimiento TipoDeMovimiento,
    decimal Bruto,
    decimal TotalPercepciones,
    decimal TotalDeducciones,
    decimal Neto,
    decimal Isr,
    decimal Subsidio,
    decimal ImssTrabajador,
    decimal ImssPatronal,
    decimal InfonavitPatronal,
    decimal InfonavitTrabajador,
    decimal Fonacot,
    decimal Isn,
    decimal ComplementoSindical,
    decimal Facturable,
    decimal Comision,
    decimal CostoTotal,
    decimal CostoIsr,
    decimal CostoImss,
    decimal CostoInfonavit,
    decimal CostoOtros,
    string? Advertencia);

/// <summary>
/// Concepto calculado con su definición, para el detalle de un trabajador.
/// </summary>
/// <param name="Clave">Clave del concepto.</param>
/// <param name="Nombre">Nombre corto.</param>
/// <param name="Tipo">Naturaleza del concepto.</param>
/// <param name="Formula">Fórmula con la que se calculó.</param>
/// <param name="Importe">Importe calculado.</param>
/// <param name="VisibleEnRecibo">Si se muestra en el recibo.</param>
public sealed record ConceptoCalculadoDto(
    string Clave,
    string Nombre,
    TipoDeConcepto Tipo,
    string Formula,
    decimal Importe,
    bool VisibleEnRecibo);

/// <summary>
/// Detalle completo del resultado de un trabajador.
/// </summary>
/// <param name="Resultado">Resumen del resultado.</param>
/// <param name="Conceptos">Conceptos en orden de evaluación.</param>
/// <param name="Variables">Variables de entrada con las que se calculó.</param>
public sealed record DetalleDeResultadoDto(
    ResultadoDeNominaDto Resultado,
    IReadOnlyList<ConceptoCalculadoDto> Conceptos,
    IReadOnlyList<ValorDto> Variables);

/// <summary>
/// Par clave-valor numérico.
/// </summary>
/// <param name="Clave">Clave.</param>
/// <param name="Valor">Valor.</param>
public sealed record ValorDto(string Clave, decimal Valor);

/// <summary>
/// Factura estimada de una razón social dentro de una corrida, separando
/// nómina ordinaria y finiquitos.
/// </summary>
/// <param name="RazonSocialId">Razón social.</param>
/// <param name="RazonSocialNombre">Nombre.</param>
/// <param name="TipoDeServicio">Nómina o Maquila.</param>
/// <param name="Trabajadores">Contratos incluidos.</param>
/// <param name="BaseNomina">Base facturable de nómina ordinaria.</param>
/// <param name="BaseFiniquitos">Base facturable de finiquitos.</param>
/// <param name="Isn">ISN.</param>
/// <param name="Isr">ISR retenido.</param>
/// <param name="Imss">Cuotas IMSS.</param>
/// <param name="Infonavit">Retiro, cesantía e INFONAVIT.</param>
/// <param name="Otros">Otros costos.</param>
/// <param name="Comision">Comisión.</param>
/// <param name="Subtotal">Subtotal antes de IVA.</param>
/// <param name="Iva">IVA.</param>
/// <param name="Total">Total facturado.</param>
public sealed record FacturacionDeCorridaDto(
    Guid RazonSocialId,
    string RazonSocialNombre,
    TipoDeServicio TipoDeServicio,
    int Trabajadores,
    decimal BaseNomina,
    decimal BaseFiniquitos,
    decimal Isn,
    decimal Isr,
    decimal Imss,
    decimal Infonavit,
    decimal Otros,
    decimal Comision,
    decimal Subtotal,
    decimal Iva,
    decimal Total);

/// <summary>
/// Corrida con sus resultados y la facturación estimada por razón social.
/// </summary>
/// <param name="Corrida">Corrida.</param>
/// <param name="Resultados">Resultados por contrato.</param>
/// <param name="Facturacion">Facturación por razón social.</param>
public sealed record ResumenDeCorridaDto(
    CorridaDeNominaDto Corrida,
    IReadOnlyList<ResultadoDeNominaDto> Resultados,
    IReadOnlyList<FacturacionDeCorridaDto> Facturacion);

/// <summary>
/// Petición para calcular la nómina de un período.
/// </summary>
/// <param name="PeriodoId">Período a calcular.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="Observaciones">Nota opcional.</param>
public sealed record CalcularNominaRequest(Guid PeriodoId, Guid? EmpresaId, string? Observaciones = null);

/// <summary>
/// Petición para aprobar o descartar una corrida.
/// </summary>
/// <param name="Estado">Estado destino: aprobada o descartada.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="Observaciones">Nota opcional.</param>
public sealed record CambiarEstadoCorridaRequest(EstadoDeCorrida Estado, Guid? EmpresaId, string? Observaciones = null);

/// <summary>
/// Petición para cotejar una corrida contra el archivo de resultados manual.
/// </summary>
/// <param name="CorridaId">Corrida a cotejar.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="DocumentoId">Archivo de resultados o de ajuste publicado en el mismo período.</param>
/// <param name="ToleranciaAbsoluta">Diferencia absoluta aceptada por concepto.</param>
public sealed record CotejarNominaRequest(
    Guid CorridaId,
    Guid? EmpresaId,
    Guid DocumentoId,
    decimal ToleranciaAbsoluta);

/// <summary>
/// Diferencia entre el sistema y el resultado manual.
/// </summary>
/// <param name="ClaveEmpleado">Clave del trabajador.</param>
/// <param name="ConceptoClave">Concepto comparado.</param>
/// <param name="ImporteSistema">Importe del sistema, o <c>null</c>.</param>
/// <param name="ImporteManual">Importe manual, o <c>null</c>.</param>
/// <param name="Diferencia">Sistema menos manual.</param>
/// <param name="DentroDeTolerancia">Si la diferencia está dentro de la tolerancia.</param>
public sealed record DiferenciaDeCotejoDto(
    string ClaveEmpleado,
    string ConceptoClave,
    decimal? ImporteSistema,
    decimal? ImporteManual,
    decimal Diferencia,
    bool DentroDeTolerancia);

/// <summary>
/// Cotejo de una corrida.
/// </summary>
/// <param name="Id">Identificador.</param>
/// <param name="CorridaId">Corrida.</param>
/// <param name="FechaCotejo">Instante del cotejo, en UTC.</param>
/// <param name="Usuario">Nombre del usuario que cotejó.</param>
/// <param name="NombreArchivo">Archivo manual.</param>
/// <param name="ToleranciaAbsoluta">Tolerancia aplicada.</param>
/// <param name="TotalComparaciones">Comparaciones realizadas.</param>
/// <param name="TotalFueraDeTolerancia">Comparaciones fuera de tolerancia.</param>
/// <param name="Diferencias">Detalle; vacío en los listados.</param>
public sealed record CotejoDto(
    Guid Id,
    Guid CorridaId,
    DateTimeOffset FechaCotejo,
    string Usuario,
    string NombreArchivo,
    decimal ToleranciaAbsoluta,
    int TotalComparaciones,
    int TotalFueraDeTolerancia,
    IReadOnlyList<DiferenciaDeCotejoDto> Diferencias);
