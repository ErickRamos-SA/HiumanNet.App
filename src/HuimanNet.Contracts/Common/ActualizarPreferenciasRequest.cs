using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Common;

/// <summary>
/// Preferencias de interfaz del usuario.
/// </summary>
/// <param name="Idioma">Idioma preferido.</param>
public sealed record ActualizarPreferenciasRequest(Idioma Idioma);
