using FluentAssertions;
using HuimanNet.Application.Common;
using HuimanNet.Application.Inicio;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Services;
using NSubstitute;
using Xunit;

namespace HuimanNet.Application.Tests;

/// <summary>
/// Pruebas de las reglas de los pendientes del panel de inicio, que antes
/// decidía la consulta SQL.
/// </summary>
public sealed class ObtenerResumenDeInicioHandlerTests
{
    private readonly Guid _empresaId = Guid.CreateVersion7();
    private readonly IConsultasInicio _consultas = Substitute.For<IConsultasInicio>();
    private readonly IUsuarioActual _usuario = Substitute.For<IUsuarioActual>();

    public ObtenerResumenDeInicioHandlerTests()
    {
        _usuario.EmpresaId.Returns(_empresaId);
        _usuario.Empresas.Returns([_empresaId]);
        _usuario.Permisos.Returns([]);

        _consultas.ObtenerDatosAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new DatosDeInicio(
            3, 2, 4, 1,
            [
                Periodo(EstadoPeriodo.Abierto, delCliente: 0, deResultado: 0),
                Periodo(EstadoPeriodo.Recibido, delCliente: 2, deResultado: 0),
                Periodo(EstadoPeriodo.ResultadosDisponibles, delCliente: 2, deResultado: 1),
            ],
            [new CorridaDeInicio(Guid.CreateVersion7(), _empresaId, 1, "Semana 1")])));
    }

    [Fact]
    public async Task Cliente_RecibeSubirYDescargar()
    {
        _usuario.Rol.Returns(RolUsuario.ClienteEmpresa);

        ResumenDeInicioDto resumen = await EjecutarAsync();

        resumen.Pendientes.Select(static p => p.Tipo)
            .Should().Equal(TipoDePendiente.SubirDocumentos, TipoDePendiente.DescargarResultados);
        resumen.Pendientes.Should().OnlyContain(p => p.EmpresaId == _empresaId && p.PeriodoId != null);
    }

    [Fact]
    public async Task Cliente_ConLaCargaNegada_NoRecibeSubirDocumentos()
    {
        _usuario.Rol.Returns(RolUsuario.ClienteEmpresa);
        _usuario.Permisos.Returns([new PermisoDeUsuario(AccionDelSistema.CargarDocumentos, false)]);

        ResumenDeInicioDto resumen = await EjecutarAsync();

        resumen.Pendientes.Select(static p => p.Tipo).Should().Equal(TipoDePendiente.DescargarResultados);
    }

    [Fact]
    public async Task Operador_RecibeProcesarYCotejar()
    {
        _usuario.Rol.Returns(RolUsuario.OperadorNomina);

        ResumenDeInicioDto resumen = await EjecutarAsync();

        resumen.Pendientes.Select(static p => p.Tipo)
            .Should().Equal(TipoDePendiente.ProcesarPeriodo, TipoDePendiente.CotejarCorrida);
        resumen.Pendientes.Last().CorridaId.Should().NotBeNull();
        resumen.Pendientes.Last().Clave.Should().Be("pendiente.cotejarCorrida");
    }

    /// <summary>Ejecuta la consulta con el usuario configurado.</summary>
    /// <returns>El resumen.</returns>
    private Task<ResumenDeInicioDto> EjecutarAsync()
        => new ObtenerResumenDeInicioHandler(_consultas, new AutorizadorDeCasosDeUso(_usuario, new PoliticaDeAcceso()))
            .EjecutarAsync(new ObtenerResumenDeInicioQuery(), TestContext.Current.CancellationToken);

    /// <summary>Crea un período de inicio de prueba.</summary>
    /// <param name="estado">Estado del período.</param>
    /// <param name="delCliente">Documentos del cliente disponibles.</param>
    /// <param name="deResultado">Documentos de resultado disponibles.</param>
    /// <returns>El período.</returns>
    private PeriodoDeInicio Periodo(EstadoPeriodo estado, int delCliente, int deResultado)
        => new(Guid.CreateVersion7(), _empresaId, estado, "Semana 1", "Creatfor", delCliente, deResultado);
}
