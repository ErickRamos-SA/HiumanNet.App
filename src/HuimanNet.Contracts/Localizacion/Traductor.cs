using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;
using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Localizacion;

/// <summary>
/// Textos de la interfaz en español e inglés, compartidos por la web y la app móvil.
/// </summary>
/// <remarks>
/// Los textos viven en <c>Localizacion/textos_es.json</c> y
/// <c>Localizacion/textos_en.json</c>, incrustados en el ensamblado. Se leen
/// una sola vez por proceso con <see cref="Utf8JsonReader"/> (sin reflexión) y
/// se aplanan en un diccionario congelado: los objetos anidados producen claves
/// con puntos (<c>"nav": { "inicio": "…" }</c> ⇒ <c>nav.inicio</c>).
/// <para>
/// Si falta una clave en inglés se usa la española; si falta en ambos, se
/// muestra la propia clave, lo que hace evidente la omisión sin romper la pantalla.
/// Una prueba unitaria verifica que ambos idiomas tengan las mismas claves.
/// </para>
/// </remarks>
public sealed class Traductor
{
    private static readonly Lazy<FrozenDictionary<string, string>> TextosEnEspanol = new(() => Cargar("es"));
    private static readonly Lazy<FrozenDictionary<string, string>> TextosEnIngles = new(() => Cargar("en"));

    private FrozenDictionary<string, string> _textos;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="Traductor"/>.
    /// </summary>
    /// <param name="idioma">Idioma inicial.</param>
    public Traductor(Idioma idioma = Idioma.Espanol)
    {
        _textos = TextosDe(idioma);
        Idioma = idioma == Idioma.Ingles ? Idioma.Ingles : Idioma.Espanol;
        Cultura = CulturaDe(Idioma);
    }

    /// <summary>
    /// Se produce cuando cambia el idioma.
    /// </summary>
    public event EventHandler? IdiomaCambiado;

    /// <summary>Obtiene el idioma activo.</summary>
    /// <value><see cref="Idioma.Espanol"/> o <see cref="Idioma.Ingles"/>.</value>
    public Idioma Idioma { get; private set; }

    /// <summary>Obtiene la cultura con la que se formatean números y fechas.</summary>
    /// <value><c>es-MX</c> o <c>en-US</c>; invariante si el proceso no tiene datos de globalización.</value>
    public CultureInfo Cultura { get; private set; }

    /// <summary>
    /// Obtiene el texto de una clave en el idioma activo.
    /// </summary>
    /// <param name="clave">Clave con puntos, por ejemplo <c>nav.inicio</c>.</param>
    /// <returns>El texto traducido.</returns>
    public string this[string clave] => Traducir(clave);

    /// <summary>
    /// Obtiene el texto de una clave en el idioma activo.
    /// </summary>
    /// <param name="clave">Clave con puntos.</param>
    /// <returns>El texto traducido, el español si falta en el idioma activo, o la clave.</returns>
    public string Traducir(string clave)
    {
        ArgumentNullException.ThrowIfNull(clave);

        return _textos.TryGetValue(clave, out string? texto) || TextosEnEspanol.Value.TryGetValue(clave, out texto)
            ? texto
            : clave;
    }

    /// <summary>
    /// Traduce una plantilla con marcadores <c>{0}</c>, <c>{1}</c>… y la rellena.
    /// </summary>
    /// <param name="clave">Clave de la plantilla.</param>
    /// <param name="argumentos">Valores de los marcadores.</param>
    /// <returns>El texto formateado con la cultura activa.</returns>
    public string Formato(string clave, params object?[] argumentos)
        => string.Format(Cultura, Traducir(clave), argumentos);

    /// <summary>
    /// Traduce el nombre de un valor de enumeración (clave <c>enum.Tipo.Valor</c>).
    /// </summary>
    /// <typeparam name="TEnum">Tipo de la enumeración.</typeparam>
    /// <param name="valor">Valor a describir.</param>
    /// <returns>El nombre para mostrar.</returns>
    public string Enumerado<TEnum>(TEnum valor)
        where TEnum : struct, Enum
        => Traducir(string.Create(CultureInfo.InvariantCulture, $"enum.{typeof(TEnum).Name}.{valor}"));

    /// <summary>
    /// Cambia el idioma activo y notifica a los suscriptores.
    /// </summary>
    /// <param name="idioma">Nuevo idioma.</param>
    public void Cambiar(Idioma idioma)
    {
        Idioma nuevo = idioma == Idioma.Ingles ? Idioma.Ingles : Idioma.Espanol;

        if (nuevo == Idioma)
        {
            return;
        }

        Idioma = nuevo;
        _textos = TextosDe(nuevo);
        Cultura = CulturaDe(nuevo);
        IdiomaCambiado?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Obtiene todas las claves definidas para un idioma.
    /// </summary>
    /// <param name="idioma">Idioma consultado.</param>
    /// <returns>Las claves.</returns>
    /// <remarks>Lo usan las pruebas para comprobar que ambos idiomas están completos.</remarks>
    public static IReadOnlyCollection<string> ClavesDe(Idioma idioma) => TextosDe(idioma).Keys;

    private static FrozenDictionary<string, string> TextosDe(Idioma idioma)
        => idioma == Idioma.Ingles ? TextosEnIngles.Value : TextosEnEspanol.Value;

    private static CultureInfo CulturaDe(Idioma idioma)
    {
        try
        {
            return CultureInfo.GetCultureInfo(idioma == Idioma.Ingles ? "en-US" : "es-MX");
        }
        catch (CultureNotFoundException)
        {
            // Procesos con globalización invariante (por ejemplo, un contenedor sin ICU).
            return CultureInfo.InvariantCulture;
        }
    }

    private static FrozenDictionary<string, string> Cargar(string codigo)
    {
        string recurso = $"HuimanNet.Textos.textos_{codigo}.json";

        using Stream flujo = typeof(Traductor).Assembly.GetManifestResourceStream(recurso)
            ?? throw new InvalidOperationException($"No se encontró el recurso de textos '{recurso}'.");

        byte[] contenido = new byte[flujo.Length];
        flujo.ReadExactly(contenido);

        var textos = new Dictionary<string, string>(StringComparer.Ordinal);
        var prefijos = new Stack<string>();
        string? propiedad = null;

        var lector = new Utf8JsonReader(contenido, new JsonReaderOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });

        while (lector.Read())
        {
            switch (lector.TokenType)
            {
                case JsonTokenType.PropertyName:
                    propiedad = lector.GetString();
                    break;

                case JsonTokenType.StartObject when propiedad is not null:
                    prefijos.Push(Componer(prefijos, propiedad));
                    propiedad = null;
                    break;

                case JsonTokenType.EndObject when prefijos.Count > 0:
                    prefijos.Pop();
                    break;

                case JsonTokenType.String when propiedad is not null:
                    textos[Componer(prefijos, propiedad)] = lector.GetString() ?? string.Empty;
                    propiedad = null;
                    break;
            }
        }

        return textos.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static string Componer(Stack<string> prefijos, string propiedad)
        => prefijos.Count == 0 ? propiedad : prefijos.Peek() + "." + propiedad;
}
