namespace HuimanNet.Contracts.Common;

/// <summary>
/// Resultado de un inicio de sesión local.
/// </summary>
/// <param name="Token">Token de acceso para las siguientes peticiones.</param>
/// <param name="ExpiraEn">Instante de caducidad del token, en UTC.</param>
/// <param name="Usuario">Identidad efectiva del usuario.</param>
public sealed record IniciarSesionResponse(string Token, DateTimeOffset ExpiraEn, UsuarioActualDto Usuario);
