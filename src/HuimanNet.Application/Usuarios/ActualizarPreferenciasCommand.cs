using HuimanNet.Domain.Enums;

namespace HuimanNet.Application.Usuarios;

/// <summary>Actualiza las preferencias del propio usuario.</summary>
/// <param name="Idioma">Idioma preferido.</param>
public sealed record ActualizarPreferenciasCommand(Idioma Idioma);
