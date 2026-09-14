namespace HuimanNet.Domain.Formulas;

/// <summary>
/// Fuente de valores con la que se evalúa una fórmula: variables del
/// trabajador y del período, parámetros del catálogo, conceptos ya calculados
/// y tablas por rangos.
/// </summary>
/// <remarks>
/// Separar la fórmula de su contexto permite compilar cada fórmula una sola
/// vez y evaluarla para miles de trabajadores sin volver a analizarla.
/// </remarks>
public interface IContextoDeEvaluacion
{
    /// <summary>
    /// Intenta resolver el valor de un identificador.
    /// </summary>
    /// <param name="nombre">Identificador en mayúsculas.</param>
    /// <param name="valor">Valor resuelto, si existe.</param>
    /// <returns><c>true</c> si el identificador tiene valor en este contexto.</returns>
    bool TryObtenerValor(string nombre, out decimal valor);

    /// <summary>
    /// Consulta una tabla por rangos con semántica de búsqueda aproximada: se
    /// toma el rango cuyo límite inferior es el mayor que no supera el valor.
    /// </summary>
    /// <param name="tabla">Clave de la tabla (por ejemplo <c>ISR</c>, <c>SUBSIDIO</c> o <c>CVR</c>).</param>
    /// <param name="valor">Valor con el que se ubica el rango.</param>
    /// <param name="campo">
    /// Campo a devolver: <c>LIMITE_INFERIOR</c>, <c>LIMITE_SUPERIOR</c>,
    /// <c>CUOTA_FIJA</c>, <c>PORCENTAJE</c> o <c>VALOR</c>.
    /// </param>
    /// <returns>El campo del rango, o cero si ningún rango cubre el valor.</returns>
    decimal ConsultarTabla(string tabla, decimal valor, string campo);
}
