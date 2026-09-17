using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Domain.Enums;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Xaml;

namespace HuimanNet.App.Localizacion;

/// <summary>
/// Aviso de que el usuario cambió el idioma de la interfaz.
/// </summary>
/// <param name="Idioma">Idioma nuevo.</param>
public sealed record IdiomaCambiadoMensaje(Idioma Idioma);
