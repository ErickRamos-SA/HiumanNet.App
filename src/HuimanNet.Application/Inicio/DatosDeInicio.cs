namespace HuimanNet.Application.Inicio;

/// <summary>
/// Datos que lee el panel de inicio, antes de decidir qué se sugiere.
/// </summary>
/// <param name="EmpleadosActivos">Empleados activos en el ámbito.</param>
/// <param name="PeriodosAbiertos">Períodos que admiten cargas.</param>
/// <param name="DocumentosDisponibles">Documentos descargables en períodos no cerrados.</param>
/// <param name="CorridasPorCotejar">Corridas calculadas y aún no cotejadas.</param>
/// <param name="Periodos">Períodos no cerrados más recientes.</param>
/// <param name="Corridas">Corridas calculadas más recientes, pendientes de cotejo.</param>
public sealed record DatosDeInicio(
    int EmpleadosActivos,
    int PeriodosAbiertos,
    int DocumentosDisponibles,
    int CorridasPorCotejar,
    IReadOnlyList<PeriodoDeInicio> Periodos,
    IReadOnlyList<CorridaDeInicio> Corridas);
