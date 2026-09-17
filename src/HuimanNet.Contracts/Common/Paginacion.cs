namespace HuimanNet.Contracts.Common;

/// <summary>
/// Tamaños de página por omisión y máximos de las consultas paginadas.
/// </summary>
/// <remarks>
/// Única fuente de estos valores para la API, el portal web y la aplicación
/// móvil: el cliente que no indica tamaño recibe el predeterminado y el que
/// pide de más recibe el máximo.
/// </remarks>
public static class Paginacion
{
    /// <summary>Número de la primera página; la numeración empieza en 1.</summary>
    public const int PaginaInicial = 1;

    /// <summary>Asientos de la bitácora por página cuando no se indica otro tamaño.</summary>
    public const int TamanoDeBitacora = 25;

    /// <summary>Máximo de asientos de la bitácora por página, para acotar el coste de la consulta.</summary>
    public const int TamanoMaximoDeBitacora = 200;

    /// <summary>Empleados por página cuando no se indica otro tamaño.</summary>
    public const int TamanoDeEmpleados = 50;

    /// <summary>Máximo de empleados por página, para acotar el coste de la consulta.</summary>
    public const int TamanoMaximoDeEmpleados = 500;

    /// <summary>
    /// Filas por página de las tablas que ya están completas en pantalla y se
    /// paginan sin volver a consultar, como los resultados de una corrida o las
    /// incidencias de un periodo.
    /// </summary>
    public const int TamanoDeTablaEnPantalla = 100;

    /// <summary>
    /// Ajusta un número de página solicitado al primero válido.
    /// </summary>
    /// <param name="pagina">Página pedida por el cliente.</param>
    /// <returns><paramref name="pagina"/>, o <see cref="PaginaInicial"/> si es menor.</returns>
    public static int NormalizarPagina(int pagina) => Math.Max(PaginaInicial, pagina);

    /// <summary>
    /// Ajusta un tamaño de página solicitado a los límites de la consulta.
    /// </summary>
    /// <param name="solicitado">Tamaño pedido; cero o negativo si el cliente no indicó ninguno.</param>
    /// <param name="predeterminado">Tamaño que se usa cuando no se indicó ninguno.</param>
    /// <param name="maximo">Tamaño máximo admitido.</param>
    /// <returns>
    /// <paramref name="predeterminado"/> si no se indicó tamaño; si no, el
    /// solicitado sin exceder <paramref name="maximo"/>.
    /// </returns>
    public static int NormalizarTamano(int solicitado, int predeterminado, int maximo)
        => solicitado <= 0 ? predeterminado : Math.Min(solicitado, maximo);
}
