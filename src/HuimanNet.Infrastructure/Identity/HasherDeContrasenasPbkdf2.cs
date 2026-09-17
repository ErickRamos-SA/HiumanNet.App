using System.Globalization;
using System.Security.Cryptography;
using HuimanNet.Application.Interfaces;
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
    /// <summary>Etiqueta del algoritmo con la que empieza cada hash.</summary>
    private const string Algoritmo = "PBKDF2-SHA512";

    /// <summary>Longitud de la sal aleatoria, en bytes.</summary>
    private const int BytesDeSal = 16;

    /// <summary>Longitud del hash derivado, en bytes.</summary>
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
