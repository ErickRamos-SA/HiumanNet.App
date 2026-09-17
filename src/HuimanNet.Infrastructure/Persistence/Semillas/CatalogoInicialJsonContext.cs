using System.Text.Json;
using System.Text.Json.Serialization;

namespace HuimanNet.Infrastructure.Persistence.Semillas;

/// <summary>
/// Contexto de serialización generado en compilación para el catálogo inicial.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(CatalogoInicial))]
internal sealed partial class CatalogoInicialJsonContext : JsonSerializerContext;
