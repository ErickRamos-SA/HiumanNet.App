using FluentAssertions;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Services;
using HuimanNet.Domain.ValueObjects;
using Xunit;

namespace HuimanNet.Domain.Tests;

public sealed class ValidadorDeDocumentoTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly ValidadorDeDocumento _validador = new(
        new PoliticaDeCarga([".xlsx", ".csv"], TamanoArchivo.DesdeMegabytes(10)));

    [Fact]
    public void Validar_ConArchivoPermitido_NoLanza()
    {
        PeriodoCarga periodo = CrearPeriodoAbierto();

        Action accion = () => _validador.Validar(
            NombreArchivo.Crear("incidencias.xlsx"),
            TamanoArchivo.DesdeMegabytes(2),
            TipoDocumento.Incidencia,
            periodo);

        accion.Should().NotThrow();
    }

    [Fact]
    public void Validar_ConExtensionNoPermitida_LanzaDocumentoInvalido()
    {
        PeriodoCarga periodo = CrearPeriodoAbierto();

        Action accion = () => _validador.Validar(
            NombreArchivo.Crear("script.exe"),
            TamanoArchivo.DesdeMegabytes(1),
            TipoDocumento.Incidencia,
            periodo);

        accion.Should().Throw<DocumentoInvalidoException>()
            .WithMessage("*.exe*");
    }

    [Fact]
    public void Validar_ConTamanoExcesivo_LanzaDocumentoInvalido()
    {
        PeriodoCarga periodo = CrearPeriodoAbierto();

        Action accion = () => _validador.Validar(
            NombreArchivo.Crear("incidencias.xlsx"),
            TamanoArchivo.DesdeMegabytes(11),
            TipoDocumento.Incidencia,
            periodo);

        accion.Should().Throw<DocumentoInvalidoException>();
    }

    [Fact]
    public void Validar_ConPeriodoEnProceso_RechazaCargaDelCliente()
    {
        PeriodoCarga periodo = CrearPeriodoAbierto();
        periodo.CambiarEstado(EstadoPeriodo.EnProceso, Ahora);

        Action accion = () => _validador.Validar(
            NombreArchivo.Crear("incidencias.xlsx"),
            TamanoArchivo.DesdeMegabytes(1),
            TipoDocumento.Incidencia,
            periodo);

        accion.Should().Throw<PeriodoCerradoException>();
    }

    [Fact]
    public void Validar_ConPeriodoEnProceso_AdmiteResultadoDelOperador()
    {
        PeriodoCarga periodo = CrearPeriodoAbierto();
        periodo.CambiarEstado(EstadoPeriodo.EnProceso, Ahora);

        Action accion = () => _validador.Validar(
            NombreArchivo.Crear("resultado.csv"),
            TamanoArchivo.DesdeMegabytes(1),
            TipoDocumento.Resultado,
            periodo);

        accion.Should().NotThrow();
    }

    [Theory]
    [InlineData("../secreto.xlsx")]
    [InlineData("carpeta/archivo.xlsx")]
    [InlineData("archivo-sin-extension")]
    public void NombreArchivo_ConValorPeligroso_EsRechazado(string valor)
    {
        Action accion = () => NombreArchivo.Crear(valor);

        accion.Should().Throw<DocumentoInvalidoException>();
    }

    private static PeriodoCarga CrearPeriodoAbierto()
        => PeriodoCarga.Abrir(
            Guid.CreateVersion7(),
            PeriodoCalendario.Crear(2026, 8, 1),
            "Primera quincena de agosto",
            Ahora);
}
