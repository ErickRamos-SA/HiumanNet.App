using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Incidencias;

/// <summary>
/// Incidencia de un contrato en un período. Cuando aún no se ha capturado,
/// <paramref name="Id"/> es <c>null</c> y las cantidades son las de un período
/// completo sin novedades.
/// </summary>
/// <param name="Id">Identificador de la incidencia, o <c>null</c> si no se ha capturado.</param>
/// <param name="PeriodoId">Período.</param>
/// <param name="ContratoId">Contrato.</param>
/// <param name="EmpleadoId">Empleado.</param>
/// <param name="ClaveEmpleado">Clave del empleado.</param>
/// <param name="NombreEmpleado">Nombre del empleado.</param>
/// <param name="RazonSocialNombre">Razón social del contrato.</param>
/// <param name="Esquema">Esquema de pago del contrato.</param>
/// <param name="DiasPeriodo">Días del período.</param>
/// <param name="Vacaciones">Días de vacaciones.</param>
/// <param name="Ausentismos">Días de ausentismo.</param>
/// <param name="Incapacidades">Días de incapacidad.</param>
/// <param name="Festivos">Festivos laborados.</param>
/// <param name="HorasDobles">Horas dobles.</param>
/// <param name="HorasTriples">Horas triples.</param>
/// <param name="DomingosTrabajados">Domingos trabajados.</param>
/// <param name="Gratificacion">Gratificación o bonos.</param>
/// <param name="Reembolsos">Reembolsos.</param>
/// <param name="Teletrabajo">Teletrabajo.</param>
/// <param name="Finiquito">Finiquito.</param>
/// <param name="Cafeteria">Cafetería.</param>
/// <param name="HorasDescontadas">Horas descontadas.</param>
/// <param name="OtrosDescuentos">Otros descuentos.</param>
/// <param name="PrestamoPersonal">Préstamo personal.</param>
/// <param name="Aguinaldo">Aguinaldo.</param>
/// <param name="DescuentosFiscales">Descuentos del recibo fiscal.</param>
/// <param name="FonacotCapturado">FONACOT capturado.</param>
/// <param name="DescuentoSindicalAdicional">Descuento adicional sindical.</param>
/// <param name="AjusteSindical">Ajuste sindical.</param>
/// <param name="IsrManual">ISR manual, o <c>null</c>.</param>
/// <param name="TipoDeMovimiento">Ordinaria o finiquito.</param>
/// <param name="Observaciones">Observaciones.</param>
/// <param name="CapturadoPor">Nombre de quien capturó, o <c>null</c>.</param>
/// <param name="FechaCaptura">Instante de captura, o <c>null</c>.</param>
public sealed record IncidenciaDto(
    Guid? Id,
    Guid PeriodoId,
    Guid ContratoId,
    Guid EmpleadoId,
    string ClaveEmpleado,
    string NombreEmpleado,
    string RazonSocialNombre,
    EsquemaDePago Esquema,
    decimal DiasPeriodo,
    decimal Vacaciones,
    decimal Ausentismos,
    decimal Incapacidades,
    decimal Festivos,
    decimal HorasDobles,
    decimal HorasTriples,
    decimal DomingosTrabajados,
    decimal Gratificacion,
    decimal Reembolsos,
    decimal Teletrabajo,
    decimal Finiquito,
    decimal Cafeteria,
    decimal HorasDescontadas,
    decimal OtrosDescuentos,
    decimal PrestamoPersonal,
    decimal Aguinaldo,
    decimal DescuentosFiscales,
    decimal FonacotCapturado,
    decimal DescuentoSindicalAdicional,
    decimal AjusteSindical,
    decimal? IsrManual,
    TipoDeMovimiento TipoDeMovimiento,
    string? Observaciones,
    string? CapturadoPor,
    DateTimeOffset? FechaCaptura);

/// <summary>
/// Petición para capturar o actualizar la incidencia de un contrato en un período.
/// </summary>
/// <param name="PeriodoId">Período.</param>
/// <param name="ContratoId">Contrato.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <param name="DiasPeriodo">Días del período.</param>
/// <param name="Vacaciones">Días de vacaciones.</param>
/// <param name="Ausentismos">Días de ausentismo.</param>
/// <param name="Incapacidades">Días de incapacidad.</param>
/// <param name="Festivos">Festivos laborados.</param>
/// <param name="HorasDobles">Horas dobles.</param>
/// <param name="HorasTriples">Horas triples.</param>
/// <param name="DomingosTrabajados">Domingos trabajados.</param>
/// <param name="Gratificacion">Gratificación o bonos.</param>
/// <param name="Reembolsos">Reembolsos.</param>
/// <param name="Teletrabajo">Teletrabajo.</param>
/// <param name="Finiquito">Finiquito.</param>
/// <param name="Cafeteria">Cafetería.</param>
/// <param name="HorasDescontadas">Horas descontadas.</param>
/// <param name="OtrosDescuentos">Otros descuentos.</param>
/// <param name="PrestamoPersonal">Préstamo personal.</param>
/// <param name="Aguinaldo">Aguinaldo.</param>
/// <param name="DescuentosFiscales">Descuentos del recibo fiscal.</param>
/// <param name="FonacotCapturado">FONACOT capturado.</param>
/// <param name="DescuentoSindicalAdicional">Descuento adicional sindical.</param>
/// <param name="AjusteSindical">Ajuste sindical.</param>
/// <param name="IsrManual">ISR manual, o <c>null</c> para calcularlo.</param>
/// <param name="TipoDeMovimiento">Ordinaria o finiquito.</param>
/// <param name="Observaciones">Observaciones.</param>
public sealed record GuardarIncidenciaRequest(
    Guid PeriodoId,
    Guid ContratoId,
    Guid? EmpresaId,
    decimal DiasPeriodo,
    decimal Vacaciones,
    decimal Ausentismos,
    decimal Incapacidades,
    decimal Festivos,
    decimal HorasDobles,
    decimal HorasTriples,
    decimal DomingosTrabajados,
    decimal Gratificacion,
    decimal Reembolsos,
    decimal Teletrabajo,
    decimal Finiquito,
    decimal Cafeteria,
    decimal HorasDescontadas,
    decimal OtrosDescuentos,
    decimal PrestamoPersonal,
    decimal Aguinaldo,
    decimal DescuentosFiscales,
    decimal FonacotCapturado,
    decimal DescuentoSindicalAdicional,
    decimal AjusteSindical,
    decimal? IsrManual,
    TipoDeMovimiento TipoDeMovimiento,
    string? Observaciones);

/// <summary>
/// Petición para importar las incidencias de un período desde uno de sus
/// archivos de incidencias (CSV o XLSX con el diseño de la hoja de incidencias
/// del modelo de referencia).
/// </summary>
/// <param name="DocumentoId">Documento de tipo incidencias del período, ya disponible.</param>
/// <param name="EmpresaId">Empresa; sólo la aportan los roles transversales.</param>
/// <remarks>
/// El archivo no viaja en la petición: se sube antes a los documentos del
/// período y aquí sólo se indica cuál importar.
/// </remarks>
public sealed record ImportarIncidenciasRequest(Guid DocumentoId, Guid? EmpresaId);

/// <summary>
/// Resultado de una importación de incidencias.
/// </summary>
/// <param name="FilasLeidas">Filas con datos encontradas en el archivo.</param>
/// <param name="Creadas">Incidencias creadas.</param>
/// <param name="Actualizadas">Incidencias actualizadas.</param>
/// <param name="Errores">Filas que no pudieron importarse, con el motivo.</param>
public sealed record ResultadoDeImportacionDto(
    int FilasLeidas,
    int Creadas,
    int Actualizadas,
    IReadOnlyList<string> Errores);
