namespace HuimanNet.Application.Common;

/// <summary>
/// Error concreto detectado al validar la entrada de un caso de uso.
/// </summary>
/// <param name="Campo">Nombre del campo que incumple la regla.</param>
/// <param name="Mensaje">Descripción del incumplimiento, apta para mostrarse al usuario.</param>
public sealed record ErrorDeValidacion(string Campo, string Mensaje);
