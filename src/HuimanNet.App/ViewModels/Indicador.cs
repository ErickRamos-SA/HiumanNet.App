using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HuimanNet.App.Localizacion;
using HuimanNet.App.Services;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Enums;

namespace HuimanNet.App.ViewModels;

/// <summary>Indicador numérico del inicio.</summary>
/// <param name="Etiqueta">Texto traducido.</param>
/// <param name="Valor">Valor.</param>
public sealed record Indicador(string Etiqueta, int Valor);
