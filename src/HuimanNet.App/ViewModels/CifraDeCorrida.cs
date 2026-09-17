using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HuimanNet.App.Services;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Domain.Enums;

namespace HuimanNet.App.ViewModels;

/// <summary>Cifra del resumen de una corrida.</summary>
/// <param name="Etiqueta">Texto traducido.</param>
/// <param name="Valor">Importe.</param>
public sealed record CifraDeCorrida(string Etiqueta, decimal Valor);
