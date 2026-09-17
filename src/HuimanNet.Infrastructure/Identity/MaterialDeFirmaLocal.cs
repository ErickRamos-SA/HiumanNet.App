using System.Security.Cryptography;
using System.Text;
using HuimanNet.Infrastructure.Configuracion;
using Microsoft.Extensions.Options;

namespace HuimanNet.Infrastructure.Identity;

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
