namespace HuimanNet.Contracts.Usuarios;

/// <summary>
/// Indicadores del panel de inicio.
/// </summary>
/// <param name="EmpleadosActivos">Empleados activos en el ámbito del usuario.</param>
/// <param name="PeriodosAbiertos">Períodos que admiten cargas.</param>
/// <param name="DocumentosDisponibles">Documentos descargables en períodos no cerrados.</param>
/// <param name="CorridasPorCotejar">Corridas calculadas y aún no cotejadas.</param>
/// <param name="Pendientes">Tareas sugeridas para el usuario.</param>
public sealed record ResumenDeInicioDto(
    int EmpleadosActivos,
    int PeriodosAbiertos,
    int DocumentosDisponibles,
    int CorridasPorCotejar,
    IReadOnlyList<PendienteDto> Pendientes);
