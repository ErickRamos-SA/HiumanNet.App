namespace HuimanNet.Api.Seguridad;

/// <summary>
/// Extensiones de registro de <see cref="MiddlewareDeUsuarioActual"/>.
/// </summary>
public static class MiddlewareDeUsuarioActualExtensions
{
    /// <summary>
    /// Agrega la resolución del usuario actual a la canalización.
    /// </summary>
    /// <param name="app">Constructor de la aplicación.</param>
    /// <returns>El mismo constructor, para encadenar llamadas.</returns>
    public static IApplicationBuilder UsarUsuarioActual(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<MiddlewareDeUsuarioActual>();
    }
}
