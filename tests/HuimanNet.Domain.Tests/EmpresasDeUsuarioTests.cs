using FluentAssertions;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Services;
using HuimanNet.Domain.ValueObjects;
using Xunit;

namespace HuimanNet.Domain.Tests;

/// <summary>
/// Usuarios de empresa cliente con una o varias empresas: operan en cualquiera
/// de las suyas y en ninguna otra.
/// </summary>
public sealed class EmpresasDeUsuarioTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PoliticaDeAcceso _politica = new();
    private readonly Guid _principal = Guid.CreateVersion7();
    private readonly Guid _adicional = Guid.CreateVersion7();

    [Fact]
    public void Usuario_EmpresasAdicionales_NoRepitenLaPrincipalYSeVacianAlSerTransversal()
    {
        Usuario usuario = Usuario.Crear("local:cliente@x.mx", "Cliente", "cliente@x.mx", RolUsuario.ClienteEmpresa, _principal, Ahora);

        usuario.AsignarEmpresasAdicionales([_adicional, _principal, _adicional]);

        usuario.EmpresasAdicionales.Should().Equal(_adicional);
        usuario.Empresas.Should().Equal(_principal, _adicional);
        usuario.PerteneceA(_adicional).Should().BeTrue();

        usuario.AsignarRol(RolUsuario.OperadorNomina, null);

        usuario.Empresas.Should().BeEmpty();
        usuario.EmpresasAdicionales.Should().BeEmpty();
    }

    [Fact]
    public void ResolverEmpresaObjetivo_ClienteSinIndicarEmpresa_UsaLaPrincipal()
        => _politica.ResolverEmpresaObjetivo(RolUsuario.ClienteEmpresa, _principal, [_principal, _adicional], null)
            .Should().Be(_principal);

    [Fact]
    public void ResolverEmpresaObjetivo_ClienteQuePideUnaDeSusEmpresas_LaObtiene()
        => _politica.ResolverEmpresaObjetivo(RolUsuario.ClienteEmpresa, _principal, [_principal, _adicional], _adicional)
            .Should().Be(_adicional);

    [Fact]
    public void ResolverEmpresaObjetivo_ClienteQuePideUnaEmpresaAjena_EsRechazado()
    {
        Action accion = () => _politica.ResolverEmpresaObjetivo(
            RolUsuario.ClienteEmpresa, _principal, [_principal, _adicional], Guid.CreateVersion7());

        accion.Should().Throw<AccesoNoAutorizadoException>();
    }

    [Fact]
    public void GarantizarPuedeDescargar_DocumentoDeUnaEmpresaAdicional_NoLanza()
    {
        Action accion = () => _politica.GarantizarPuedeDescargar(
            RolUsuario.ClienteEmpresa, [_principal, _adicional], DocumentoDisponible(_adicional));

        accion.Should().NotThrow();
    }

    [Fact]
    public void GarantizarPuedeDescargar_DocumentoDeUnaEmpresaAjena_EsRechazado()
    {
        Action accion = () => _politica.GarantizarPuedeDescargar(
            RolUsuario.ClienteEmpresa, [_principal, _adicional], DocumentoDisponible(Guid.CreateVersion7()));

        accion.Should().Throw<AccesoNoAutorizadoException>();
    }

    private static Documento DocumentoDisponible(Guid empresaId)
    {
        Documento documento = Documento.Solicitar(
            empresaId,
            Guid.CreateVersion7(),
            TipoDocumento.Resultado,
            NombreArchivo.Crear("resultado.xlsx"),
            TamanoArchivo.DesdeMegabytes(1),
            Guid.CreateVersion7(),
            Ahora);

        documento.ConfirmarCarga(
            TamanoArchivo.DesdeMegabytes(1),
            HuellaArchivo.Crear(new string('a', HuellaArchivo.LongitudSha256Hex)),
            Ahora);

        documento.MarcarDisponible(Ahora);
        return documento;
    }
}
