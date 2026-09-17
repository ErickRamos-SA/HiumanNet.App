namespace HuimanNet.Application.Inicio;

/// <summary>
/// Obtiene los indicadores del panel de inicio del usuario.
/// </summary>
/// <param name="EmpresaId">
/// Empresa elegida por un usuario de empresa cliente con varias empresas;
/// <c>null</c> para su empresa principal o, en los roles transversales, para todas.
/// </param>
public sealed record ObtenerResumenDeInicioQuery(Guid? EmpresaId = null);
