using System.Buffers;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Infrastructure.Configuracion;
using Microsoft.Extensions.Options;

namespace HuimanNet.Infrastructure.Identity;

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
