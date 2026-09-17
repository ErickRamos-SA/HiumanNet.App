using HuimanNet.Domain.Entities;

namespace HuimanNet.Application.Incidencias;

/// <summary>
/// Datos de referencia con los que se interpreta un archivo de incidencias.
/// </summary>
/// <param name="EmpleadosPorClave">Empleados activos por clave.</param>
/// <param name="ContratosPorEmpleado">Contratos vigentes por empleado.</param>
/// <param name="RazonesSociales">Razones sociales por identificador.</param>
/// <param name="Existentes">Incidencias ya capturadas por contrato.</param>
public sealed record ContextoDeImportacion(
    IReadOnlyDictionary<string, Empleado> EmpleadosPorClave,
    ILookup<Guid, Contrato> ContratosPorEmpleado,
    IReadOnlyDictionary<Guid, RazonSocial> RazonesSociales,
    IReadOnlyDictionary<Guid, Incidencia> Existentes);
