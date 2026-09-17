using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using HuimanNet.App.Localizacion;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace HuimanNet.App.Services;

/// <summary>
/// <see cref="IProveedorDeToken"/> que delega en Microsoft Entra o en el token
/// local según el modo de identidad del servidor.
/// </summary>
public sealed class ProveedorDeToken : IProveedorDeToken
{
    private readonly SesionDeLaApp _sesion;
    private readonly ProveedorDeTokenLocal _local;
    private readonly IServiceProvider _servicios;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ProveedorDeToken"/>.
    /// </summary>
    /// <param name="sesion">Estado de la sesión.</param>
    /// <param name="local">Token del modo local.</param>
    /// <param name="servicios">Contenedor, para crear el cliente de Entra sólo si hace falta.</param>
    public ProveedorDeToken(SesionDeLaApp sesion, ProveedorDeTokenLocal local, IServiceProvider servicios)
    {
        _sesion = sesion;
        _local = local;
        _servicios = servicios;
    }

    /// <summary>Obtiene el proveedor de Microsoft Entra.</summary>
    /// <value>Se resuelve al usarlo, para no crear el cliente de Entra en modo local.</value>
    private ProveedorDeTokenEntra Entra =>_servicios.GetRequiredService<ProveedorDeTokenEntra>();

    /// <inheritdoc/>
    public Task<string?> ObtenerTokenSilenciosoAsync(CancellationToken cancellationToken = default)
        => _sesion.EsLocal ? _local.ObtenerAsync() : Entra.ObtenerTokenSilenciosoAsync(cancellationToken);

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// En modo local, si el token caducó: el usuario debe volver a escribir su contraseña.
    /// </exception>
    public Task<string> IniciarSesionAsync(CancellationToken cancellationToken = default)
    {
        if (_sesion.EsLocal)
        {
            _local.Borrar();
            throw new InvalidOperationException(Textos.Traductor["movil.sesionExpirada"]);
        }

        return Entra.IniciarSesionAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task CerrarSesionAsync(CancellationToken cancellationToken = default)
    {
        _local.Borrar();

        if (!_sesion.EsLocal)
        {
            await Entra.CerrarSesionAsync(cancellationToken);
        }
    }
}
