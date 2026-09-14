using HuimanNet.Domain.Enums;

namespace HuimanNet.Web.Services;

/// <summary>
/// Determina el idioma de una petición anónima (pantalla de acceso, errores).
/// </summary>
/// <remarks>
/// Con sesión iniciada manda la preferencia guardada del usuario. Sin sesión se
/// usa, por orden, la cookie <see cref="NombreDeCookie"/> y el encabezado
/// <c>Accept-Language</c> del navegador.
/// </remarks>
public static class IdiomaDeLaPeticion
{
    /// <summary>Nombre de la cookie que recuerda el idioma elegido.</summary>
    public const string NombreDeCookie = "huimannet.idioma";

    /// <summary>
    /// Detecta el idioma de la petición.
    /// </summary>
    /// <param name="contexto">Contexto HTTP, o <c>null</c> fuera de una petición.</param>
    /// <returns>El idioma detectado; español por omisión.</returns>
    public static Idioma Detectar(HttpContext? contexto)
    {
        if (contexto is null)
        {
            return Idioma.Espanol;
        }

        if (contexto.Request.Cookies.TryGetValue(NombreDeCookie, out string? codigo) && !string.IsNullOrWhiteSpace(codigo))
        {
            return IdiomaExtensiones.DesdeCodigo(codigo);
        }

        string aceptados = contexto.Request.Headers.AcceptLanguage.ToString();
        return aceptados.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? Idioma.Ingles : Idioma.Espanol;
    }

    /// <summary>
    /// Guarda el idioma elegido en una cookie de larga duración.
    /// </summary>
    /// <param name="contexto">Contexto HTTP.</param>
    /// <param name="idioma">Idioma elegido.</param>
    public static void Recordar(HttpContext contexto, Idioma idioma)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        contexto.Response.Cookies.Append(NombreDeCookie, idioma.Codigo(), new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(365),
        });
    }
}
