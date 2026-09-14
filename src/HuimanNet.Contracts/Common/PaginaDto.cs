namespace HuimanNet.Contracts.Common;

/// <summary>
/// Envoltorio de transporte para respuestas paginadas.
/// </summary>
/// <typeparam name="T">Tipo de los elementos devueltos.</typeparam>
/// <param name="Elementos">Elementos de la página actual.</param>
/// <param name="TotalElementos">Total de elementos que satisfacen el filtro.</param>
/// <param name="Pagina">Número de página devuelta, empezando en 1.</param>
/// <param name="TamanoPagina">Número máximo de elementos por página.</param>
/// <param name="TotalPaginas">Número total de páginas disponibles.</param>
public sealed record PaginaDto<T>(
    IReadOnlyList<T> Elementos,
    int TotalElementos,
    int Pagina,
    int TamanoPagina,
    int TotalPaginas);
