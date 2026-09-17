namespace HuimanNet.Application.Nomina;

/// <summary>
/// Lista las corridas más recientes del ámbito del solicitante.
/// </summary>
/// <param name="EmpresaId">
/// Empresa elegida; <c>null</c> para todas (roles transversales) o para la
/// principal (empresa cliente).
/// </param>
/// <param name="Limite">Número máximo de corridas.</param>
public sealed record ListarCorridasRecientesQuery(Guid? EmpresaId, int Limite);
