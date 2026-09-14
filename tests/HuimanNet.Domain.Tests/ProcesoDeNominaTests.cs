using FluentAssertions;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Services;
using HuimanNet.Domain.ValueObjects;
using Xunit;

namespace HuimanNet.Domain.Tests;

/// <summary>
/// Reglas del proceso de nómina por período: quién calcula, cuándo se puede
/// calcular y qué ocurre al reprocesar.
/// </summary>
public sealed class ProcesoDeNominaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(AccionDelSistema.CalcularNomina)]
    [InlineData(AccionDelSistema.CotejarNomina)]
    [InlineData(AccionDelSistema.AprobarNomina)]
    [InlineData(AccionDelSistema.PublicarResultados)]
    [InlineData(AccionDelSistema.GestionarPeriodos)]
    [InlineData(AccionDelSistema.AdministrarUsuarios)]
    public void ClienteEmpresa_NoRecibeAccionesDeNominaNiConPermisoPersonalizado(AccionDelSistema accion)
    {
        PermisosPorRol.PuedeConcederse(RolUsuario.ClienteEmpresa, accion).Should().BeFalse();

        IReadOnlySet<AccionDelSistema> efectivas = PermisosPorRol.Efectivas(
            RolUsuario.ClienteEmpresa, [new PermisoDeUsuario(accion, true)]);

        efectivas.Should().NotContain(accion);
    }

    [Fact]
    public void ClienteEmpresa_ConsultaLaNominaYAdmiteAccionesNoExclusivas()
    {
        IReadOnlySet<AccionDelSistema> efectivas = PermisosPorRol.Efectivas(
            RolUsuario.ClienteEmpresa, [new PermisoDeUsuario(AccionDelSistema.ConsultarExplicacionDeCalculos, true)]);

        efectivas.Should().Contain(AccionDelSistema.ConsultarNomina)
            .And.Contain(AccionDelSistema.ConsultarExplicacionDeCalculos)
            .And.NotContain(AccionDelSistema.CalcularNomina);
    }

    [Theory]
    [InlineData(RolUsuario.OperadorNomina)]
    [InlineData(RolUsuario.Administrador)]
    public void RolesDeNomina_CalculanPorOmision(RolUsuario rol)
        => PermisosPorRol.Efectivas(rol, null).Should().Contain(AccionDelSistema.CalcularNomina);

    [Theory]
    [InlineData(EstadoPeriodo.Abierto, false)]
    [InlineData(EstadoPeriodo.Recibido, true)]
    [InlineData(EstadoPeriodo.EnProceso, true)]
    [InlineData(EstadoPeriodo.ResultadosDisponibles, true)]
    [InlineData(EstadoPeriodo.Cerrado, false)]
    public void Periodo_AdmiteCalculoSoloConArchivosYSinCerrar(EstadoPeriodo estado, bool esperado)
    {
        PeriodoCarga periodo = Periodo(estado);

        periodo.AdmiteCalculo.Should().Be(esperado);

        Action accion = periodo.GarantizarQueAdmiteCalculo;

        if (esperado)
        {
            accion.Should().NotThrow();
        }
        else
        {
            accion.Should().Throw<NominaInvalidaException>();
        }
    }

    [Fact]
    public void Reemplazar_DescartaLaCorridaConUnaNotaHaciaLaNueva()
    {
        CorridaDeNomina corrida = Corrida("Primera prueba");

        corrida.Reemplazar(2);

        corrida.Estado.Should().Be(EstadoDeCorrida.Descartada);
        corrida.EstaAbierta.Should().BeFalse();
        corrida.Observaciones.Should().Be("Primera prueba · Reemplazada por la corrida #2.");
    }

    [Fact]
    public void Reemplazar_CorridaAprobada_EsRechazado()
    {
        CorridaDeNomina corrida = Corrida(null);
        corrida.Aprobar(null);

        Action accion = () => corrida.Reemplazar(2);

        accion.Should().Throw<NominaInvalidaException>();
    }

    private static PeriodoCarga Periodo(EstadoPeriodo estado)
        => PeriodoCarga.Rehidratar(
            Guid.CreateVersion7(), Guid.CreateVersion7(), PeriodoCalendario.Crear(2026, 2, 1), "Nómina semanal 05",
            estado, Ahora, fechaLimiteCarga: null, fechaCierre: null);

    private static CorridaDeNomina Corrida(string? observaciones)
        => CorridaDeNomina.Iniciar(
            Guid.CreateVersion7(), Guid.CreateVersion7(), 1, new DateOnly(2026, 2, 7), Guid.CreateVersion7(), Ahora, observaciones);
}
