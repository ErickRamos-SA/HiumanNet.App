namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Derivación y verificación de <i>hashes</i> de contraseña para el modo de
/// identidad local.
/// </summary>
/// <remarks>
/// La contraseña en claro nunca se almacena ni se registra. La implementación
/// usa una función de derivación con sal y factor de trabajo configurable.
/// </remarks>
public interface IHasherDeContrasenas
{
    /// <summary>
    /// Deriva el <i>hash</i> de una contraseña.
    /// </summary>
    /// <param name="contrasena">Contraseña en claro.</param>
    /// <returns>Cadena autocontenida con algoritmo, sal e iteraciones.</returns>
    string Hashear(string contrasena);

    /// <summary>
    /// Comprueba una contraseña contra un <i>hash</i>.
    /// </summary>
    /// <param name="contrasena">Contraseña en claro.</param>
    /// <param name="hash">Hash almacenado.</param>
    /// <returns><c>true</c> si coinciden.</returns>
    bool Verificar(string contrasena, string hash);
}
