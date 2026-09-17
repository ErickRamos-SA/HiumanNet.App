namespace HuimanNet.Application.Common;

/// <summary>
/// Valida la entrada de un caso de uso antes de que llegue al dominio.
/// </summary>
/// <typeparam name="T">Tipo del comando o consulta a validar.</typeparam>
/// <remarks>
/// Los validadores se escriben a mano y no usan reflexión, requisito para que
/// la API pueda publicarse con Native AOT (ESPECIFICACION.md §6).
/// </remarks>
public interface IValidadorDeEntrada<in T>
{
    /// <summary>
    /// Valida la entrada indicada.
    /// </summary>
    /// <param name="entrada">Comando o consulta a validar.</param>
    /// <returns>El resultado con los errores acumulados.</returns>
    ResultadoDeValidacion Validar(T entrada);
}
