using System.Text.Json;
using FluentAssertions;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Contracts.Serialization;
using HuimanNet.Domain.Enums;
using Xunit;

namespace HuimanNet.Api.Tests;

/// <summary>
/// Protege el contrato HTTP: si un DTO cambia de forma o deja de estar
/// declarado en el contexto de serialización, la publicación con Native AOT
/// fallaría en tiempo de ejecución, no de compilación. Estas pruebas convierten
/// ese fallo en un error de CI.
/// </summary>
public sealed class ContratoJsonTests
{
    [Fact]
    public void SolicitarCargaRequest_SeSerializaEnCamelCaseYConEnumeradosComoTexto()
    {
        var peticion = new SolicitarCargaRequest(
            Guid.Parse("0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b"),
            TipoDocumento.Incidencia,
            "incidencias.xlsx",
            2048);

        string json = JsonSerializer.Serialize(
            peticion, HuimanNetJsonContext.Default.SolicitarCargaRequest);

        json.Should().Contain("\"periodoId\"");
        json.Should().Contain("\"tipo\":\"Incidencia\"");
        json.Should().Contain("\"nombreArchivo\":\"incidencias.xlsx\"");
    }

    [Fact]
    public void SolicitarCargaResponse_IdaYVuelta_ConservaLosValores()
    {
        var original = new SolicitarCargaResponse(
            Guid.CreateVersion7(),
            new Uri("https://cuenta.blob.core.windows.net/documentos/a?sig=b"),
            new DateTimeOffset(2026, 8, 1, 12, 15, 0, TimeSpan.Zero));

        string json = JsonSerializer.Serialize(
            original, HuimanNetJsonContext.Default.SolicitarCargaResponse);

        SolicitarCargaResponse? recuperado = JsonSerializer.Deserialize(
            json, HuimanNetJsonContext.Default.SolicitarCargaResponse);

        recuperado.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void ListaDeDocumentos_EstaDeclaradaEnElContexto()
    {
        IReadOnlyList<DocumentoDto> documentos =
        [
            new(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                TipoDocumento.Resultado,
                "resultado.xlsx",
                4096,
                EstadoDocumento.Disponible,
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch,
                "Ana Gómez",
                EsDescargable: true),
        ];

        string json = JsonSerializer.Serialize(
            documentos, HuimanNetJsonContext.Default.IReadOnlyListDocumentoDto);

        json.Should().Contain("\"estado\":\"Disponible\"");
    }
}
