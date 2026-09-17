using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using HuimanNet.App.Localizacion;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace HuimanNet.App.Services;

/// <summary>Aviso de que el usuario inició sesión y su identidad ya se conoce.</summary>
/// <param name="Usuario">Identidad efectiva devuelta por la API.</param>
public sealed record SesionIniciadaMensaje(UsuarioActualDto Usuario);
