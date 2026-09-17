namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Token de acceso emitido para un usuario local.
/// </summary>
/// <param name="Token">Token compacto.</param>
/// <param name="ExpiraEn">Instante de caducidad, en UTC.</param>
public sealed record TokenEmitido(string Token, DateTimeOffset ExpiraEn);
