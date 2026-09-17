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

/// <summary>Opción del menú «Más».</summary>
/// <param name="Titulo">Título traducido.</param>
/// <param name="Descripcion">Descripción traducida.</param>
/// <param name="Ruta">Ruta de Shell.</param>
public sealed record OpcionDeMenu(string Titulo, string Descripcion, string Ruta);
