using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using HuimanNet.App.Localizacion;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace HuimanNet.App.Services;

/// <summary>Aviso de que cambió la empresa de trabajo.</summary>
/// <param name="EmpresaId">Empresa elegida, o <c>null</c>.</param>
public sealed record EmpresaCambiadaMensaje(Guid? EmpresaId);
