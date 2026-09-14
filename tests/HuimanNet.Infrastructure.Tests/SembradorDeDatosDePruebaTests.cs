using FluentAssertions;
using HuimanNet.Application.Empleados;
using HuimanNet.Application.Interfaces;
using HuimanNet.Application.Nomina;
using HuimanNet.Application.Periodos.Commands;
using HuimanNet.Application.Periodos.Queries;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empleados;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Repositories;
using HuimanNet.Infrastructure.Persistence.Semillas;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static HuimanNet.Infrastructure.Tests.EntornoSqlDePrueba;

namespace HuimanNet.Infrastructure.Tests;

/// <summary>
/// Verifica los datos de prueba del desarrollo contra un SQL Server real: se
/// cargan una sola vez, los usuarios de prueba siguen la contraseña del
/// administrador, el cliente sólo ve sus empresas y la nómina de la empresa de
/// prueba se calcula con todos sus esquemas de pago.
/// </summary>
/// <remarks>
/// Usa una base de datos exclusiva, <c>HuimanNetSemilla_Pruebas</c>, que se
/// borra y se recrea en cada ejecución.
/// </remarks>
public sealed class SembradorDeDatosDePruebaTests
{
    private const string CorreoAdministrador = "admin.semilla@huimannet.local";

    [Fact]
    public async Task Arranque_CargaDatosDePruebaUtilizablesEIdempotentes()
    {
        string cadena = Cadena("HuimanNetSemilla_Pruebas");
        string? motivo = await RecrearBaseDeDatosAsync(cadena);
        Assert.SkipWhen(motivo is not null, $"SQL Server de pruebas no disponible: {motivo}");

        await using ServiceProvider servicios = ConstruirServicios(cadena, CorreoAdministrador, new Dictionary<string, string?>
        {
            ["Identidad:AdministradorInicial:Contrasena"] = "Semilla-de-pruebas-1",
            ["SqlServer:SembrarDatosDePrueba"] = "true",
        });

        (await PreparacionDelEntorno.EjecutarAsync(servicios, Ct)).Should().BeTrue();
        (await PreparacionDelEntorno.EjecutarAsync(servicios, Ct)).Should().BeTrue("un segundo arranque no debe fallar ni duplicar datos");

        var casos = new Casos(servicios);

        IReadOnlyList<Empresa> empresas = await casos.Usar<IEmpresaRepository, IReadOnlyList<Empresa>>(r => r.ListarAsync(false, Ct));
        empresas.Select(e => e.RazonSocial).Should().BeEquivalentTo("Creatfor Demo", "Creatfor Servicios Demo", "Empresa Ajena Demo");

        // ---- Usuarios de prueba con la contraseña del administrador ----------
        Usuario administrador = await ObtenerUsuarioAsync(servicios, CorreoAdministrador);
        Usuario nomina = await ObtenerUsuarioAsync(servicios, SembradorDeDatosDePrueba.CorreoNomina);
        Usuario cliente = await ObtenerUsuarioAsync(servicios, SembradorDeDatosDePrueba.CorreoCliente);

        nomina.Rol.Should().Be(RolUsuario.OperadorNomina);
        nomina.Empresas.Should().BeEmpty("nómina opera sobre todas las empresas");
        cliente.Rol.Should().Be(RolUsuario.ClienteEmpresa);
        cliente.Empresas.Should().Equal(
            empresas.Single(e => e.RazonSocial == "Creatfor Demo").Id,
            empresas.Single(e => e.RazonSocial == "Creatfor Servicios Demo").Id);

        foreach (Usuario deprueba in new[] { nomina, cliente })
        {
            deprueba.HashContrasena.Should().NotBeNullOrEmpty().And.Be(administrador.HashContrasena);
            deprueba.RequiereCambioDeContrasena.Should().BeFalse();
        }

        // ---- Si el administrador cambia su contraseña, los de prueba la siguen
        IHasherDeContrasenas hasher = servicios.GetRequiredService<IHasherDeContrasenas>();
        administrador.EstablecerContrasena(hasher.Hashear("Otra-contrasena-2"), requiereCambio: false);
        await casos.Usar<IUsuarioRepository, bool>(async r =>
        {
            await r.ActualizarAsync(administrador, Ct);
            return true;
        });

        (await PreparacionDelEntorno.EjecutarAsync(servicios, Ct)).Should().BeTrue();
        (await ObtenerUsuarioAsync(servicios, SembradorDeDatosDePrueba.CorreoNomina)).HashContrasena
            .Should().Be(administrador.HashContrasena);

        // ---- El cliente sólo ve sus dos empresas -----------------------------
        UsuarioDePrueba identidad = servicios.GetRequiredService<UsuarioDePrueba>();
        identidad.Establecer(cliente);

        PaginaDto<EmpleadoResumenDto> empleadosDelCliente = await casos.Consultar<ListarEmpleadosQuery, PaginaDto<EmpleadoResumenDto>>(
            new ListarEmpleadosQuery(null, true, null, 1, 50));
        empleadosDelCliente.Elementos.Should().HaveCount(7)
            .And.NotContain(e => e.EmpresaRazonSocial == "Empresa Ajena Demo");

        // ---- Nómina calcula el período de prueba con todos los esquemas -----
        identidad.Establecer(nomina);
        Guid creatforDemo = cliente.EmpresaId!.Value;

        PeriodoDto periodo = (await casos.Consultar<ListarPeriodosQuery, IReadOnlyList<PeriodoDto>>(
            new ListarPeriodosQuery(creatforDemo, IncluirCerrados: true))).Should().ContainSingle().Subject;

        await casos.Ejecutar(new CambiarEstadoPeriodoCommand(periodo.Id, EstadoPeriodo.EnProceso, null, creatforDemo));

        CorridaDeNominaDto corrida = await casos.Ejecutar<CalcularNominaCommand, CorridaDeNominaDto>(
            new CalcularNominaCommand(periodo.Id, creatforDemo, "Datos de prueba"));

        corrida.Trabajadores.Should().Be(5, "IMSS puro, IMSS mixto, sindicato y honorarios deben calcularse");
        corrida.CostoTotal.Should().BePositive();
    }
}
