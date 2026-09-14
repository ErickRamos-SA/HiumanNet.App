using System.Text.Json.Serialization;
using HuimanNet.Application.Interfaces;

namespace HuimanNet.Infrastructure.Notifications;

/// <summary>
/// Contexto de serialización generado en compilación para los mensajes de la
/// cola de avisos.
/// </summary>
/// <remarks>
/// Igual que en el contrato HTTP, la serialización se genera en compilación para
/// que la API pueda publicarse con Native AOT (ESPECIFICACION.md §6).
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AvisoPendiente))]
public sealed partial class AvisosJsonContext : JsonSerializerContext;
