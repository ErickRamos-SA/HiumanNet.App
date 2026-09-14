namespace HuimanNet.App.Configuracion;

/// <summary>
/// Parámetros de conexión de la app móvil.
/// </summary>
/// <remarks>
/// Son públicos por naturaleza (una app instalada no puede guardar secretos):
/// dirección de la API y datos del registro de la app en Microsoft Entra. El
/// modo de identidad (Entra o cuentas locales) no se fija aquí: lo informa la
/// API en <c>/api/v1/configuracion</c>, de modo que la misma app sirve para
/// desarrollo y para producción.
/// </remarks>
public static class OpcionesDeLaApp
{
    /// <summary>Obtiene la dirección base de la API.</summary>
    /// <value>
    /// En depuración, la API local por HTTP: en el emulador de Android el equipo
    /// anfitrión es <c>10.0.2.2</c>; en un teléfono conectado por USB se usa
    /// <c>localhost</c> y se reenvía el puerto con <c>adb reverse tcp:5208 tcp:5208</c>.
    /// En publicación, la API de Azure por HTTPS.
    /// </value>
    public static string UrlBaseApi { get; } =
#if DEBUG
        DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.DeviceType == DeviceType.Virtual
            ? "http://10.0.2.2:5208"
            : "http://localhost:5208";
#else
        "https://api.huimannet.example";
#endif

    /// <summary>Obtiene el identificador de la app registrada en Microsoft Entra.</summary>
    /// <value>Client ID público.</value>
    public static string ClientId { get; } = "00000000-0000-0000-0000-000000000000";

    /// <summary>Obtiene la autoridad del inquilino de Entra.</summary>
    /// <value>URL del inquilino.</value>
    public static string Autoridad { get; } = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000";

    /// <summary>Obtiene los ámbitos que se piden para llamar a la API.</summary>
    /// <value>Ámbito expuesto por la API.</value>
    public static IReadOnlyList<string> Ambitos { get; } = ["api://huimannet/acceso"];

    /// <summary>Obtiene el URI de redirección de MSAL.</summary>
    /// <value>Esquema <c>msal{ClientId}</c>.</value>
    public static string UriDeRedireccion { get; } = $"msal{ClientId}://auth";

    /// <summary>Obtiene el tiempo máximo de una petición completa, reintentos incluidos.</summary>
    /// <value>Cinco minutos: el cálculo de una empresa grande puede tardar.</value>
    public static TimeSpan TiempoDeEspera { get; } = TimeSpan.FromMinutes(5);
}
