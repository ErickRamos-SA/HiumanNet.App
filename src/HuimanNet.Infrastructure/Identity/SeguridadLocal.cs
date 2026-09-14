using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Infrastructure.Configuracion;
using Microsoft.Extensions.Options;

namespace HuimanNet.Infrastructure.Identity;

/// <summary>
/// Derivación de contraseñas con PBKDF2-SHA512 y sal aleatoria.
/// </summary>
/// <remarks>
/// Formato autocontenido: <c>PBKDF2-SHA512$iteraciones$salBase64$hashBase64</c>.
/// Guardar las iteraciones en el propio hash permite subir el factor de trabajo
/// sin invalidar las contraseñas existentes. La comparación es de tiempo constante.
/// </remarks>
public sealed class HasherDeContrasenasPbkdf2 : IHasherDeContrasenas
{
    private const string Algoritmo = "PBKDF2-SHA512";
    private const int BytesDeSal = 16;
    private const int BytesDeHash = 32;

    private readonly int _iteraciones;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="HasherDeContrasenasPbkdf2"/>.
    /// </summary>
    /// <param name="opciones">Opciones de identidad.</param>
    public HasherDeContrasenasPbkdf2(IOptions<OpcionesDeIdentidad> opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        _iteraciones = Math.Max(100_000, opciones.Value.IteracionesDeHash);
    }

    /// <inheritdoc/>
    public string Hashear(string contrasena)
    {
        ArgumentException.ThrowIfNullOrEmpty(contrasena);

        byte[] sal = RandomNumberGenerator.GetBytes(BytesDeSal);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, _iteraciones, HashAlgorithmName.SHA512, BytesDeHash);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Algoritmo}${_iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}");
    }

    /// <inheritdoc/>
    public bool Verificar(string contrasena, string hash)
    {
        if (string.IsNullOrEmpty(contrasena) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        string[] partes = hash.Split('$');

        if (partes.Length != 4 || partes[0] != Algoritmo
            || !int.TryParse(partes[1], NumberStyles.None, CultureInfo.InvariantCulture, out int iteraciones))
        {
            return false;
        }

        try
        {
            byte[] sal = Convert.FromBase64String(partes[2]);
            byte[] esperado = Convert.FromBase64String(partes[3]);
            byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, iteraciones, HashAlgorithmName.SHA512, esperado.Length);

            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

/// <summary>
/// Clave simétrica con la que se firman y validan los tokens del modo local.
/// </summary>
/// <remarks>
/// Se registra como singleton para que el emisor y la validación del
/// anfitrión usen exactamente los mismos bytes. Si la configuración no define
/// clave (desarrollo) se genera una aleatoria por proceso: los tokens dejan de
/// valer al reiniciar, que es aceptable fuera de producción.
/// </remarks>
public sealed class MaterialDeFirmaLocal
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="MaterialDeFirmaLocal"/>.
    /// </summary>
    /// <param name="opciones">Opciones de identidad.</param>
    public MaterialDeFirmaLocal(IOptions<OpcionesDeIdentidad> opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        string clave = opciones.Value.ClaveDeFirmaLocal;

        Clave = string.IsNullOrWhiteSpace(clave)
            ? RandomNumberGenerator.GetBytes(64)
            : SHA512.HashData(Encoding.UTF8.GetBytes(clave));
    }

    /// <summary>Obtiene los bytes de la clave HMAC.</summary>
    /// <value>64 bytes.</value>
    public byte[] Clave { get; }
}

/// <summary>
/// Emite tokens JWT firmados con HMAC-SHA256 para los usuarios del modo local.
/// </summary>
/// <remarks>
/// Se escribe el token a mano con <see cref="Utf8JsonWriter"/> en lugar de
/// usar un manejador de tokens genérico: es trivial, no usa reflexión y el
/// resultado lo valida el middleware estándar <c>JwtBearer</c> de la API.
/// El token lleva sólo la identidad; el rol y los permisos se vuelven a leer de
/// la base de datos en cada petición, de modo que un cambio o una baja surten
/// efecto de inmediato.
/// </remarks>
public sealed class EmisorDeTokensLocal : IEmisorDeTokens
{
    private static readonly byte[] Cabecera = Encoding.UTF8.GetBytes("""{"alg":"HS256","typ":"JWT"}""");

    private readonly MaterialDeFirmaLocal _material;
    private readonly OpcionesDeIdentidad _opciones;
    private readonly TimeProvider _reloj;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="EmisorDeTokensLocal"/>.
    /// </summary>
    /// <param name="material">Clave de firma.</param>
    /// <param name="opciones">Opciones de identidad.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    public EmisorDeTokensLocal(MaterialDeFirmaLocal material, IOptions<OpcionesDeIdentidad> opciones, TimeProvider reloj)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        _material = material;
        _opciones = opciones.Value;
        _reloj = reloj;
    }

    /// <inheritdoc/>
    public TokenEmitido Emitir(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        DateTimeOffset ahora = _reloj.GetUtcNow();
        DateTimeOffset expira = ahora.AddMinutes(Math.Max(5, _opciones.MinutosVigenciaTokenLocal));

        var carga = new ArrayBufferWriter<byte>(512);

        using (var escritor = new Utf8JsonWriter(carga))
        {
            escritor.WriteStartObject();
            escritor.WriteString("sub", usuario.IdentificadorExterno);
            escritor.WriteString("name", usuario.NombreCompleto);
            escritor.WriteString("preferred_username", usuario.Correo);
            escritor.WriteString("roles", usuario.Rol.ToString());
            escritor.WriteString("iss", _opciones.EmisorLocal);
            escritor.WriteString("aud", _opciones.AudienciaLocal);
            escritor.WriteString("jti", Guid.NewGuid().ToString("N"));
            escritor.WriteNumber("iat", ahora.ToUnixTimeSeconds());
            escritor.WriteNumber("nbf", ahora.ToUnixTimeSeconds());
            escritor.WriteNumber("exp", expira.ToUnixTimeSeconds());
            escritor.WriteEndObject();
        }

        string firmable = Base64Url.EncodeToString(Cabecera) + "." + Base64Url.EncodeToString(carga.WrittenSpan);
        byte[] firma = HMACSHA256.HashData(_material.Clave, Encoding.ASCII.GetBytes(firmable));

        return new TokenEmitido(firmable + "." + Base64Url.EncodeToString(firma), expira);
    }
}
