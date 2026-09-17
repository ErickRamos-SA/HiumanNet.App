using FluentAssertions;
using HuimanNet.Application.Interfaces;
using HuimanNet.Application.Usuarios;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HuimanNet.Application.Tests;

/// <summary>
/// Pruebas de la traducción de una identidad autenticada a un usuario del
/// portal: el enlace y el alta automática deben quedar en la bitácora.
/// </summary>
public sealed class ResolutorDeUsuarioAutenticadoTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IUsuarioRepository _usuarios = Substitute.For<IUsuarioRepository>();
    private readonly IAuditoriaRepository _auditoria = Substitute.For<IAuditoriaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public ResolutorDeUsuarioAutenticadoTests()
        => _unitOfWork.IniciarTransaccionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ITransaccion>()));

    [Fact]
    public async Task Resolver_UsuarioExternoConRol_SeDaDeAltaYQuedaEnLaBitacora()
    {
        Usuario usuario = await CrearResolutor().ResolverAsync(
            Identidad(RolUsuario.OperadorNomina), "10.0.0.1", TestContext.Current.CancellationToken);

        usuario.Rol.Should().Be(RolUsuario.OperadorNomina);
        await _usuarios.Received(1).AgregarAsync(usuario, Arg.Any<CancellationToken>());
        await _auditoria.Received(1).AgregarAsync(
            Arg.Is<RegistroAuditoria>(r =>
                r.Accion == AccionAuditada.AdministracionDeUsuario && r.UsuarioId == usuario.Id && r.RecursoId == usuario.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolver_UsuarioPreaprovisionado_SeEnlazaYQuedaEnLaBitacora()
    {
        Usuario preaprovisionado = Usuario.Crear(
            Usuario.PrefijoLocal + "ana@cliente.mx", "Ana Gómez", "ana@cliente.mx", RolUsuario.OperadorNomina, null, Ahora);

        _usuarios.ObtenerPorCorreoAsync("ana@cliente.mx", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Usuario?>(preaprovisionado));

        Usuario usuario = await CrearResolutor().ResolverAsync(
            Identidad(rol: null), null, TestContext.Current.CancellationToken);

        usuario.Should().BeSameAs(preaprovisionado);
        usuario.IdentificadorExterno.Should().Be("oid-ana");
        await _usuarios.Received(1).ActualizarAsync(preaprovisionado, Arg.Any<CancellationToken>());
        await _auditoria.Received(1).AgregarAsync(
            Arg.Is<RegistroAuditoria>(r => r.Accion == AccionAuditada.AdministracionDeUsuario),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolver_CuentaLocalDesconocida_EsRechazadaSinDarDeAlta()
    {
        Func<Task> accion = () => CrearResolutor().ResolverAsync(
            Identidad(RolUsuario.Administrador) with { EsCuentaExterna = false }, null, TestContext.Current.CancellationToken);

        await accion.Should().ThrowAsync<AccesoNoAutorizadoException>();
        await _usuarios.DidNotReceive().AgregarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolver_ClienteSinEmpresa_EsRechazado()
    {
        Func<Task> accion = () => CrearResolutor().ResolverAsync(
            Identidad(RolUsuario.ClienteEmpresa), null, TestContext.Current.CancellationToken);

        await accion.Should().ThrowAsync<AccesoNoAutorizadoException>();
        await _auditoria.DidNotReceive().AgregarAsync(Arg.Any<RegistroAuditoria>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolver_UsuarioDesactivado_EsRechazado()
    {
        Usuario existente = Usuario.Crear("oid-ana", "Ana Gómez", "ana@cliente.mx", RolUsuario.OperadorNomina, null, Ahora);
        existente.Desactivar();

        _usuarios.ObtenerPorIdentificadorExternoAsync("oid-ana", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Usuario?>(existente));

        Func<Task> accion = () => CrearResolutor().ResolverAsync(
            Identidad(RolUsuario.OperadorNomina), null, TestContext.Current.CancellationToken);

        await accion.Should().ThrowAsync<AccesoNoAutorizadoException>();
    }

    /// <summary>Identidad de Entra de prueba.</summary>
    /// <param name="rol">Rol de aplicación que trae el token, o <c>null</c>.</param>
    /// <returns>La identidad.</returns>
    private static IdentidadAutenticada Identidad(RolUsuario? rol)
        => new("oid-ana", EsCuentaExterna: true, "ana@cliente.mx", "Ana Gómez", rol, EmpresaIndicada: null);

    /// <summary>Crea el resolutor con los dobles de prueba.</summary>
    /// <returns>El resolutor.</returns>
    private ResolutorDeUsuarioAutenticado CrearResolutor()
        => new(_usuarios, _auditoria, _unitOfWork, TimeProvider.System, NullLogger<ResolutorDeUsuarioAutenticado>.Instance);
}
