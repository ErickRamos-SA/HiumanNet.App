using FluentAssertions;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Services;
using HuimanNet.Domain.ValueObjects;
using Xunit;

namespace HuimanNet.Domain.Tests;

/// <summary>
/// Pruebas de las reglas que antes estaban repetidas en la aplicación, la web y
/// la app móvil y ahora viven sólo en el dominio.
/// </summary>
public sealed class ReglasCompartidasTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PoliticaDeAcceso _politica = new();

    [Theory]
    [InlineData(TipoDocumento.Incidencia, AccionDelSistema.CargarDocumentos)]
    [InlineData(TipoDocumento.DatosEmpleado, AccionDelSistema.CargarDocumentos)]
    [InlineData(TipoDocumento.Resultado, AccionDelSistema.PublicarResultados)]
    [InlineData(TipoDocumento.Ajuste, AccionDelSistema.PublicarResultados)]
    public void AccionParaCargar_AsociaCadaTipoConSuPermiso(TipoDocumento tipo, AccionDelSistema esperada)
        => PoliticaDeAcceso.AccionParaCargar(tipo).Should().Be(esperada);

    [Fact]
    public void AccionParaCargar_TipoNoEspecificado_EsRechazado()
    {
        Action accion = () => PoliticaDeAcceso.AccionParaCargar(TipoDocumento.NoEspecificado);

        accion.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TiposQuePuedeCargar_ClienteConPermisosPorOmision_SubeIncidenciasYDatos()
        => PoliticaDeAcceso.TiposQuePuedeCargar(RolUsuario.ClienteEmpresa, AccionesDe(RolUsuario.ClienteEmpresa))
            .Should().BeEquivalentTo([TipoDocumento.Incidencia, TipoDocumento.DatosEmpleado]);

    [Fact]
    public void TiposQuePuedeCargar_OperadorConPermisosPorOmision_SubeResultadosYAjustes()
        => PoliticaDeAcceso.TiposQuePuedeCargar(RolUsuario.OperadorNomina, AccionesDe(RolUsuario.OperadorNomina))
            .Should().BeEquivalentTo([TipoDocumento.Resultado, TipoDocumento.Ajuste]);

    [Fact]
    public void TiposQuePuedeCargar_ClienteConLaCargaNegada_NoSubeNada()
        => PoliticaDeAcceso.TiposQuePuedeCargar(
                RolUsuario.ClienteEmpresa,
                AccionesDe(RolUsuario.ClienteEmpresa, new PermisoDeUsuario(AccionDelSistema.CargarDocumentos, false)))
            .Should().BeEmpty();

    [Fact]
    public void GarantizarPuedeCargar_ClienteConLaCargaNegada_EsRechazado()
    {
        Action accion = () => _politica.GarantizarPuedeCargar(
            RolUsuario.ClienteEmpresa,
            [new PermisoDeUsuario(AccionDelSistema.CargarDocumentos, false)],
            TipoDocumento.Incidencia);

        accion.Should().Throw<AccesoNoAutorizadoException>();
    }

    [Fact]
    public void GarantizarPuedeCargar_ClienteSinPermisosPersonalizados_NoLanza()
    {
        Action accion = () => _politica.GarantizarPuedeCargar(
            RolUsuario.ClienteEmpresa, personalizados: null, TipoDocumento.Incidencia);

        accion.Should().NotThrow();
    }

    [Theory]
    [InlineData(RolUsuario.ClienteEmpresa, false)]
    [InlineData(RolUsuario.OperadorNomina, true)]
    [InlineData(RolUsuario.Administrador, true)]
    public void EsTransversal_SoloNominaYAdministracion(RolUsuario rol, bool esperado)
        => rol.EsTransversal().Should().Be(esperado);

    [Theory]
    [InlineData(EstadoPeriodo.Abierto, EstadoPeriodo.Recibido)]
    [InlineData(EstadoPeriodo.Recibido, EstadoPeriodo.EnProceso)]
    [InlineData(EstadoPeriodo.EnProceso, EstadoPeriodo.ResultadosDisponibles)]
    [InlineData(EstadoPeriodo.ResultadosDisponibles, EstadoPeriodo.Cerrado)]
    public void Siguiente_AvanzaUnPasoEnElFlujo(EstadoPeriodo actual, EstadoPeriodo esperado)
        => actual.Siguiente().Should().Be(esperado);

    [Fact]
    public void Siguiente_PeriodoCerrado_NoTieneSiguiente()
        => EstadoPeriodo.Cerrado.Siguiente().Should().BeNull();

    [Theory]
    [InlineData(2026, 2, 28)]
    [InlineData(2028, 2, 29)]
    [InlineData(2026, 4, 30)]
    [InlineData(2026, 12, 31)]
    public void FechaDeReferencia_EsElUltimoDiaDelMes(int anio, int mes, int diaEsperado)
    {
        PeriodoCarga periodo = PeriodoCarga.Abrir(
            Guid.CreateVersion7(), PeriodoCalendario.Crear(anio, mes, 1), "Prueba", Ahora);

        periodo.FechaDeReferencia.Should().Be(new DateOnly(anio, mes, diaEsperado));
    }

    [Fact]
    public void Sumar_ConsolidaLosResumenesYCuentaLosTrabajadores()
    {
        TotalesDeCorrida totales = TotalesDeCorrida.Sumar(
            [Resultado(bruto: 1_000m, neto: 900m, costo: 1_300m), Resultado(bruto: 2_000m, neto: 1_700m, costo: 2_600m)]);

        totales.Trabajadores.Should().Be(2);
        totales.Bruto.Should().Be(3_000m);
        totales.Neto.Should().Be(2_600m);
        totales.CostoTotal.Should().Be(3_900m);
    }

    [Fact]
    public void Sumar_SinResultados_DevuelveTotalesEnCero()
        => TotalesDeCorrida.Sumar([]).Should().Be(TotalesDeCorrida.Vacios);

    [Fact]
    public void Faltantes_SindicatoConLosTotalesDelRecibo_NoFaltaNada()
        => ClavesDeResumen.Faltantes(CalculoConTotalesDelRecibo(), EsquemaDePago.Sindicato).Should().BeEmpty();

    [Fact]
    public void Faltantes_ImssSoloConLosTotalesDelRecibo_ExigeLasCargasPatronales()
    {
        IReadOnlyList<string> faltantes = ClavesDeResumen.Faltantes(CalculoConTotalesDelRecibo(), EsquemaDePago.Imss);

        faltantes.Should().Contain([ClavesDeResumen.Isr, ClavesDeResumen.ImssPatronal, ClavesDeResumen.Isn])
            .And.NotContain(ClavesDeResumen.NetoPagado);
    }

    /// <summary>Acciones efectivas de un rol con los permisos personalizados indicados.</summary>
    /// <param name="rol">Rol del usuario.</param>
    /// <param name="personalizados">Permisos que concede o niega el administrador.</param>
    /// <returns>El conjunto de acciones que el usuario puede ejecutar.</returns>
    private static IReadOnlySet<AccionDelSistema> AccionesDe(RolUsuario rol, params PermisoDeUsuario[] personalizados)
        => PermisosPorRol.Efectivas(rol, personalizados);

    /// <summary>Crea un resultado persistido con las cifras indicadas y el resto en cero.</summary>
    /// <param name="bruto">Bruto de incidencias.</param>
    /// <param name="neto">Neto pagado.</param>
    /// <param name="costo">Costo total antes de IVA.</param>
    /// <returns>El resultado de un contrato ficticio.</returns>
    private static ResultadoDeNomina Resultado(decimal bruto, decimal neto, decimal costo)
        => ResultadoDeNomina.Rehidratar(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), Guid.CreateVersion7(), EsquemaDePago.Imss, "E001", "Ana Gómez",
            TipoDeMovimiento.Ordinaria,
            new ResumenDeResultado(bruto, 0, 0, neto, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, costo, 0, 0, 0, 0),
            [], advertencia: null);

    /// <summary>Calcula un plan que sólo define los totales del recibo y el costo.</summary>
    /// <returns>El resultado del motor para ese plan.</returns>
    private static ResultadoDeCalculo CalculoConTotalesDelRecibo()
    {
        string[] claves =
        [
            ClavesDeResumen.BrutoIncidencias, ClavesDeResumen.TotalPercepciones,
            ClavesDeResumen.TotalDeducciones, ClavesDeResumen.NetoPagado, ClavesDeResumen.CostoTotal,
        ];

        ConceptoDeNomina[] conceptos =
        [
            .. claves.Select((clave, indice) => ConceptoDeNomina.Crear(
                clave, clave, "Prueba", TipoDeConcepto.Percepcion, EsquemasDePago.Todos, indice + 1, "0", true, null, Ahora)),
        ];

        PlanDeCalculo plan = PlanDeCalculo.Construir(
            EsquemaDePago.Imss, conceptos, new Dictionary<string, decimal>(), new Dictionary<string, TablaDeRangos>());

        return new MotorDeCalculo().Calcular(plan, new Dictionary<string, decimal>());
    }
}
