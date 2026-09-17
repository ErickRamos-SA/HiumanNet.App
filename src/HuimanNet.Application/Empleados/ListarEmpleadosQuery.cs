namespace HuimanNet.Application.Empleados;

/// <summary>
/// Lista los empleados de forma paginada.
/// </summary>
/// <param name="EmpresaId">
/// Empresa a filtrar. Los roles transversales pueden omitirla para ver todas;
/// al usuario de empresa cliente se le impone la suya.
/// </param>
/// <param name="SoloActivos">Si es <c>true</c>, omite los dados de baja.</param>
/// <param name="Texto">Texto a buscar en clave o nombre.</param>
/// <param name="Pagina">Número de página, empezando en 1.</param>
/// <param name="TamanoPagina">Elementos por página.</param>
public sealed record ListarEmpleadosQuery(Guid? EmpresaId, bool SoloActivos, string? Texto, int Pagina, int TamanoPagina);
