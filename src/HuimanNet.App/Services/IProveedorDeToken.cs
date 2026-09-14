namespace HuimanNet.App.Services;

/// <summary>
/// Obtiene los tokens de acceso con los que la app llama a la API.
/// </summary>
/// <remarks>
/// La app móvil es un <b>cliente público</b>: no guarda ningún secreto. El token
/// se obtiene con el flujo de código de autorización con PKCE y lo custodia el
/// almacén seguro del sistema operativo, no la aplicación.
/// </remarks>
public interface IProveedorDeToken
{
    /// <summary>
    /// Obtiene un token de acceso sin interacción del usuario.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// El token vigente, o <c>null</c> si hace falta que el usuario inicie sesión.
    /// </returns>
    Task<string?> ObtenerTokenSilenciosoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicia sesión de forma interactiva.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El token de acceso obtenido.</returns>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si el usuario cancela o si el proveedor de identidad rechaza la solicitud.
    /// </exception>
    Task<string> IniciarSesionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cierra la sesión y descarta los tokens almacenados.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task CerrarSesionAsync(CancellationToken cancellationToken = default);
}
