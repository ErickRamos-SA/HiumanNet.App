using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using HuimanNet.App.Localizacion;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace HuimanNet.App.Services;

/// <summary>
/// Token del modo de identidad local, guardado en el almacén seguro del sistema
/// (Keystore en Android, Keychain en iOS).
/// </summary>
public sealed class ProveedorDeTokenLocal
{
    /// <summary>Clave del almacén seguro donde se guarda el token.</summary>
    private const string ClaveToken = "huimannet.token";

    /// <summary>Clave del almacén seguro donde se guarda la caducidad del token, en segundos Unix.</summary>
    private const string ClaveExpiracion = "huimannet.token.expira";

    private string? _token;
    private DateTimeOffset _expira = DateTimeOffset.MinValue;
    private bool _cargado;

    /// <summary>
    /// Obtiene el token vigente.
    /// </summary>
    /// <returns>El token, o <c>null</c> si no hay o está por caducar.</returns>
    public async Task<string?> ObtenerAsync()
    {
        if (!_cargado)
        {
            _token = await SecureStorage.Default.GetAsync(ClaveToken);
            string? expira = await SecureStorage.Default.GetAsync(ClaveExpiracion);
            _expira = long.TryParse(expira, NumberStyles.None, CultureInfo.InvariantCulture, out long segundos)
                ? DateTimeOffset.FromUnixTimeSeconds(segundos)
                : DateTimeOffset.MinValue;
            _cargado = true;
        }

        return _token is not null && _expira > DateTimeOffset.UtcNow.AddMinutes(1) ? _token : null;
    }

    /// <summary>
    /// Guarda el token emitido por la API.
    /// </summary>
    /// <param name="token">Token compacto.</param>
    /// <param name="expira">Instante de caducidad.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public async Task EstablecerAsync(string token, DateTimeOffset expira)
    {
        _token = token;
        _expira = expira;
        _cargado = true;

        await SecureStorage.Default.SetAsync(ClaveToken, token);
        await SecureStorage.Default.SetAsync(ClaveExpiracion, expira.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>Borra el token del dispositivo.</summary>
    public void Borrar()
    {
        _token = null;
        _expira = DateTimeOffset.MinValue;
        _cargado = true;
        SecureStorage.Default.Remove(ClaveToken);
        SecureStorage.Default.Remove(ClaveExpiracion);
    }
}
