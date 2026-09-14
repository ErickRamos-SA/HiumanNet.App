using FluentAssertions;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Services;
using HuimanNet.Domain.ValueObjects;
using Xunit;

namespace HuimanNet.Domain.Tests;

/// <summary>
/// Pruebas del aislamiento entre empresas: el riesgo número uno del portal.
/// </summary>
public sealed class PoliticaDeAccesoTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PoliticaDeAcceso _politica = new();

    [Fact]
    public void ResolverEmpresaObjetivo_ClienteQueSolicitaOtraEmpresa_EsRechazado()
    {
        Guid empresaPropia = Guid.CreateVersion7();
        Guid empresaAjena = Guid.CreateVersion7();

        Action accion = () => _politica.ResolverEmpresaObjetivo(
            RolUsuario.ClienteEmpresa, empresaPropia, empresaAjena);

        accion.Should().Throw<AccesoNoAutorizadoException>();
    }

    [Fact]
    public void ResolverEmpresaObjetivo_ClienteSinIndicarEmpresa_UsaLaDelToken()
    {
        Guid empresaPropia = Guid.CreateVersion7();

        Guid resultado = _politica.ResolverEmpresaObjetivo(
            RolUsuario.ClienteEmpresa, empresaPropia, empresaSolicitada: null);

        resultado.Should().Be(empresaPropia);
    }

    [Fact]
    public void ResolverEmpresaObjetivo_OperadorSinIndicarEmpresa_EsRechazado()
    {
        Action accion = () => _politica.ResolverEmpresaObjetivo(
            RolUsuario.OperadorNomina, empresaDelUsuario: null, empresaSolicitada: null);

        accion.Should().Throw<AccesoNoAutorizadoException>();
    }

    [Theory]
    [InlineData(RolUsuario.ClienteEmpresa, TipoDocumento.Incidencia, true)]
    [InlineData(RolUsuario.ClienteEmpresa, TipoDocumento.Resultado, false)]
    [InlineData(RolUsuario.OperadorNomina, TipoDocumento.Resultado, true)]
    [InlineData(RolUsuario.OperadorNomina, TipoDocumento.Incidencia, false)]
    [InlineData(RolUsuario.Administrador, TipoDocumento.Incidencia, false)]
    public void PuedeCargar_AplicaLaMatrizDePermisos(
        RolUsuario rol, TipoDocumento tipo, bool esperado)
        => _politica.PuedeCargar(rol, tipo).Should().Be(esperado);

    [Fact]
    public void GarantizarPuedeDescargar_DocumentoDeOtraEmpresa_EsRechazado()
    {
        Documento documento = CrearDocumentoDisponible(Guid.CreateVersion7());

        Action accion = () => _politica.GarantizarPuedeDescargar(
            RolUsuario.ClienteEmpresa, Guid.CreateVersion7(), documento);

        accion.Should().Throw<AccesoNoAutorizadoException>();
    }

    [Fact]
    public void GarantizarPuedeDescargar_DocumentoSinEscanear_EsRechazado()
    {
        Guid empresaId = Guid.CreateVersion7();
        Documento documento = CrearDocumentoPendiente(empresaId);

        Action accion = () => _politica.GarantizarPuedeDescargar(
            RolUsuario.ClienteEmpresa, empresaId, documento);

        accion.Should().Throw<DocumentoInvalidoException>();
    }

    [Fact]
    public void GarantizarPuedeDescargar_DocumentoPropioYDisponible_NoLanza()
    {
        Guid empresaId = Guid.CreateVersion7();
        Documento documento = CrearDocumentoDisponible(empresaId);

        Action accion = () => _politica.GarantizarPuedeDescargar(
            RolUsuario.ClienteEmpresa, empresaId, documento);

        accion.Should().NotThrow();
    }

    private static Documento CrearDocumentoPendiente(Guid empresaId)
        => Documento.Solicitar(
            empresaId,
            Guid.CreateVersion7(),
            TipoDocumento.Resultado,
            NombreArchivo.Crear("resultado.xlsx"),
            TamanoArchivo.DesdeMegabytes(1),
            Guid.CreateVersion7(),
            Ahora);

    private static Documento CrearDocumentoDisponible(Guid empresaId)
    {
        Documento documento = CrearDocumentoPendiente(empresaId);

        documento.ConfirmarCarga(
            TamanoArchivo.DesdeMegabytes(1),
            HuellaArchivo.Crear(new string('a', HuellaArchivo.LongitudSha256Hex)),
            Ahora);

        documento.MarcarDisponible(Ahora);

        return documento;
    }
}
