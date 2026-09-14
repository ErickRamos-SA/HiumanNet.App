using FluentAssertions;
using HuimanNet.Application.Documentos.Commands;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;
using HuimanNet.Domain.Services;
using HuimanNet.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HuimanNet.Application.Tests;

public sealed class SolicitarCargaDocumentoHandlerTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Uri UrlFirmada = new("https://cuenta.blob.core.windows.net/documentos/x?sig=y");

    private readonly IDocumentoRepository _documentos = Substitute.For<IDocumentoRepository>();
    private readonly IPeriodoRepository _periodos = Substitute.For<IPeriodoRepository>();
    private readonly IAuditoriaRepository _auditoria = Substitute.For<IAuditoriaRepository>();
    private readonly IAlmacenDocumentos _almacen = Substitute.For<IAlmacenDocumentos>();
    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly Guid _empresaId = Guid.CreateVersion7();

    public SolicitarCargaDocumentoHandlerTests()
    {
        _usuarioActual.UsuarioId.Returns(Guid.CreateVersion7());
        _usuarioActual.NombreCompleto.Returns("Ana Gómez");
        _usuarioActual.Rol.Returns(RolUsuario.ClienteEmpresa);
        _usuarioActual.EmpresaId.Returns(_empresaId);

        _unitOfWork.IniciarTransaccionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ITransaccion>()));

        _almacen.CrearEnlaceDeEscrituraAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EnlaceTemporal(UrlFirmada, Ahora.AddMinutes(15))));
    }

    [Fact]
    public async Task Ejecutar_ConPeriodoAbierto_ReservaElDocumentoYDevuelveLaUrl()
    {
        PeriodoCarga periodo = DadoUnPeriodoAbierto();
        SolicitarCargaDocumentoHandler manejador = CrearManejador();

        SolicitarCargaResponse respuesta = await manejador.EjecutarAsync(
            new SolicitarCargaDocumentoCommand(
                periodo.Id, TipoDocumento.Incidencia, "incidencias.xlsx", 2048, EmpresaId: null),
            TestContext.Current.CancellationToken);

        respuesta.UrlCarga.Should().Be(UrlFirmada);
        await _documentos.Received(1).AgregarAsync(
            Arg.Is<Documento>(d =>
                d != null && d.EmpresaId == _empresaId && d.Estado == EstadoDocumento.Pendiente),
            Arg.Any<CancellationToken>());
        await _auditoria.Received(1).AgregarAsync(
            Arg.Any<RegistroAuditoria>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ejecutar_ConTipoQueElRolNoPuedeCargar_EsRechazado()
    {
        DadoUnPeriodoAbierto();
        SolicitarCargaDocumentoHandler manejador = CrearManejador();

        Func<Task> accion = () => manejador.EjecutarAsync(new SolicitarCargaDocumentoCommand(
            Guid.CreateVersion7(), TipoDocumento.Resultado, "resultado.xlsx", 2048, EmpresaId: null));

        await accion.Should().ThrowAsync<AccesoNoAutorizadoException>();
        await _almacen.DidNotReceive().CrearEnlaceDeEscrituraAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ejecutar_ConPeriodoDeOtraEmpresa_NoEmiteFirma()
    {
        _periodos.ObtenerPorIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PeriodoCarga?>(null));

        SolicitarCargaDocumentoHandler manejador = CrearManejador();

        Func<Task> accion = () => manejador.EjecutarAsync(new SolicitarCargaDocumentoCommand(
            Guid.CreateVersion7(), TipoDocumento.Incidencia, "incidencias.xlsx", 2048, EmpresaId: null));

        await accion.Should().ThrowAsync<AccesoNoAutorizadoException>();
        await _almacen.DidNotReceive().CrearEnlaceDeEscrituraAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ejecutar_ConExtensionNoPermitida_NoEmiteFirma()
    {
        PeriodoCarga periodo = DadoUnPeriodoAbierto();
        SolicitarCargaDocumentoHandler manejador = CrearManejador();

        Func<Task> accion = () => manejador.EjecutarAsync(new SolicitarCargaDocumentoCommand(
            periodo.Id, TipoDocumento.Incidencia, "malware.exe", 2048, EmpresaId: null));

        await accion.Should().ThrowAsync<DocumentoInvalidoException>();
        await _almacen.DidNotReceive().CrearEnlaceDeEscrituraAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private PeriodoCarga DadoUnPeriodoAbierto()
    {
        PeriodoCarga periodo = PeriodoCarga.Abrir(
            _empresaId, PeriodoCalendario.Crear(2026, 8, 1), "Primera quincena", Ahora);

        _periodos.ObtenerPorIdAsync(periodo.Id, _empresaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PeriodoCarga?>(periodo));

        return periodo;
    }

    private SolicitarCargaDocumentoHandler CrearManejador()
        => new(
            _documentos,
            _periodos,
            _auditoria,
            _almacen,
            _usuarioActual,
            _unitOfWork,
            new PoliticaDeAcceso(),
            new ValidadorDeDocumento(PoliticaDeCarga.Predeterminada),
            new ProveedorDeTiempoFijo(Ahora),
            NullLogger<SolicitarCargaDocumentoHandler>.Instance);

    /// <summary>
    /// Reloj determinista para las pruebas.
    /// </summary>
    private sealed class ProveedorDeTiempoFijo(DateTimeOffset instante) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instante;
    }
}
