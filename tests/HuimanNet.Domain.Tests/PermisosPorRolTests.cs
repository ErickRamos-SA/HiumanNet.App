using FluentAssertions;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Services;
using Xunit;

namespace HuimanNet.Domain.Tests;

/// <summary>
/// Pruebas de los permisos efectivos: rol predeterminado más excepciones del administrador.
/// </summary>
public sealed class PermisosPorRolTests
{
    [Fact]
    public void Efectivas_ClienteSinExcepciones_NoPuedeCalcularNomina()
    {
        IReadOnlySet<AccionDelSistema> acciones = PermisosPorRol.Efectivas(RolUsuario.ClienteEmpresa, null);

        acciones.Should().Contain(AccionDelSistema.CargarDocumentos).And.NotContain(AccionDelSistema.CalcularNomina);
    }

    [Fact]
    public void Efectivas_ExcepcionesConcedenYNieganSobreElRol()
    {
        IReadOnlySet<AccionDelSistema> acciones = PermisosPorRol.Efectivas(
            RolUsuario.OperadorNomina,
            [new PermisoDeUsuario(AccionDelSistema.AdministrarCatalogosDeCalculo, true), new PermisoDeUsuario(AccionDelSistema.AprobarNomina, false)]);

        acciones.Should().Contain(AccionDelSistema.AdministrarCatalogosDeCalculo).And.NotContain(AccionDelSistema.AprobarNomina);
    }

    [Fact]
    public void Predeterminadas_Administrador_TieneTodasLasAcciones()
        => PermisosPorRol.Predeterminadas(RolUsuario.Administrador)
            .Should().HaveCount(Enum.GetValues<AccionDelSistema>().Length - 1);
}
