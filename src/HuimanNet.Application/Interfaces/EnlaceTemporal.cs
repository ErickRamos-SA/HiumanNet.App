namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Enlace temporal de acceso directo a un blob.
/// </summary>
/// <param name="Url">URL firmada (SAS) con alcance de un único blob.</param>
/// <param name="ExpiraEn">Instante en que la firma deja de ser válida, en UTC.</param>
public sealed record EnlaceTemporal(Uri Url, DateTimeOffset ExpiraEn);
