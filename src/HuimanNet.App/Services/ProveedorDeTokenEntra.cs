using HuimanNet.App.Configuracion;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace HuimanNet.App.Services;

/// <summary>
/// Implementación de <see cref="IProveedorDeToken"/> con MSAL contra Microsoft
/// Entra.
/// </summary>
/// <remarks>
/// Usa el flujo de código de autorización con <b>PKCE</b> a través del navegador
/// del sistema. La caché de tokens la gestiona MSAL sobre el almacén seguro de
/// cada plataforma (Keychain en iOS, KeyStore en Android): la aplicación nunca
/// escribe el token en disco.
/// </remarks>
public sealed class ProveedorDeTokenEntra : IProveedorDeToken
{
    private static readonly TimeSpan TiempoMaximoSilencioso = TimeSpan.FromSeconds(10);

    private readonly IPublicClientApplication _cliente;
    private readonly ILogger<ProveedorDeTokenEntra> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ProveedorDeTokenEntra"/>.
    /// </summary>
    /// <param name="logger">Registro de eventos.</param>
    public ProveedorDeTokenEntra(ILogger<ProveedorDeTokenEntra> logger)
    {
        _logger = logger;

        _cliente = PublicClientApplicationBuilder
            .Create(OpcionesDeLaApp.ClientId)
            .WithAuthority(OpcionesDeLaApp.Autoridad)
            .WithRedirectUri(OpcionesDeLaApp.UriDeRedireccion)
            .Build();
    }

    /// <inheritdoc/>
    public async Task<string?> ObtenerTokenSilenciosoAsync(
        CancellationToken cancellationToken = default)
    {
        IEnumerable<IAccount> cuentas;

        try
        {
            // MSAL no admite cancelación aquí: se acota la espera para que un
            // problema de red no deje la app detenida sin explicación.
            cuentas = await _cliente.GetAccountsAsync().WaitAsync(TiempoMaximoSilencioso, cancellationToken);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("MSAL no respondió al consultar las cuentas; se continúa sin sesión.");
            return null;
        }
        IAccount? cuenta = cuentas.FirstOrDefault();

        if (cuenta is null)
        {
            return null;
        }

        try
        {
            AuthenticationResult resultado = await _cliente
                .AcquireTokenSilent(OpcionesDeLaApp.Ambitos, cuenta)
                .ExecuteAsync(cancellationToken);

            return resultado.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            // El token caducó y el refresco no basta: hace falta interacción.
            _logger.LogInformation("La renovación silenciosa exige interacción del usuario.");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<string> IniciarSesionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            AcquireTokenInteractiveParameterBuilder solicitud = _cliente
                .AcquireTokenInteractive(OpcionesDeLaApp.Ambitos)
                .WithUseEmbeddedWebView(false);

#if ANDROID
            // MSAL necesita la actividad visible para abrir el navegador del sistema
            // y recibir el retorno (MainActivity.OnActivityResult).
            if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is { } actividad)
            {
                solicitud = solicitud.WithParentActivityOrWindow(actividad);
            }
#endif

            AuthenticationResult resultado = await solicitud.ExecuteAsync(cancellationToken);

            _logger.LogInformation("Sesión iniciada correctamente.");
            return resultado.AccessToken;
        }
        catch (MsalException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudo completar el inicio de sesión.", excepcion);
        }
    }

    /// <inheritdoc/>
    public async Task CerrarSesionAsync(CancellationToken cancellationToken = default)
    {
        foreach (IAccount cuenta in await _cliente.GetAccountsAsync())
        {
            await _cliente.RemoveAsync(cuenta);
        }

        _logger.LogInformation("Sesión cerrada y caché de tokens vaciada.");
    }
}
