using FluentAssertions;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Nomina;
using HuimanNet.Infrastructure.Persistence.Semillas;
using Xunit;

namespace HuimanNet.Infrastructure.Tests;

/// <summary>
/// Verifica que el motor, alimentado con el catálogo inicial, reproduce las
/// cifras del archivo de cálculo de referencia (NOMINA SEM 05, febrero 2026).
/// </summary>
/// <remarks>
/// No toca la base de datos: arma el plan con el mismo JSON que siembra el
/// catálogo. Si alguien corrige una fórmula del catálogo inicial y se desvía
/// de la hoja, estas pruebas lo señalan.
/// </remarks>
public sealed class MotorConCatalogoInicialTests
{
    private static readonly DateTimeOffset Momento = new(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly FechaDeReferencia = new(2026, 2, 28);
    private static readonly Guid EmpresaId = Guid.CreateVersion7();

    private static readonly CatalogoInicial Catalogo = CatalogoInicial.Cargar();

    private static readonly IReadOnlyDictionary<string, decimal> Parametros =
        Catalogo.ConstruirParametros(Momento).ToDictionary(p => p.Clave, p => p.Valor, StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, TablaDeRangos> Tablas =
        Catalogo.ConstruirTablas(Momento).ToDictionary(t => t.Clave, StringComparer.Ordinal);

    private static readonly PlanDeCalculo PlanImss =
        PlanDeCalculo.Construir(EsquemaDePago.Imss, Catalogo.ConstruirConceptos(Momento), Parametros, Tablas);

    private readonly MotorDeCalculo _motor = new();

    [Fact]
    public void CatalogoInicial_TieneTodasLasPiezas()
    {
        Catalogo.Parametros.Should().HaveCountGreaterThan(30);
        Catalogo.Tablas.Select(t => t.Clave).Should().Contain(["ISR", "SUBSIDIO", "CVR"]);
        Catalogo.Explicaciones.Select(e => e.Esquema).Distinct().Should().HaveCount(3);

        foreach (EsquemaDePago esquema in new[] { EsquemaDePago.Imss, EsquemaDePago.Sindicato, EsquemaDePago.Honorarios })
        {
            Action construir = () => PlanDeCalculo.Construir(esquema, Catalogo.ConstruirConceptos(Momento), Parametros, Tablas);
            construir.Should().NotThrow($"el catálogo del esquema {esquema} debe ser coherente");
        }
    }

    [Fact]
    public void Stephany_SalarioMixtoConPrestamoYDescuentoSindical_ReproduceLaHoja()
    {
        var datos = new DatosDeIncidencia(
            7m, 0, 0, 0, 0, 0, 0, 0,
            Gratificacion: 900m, 0, 0, 0, 0, 0, 0,
            PrestamoPersonal: 961.5m, 0, 0, 0,
            DescuentoSindicalAdicional: 3358.39m, 0m, null, TipoDeMovimiento.Ordinaria, null);

        ResultadoDeCalculo r = Calcular(sueldoReal: 22000m, salarioDiarioFiscal: 440.87m, sdi: 465.03m, ZonaSalarioMinimo.B, datos);

        r.Obtener(ClavesDeResumen.BrutoIncidencias).Should().Be(21938.5m);
        r.Obtener(ClavesDeResumen.TotalPercepciones).Should().Be(3086.09m);
        r.Obtener(ClavesDeResumen.Isr).Should().Be(0m, "el salario fiscal es el mínimo de la zona B");
        r.Obtener(ClavesDeResumen.ImssTrabajador).Should().Be(0m, "el salario fiscal es el mínimo de la zona B");
        r.Obtener(ClavesDeResumen.NetoPagado).Should().Be(3086.09m);
        r.Obtener(ClavesDeResumen.ComplementoSindical).Should().Be(15494.02m);
        r.Obtener(ClavesDeResumen.Isn).Should().BeApproximately(131.158825m, 0.000001m);
        r.Obtener(ClavesDeResumen.CostoImss).Should().BeApproximately(446.51092164m, 0.000001m);
        r.Obtener(ClavesDeResumen.CostoInfonavit).Should().BeApproximately(509.0497398m, 0.000001m);
        r.Obtener(ClavesDeResumen.Comision).Should().BeApproximately(1842.0175589152m, 0.000001m);
        r.Obtener(ClavesDeResumen.CostoTotal).Should().BeApproximately(24867.2370453552m, 0.000001m);
    }

    [Fact]
    public void Luis_SemanaCompletaSinNovedades_ReproduceLaHoja()
    {
        ResultadoDeCalculo r = Calcular(10000m, 440.87m, 465.03m, ZonaSalarioMinimo.B, DatosDeIncidencia.SinNovedades(7m));

        r.Obtener(ClavesDeResumen.BrutoIncidencias).Should().Be(10000m);
        r.Obtener(ClavesDeResumen.ComplementoSindical).Should().Be(6913.91m);
        r.Obtener(ClavesDeResumen.Comision).Should().BeApproximately(886.9375589152m, 0.000001m);
        r.Obtener(ClavesDeResumen.CostoTotal).Should().BeApproximately(11973.6570453552m, 0.000001m);
    }

    [Fact]
    public void Isr_SalarioFiscalSobreElMinimoEnZonaA_UsaLaTablaSemanal()
    {
        ResultadoDeCalculo r = Calcular(3500m, 500m, 522.6m, ZonaSalarioMinimo.A, DatosDeIncidencia.SinNovedades(7m), pagaComplemento: false);

        r.Obtener(ClavesDeResumen.TotalPercepciones).Should().Be(3500m);
        r.Obtener(ClavesDeResumen.Isr).Should().Be(331.27m);
    }

    [Fact]
    public void ImssObrero_SalarioFiscalSobreElMinimoEnZonaB_CoincideConLaCedula()
    {
        ResultadoDeCalculo r = Calcular(3150m, 450m, 465.03m, ZonaSalarioMinimo.B, DatosDeIncidencia.SinNovedades(7m), pagaComplemento: false);

        // Cédula de cuotas de la hoja de referencia (columna de cuota obrera semanal).
        r.Obtener(ClavesDeResumen.ImssTrabajador).Should().Be(80.83m);
    }

    [Fact]
    public void SinComplementoSindical_LaGratificacionEsFiscal()
    {
        DatosDeIncidencia conBono = DatosDeIncidencia.SinNovedades(7m) with { Gratificacion = 500m };

        ResultadoDeCalculo r = Calcular(3500m, 500m, 522.6m, ZonaSalarioMinimo.A, conBono, pagaComplemento: false);

        r.Obtener(ClavesDeResumen.TotalPercepciones).Should().Be(4000m);
        r.Obtener(ClavesDeResumen.ComplementoSindical).Should().Be(0m);
    }

    private ResultadoDeCalculo Calcular(
        decimal sueldoReal, decimal salarioDiarioFiscal, decimal sdi, ZonaSalarioMinimo zona, DatosDeIncidencia datos, bool pagaComplemento = true)
    {
        RazonSocial razon = RazonSocial.Crear(
            EmpresaId, "Creatfor Imagen y Ventas", "CIV210104AB1", null, zona,
            new ConfiguracionDeRazonSocial(
                TipoDeServicio.Nomina, SubsidioAbsorbido: false, AplicaFaltasProporcionales: false,
                ModalidadDeComision.SobreCosto, 0.08m, ZonaIsn.SegunZonaDelTrabajador, 0.16m, 0m, null),
            "BBVA", Momento);

        Contrato contrato = Contrato.Crear(
            Guid.CreateVersion7(), EmpresaId, razon.Id, EsquemaDePago.Imss, "917", null, null, null,
            new CondicionesDeContrato(
                sueldoReal, salarioDiarioFiscal, sdi, zona, CreditoInfonavit.Ninguno, 0m, 0m, 0m, 0m, 0m,
                HonorariosAplicaIva: false, PagaComplementoSindical: pagaComplemento),
            new DateOnly(2021, 1, 4), Momento);

        Incidencia incidencia = Incidencia.Registrar(EmpresaId, Guid.CreateVersion7(), contrato.Id, datos, Guid.CreateVersion7(), Momento);

        IReadOnlyDictionary<string, decimal> variables =
            ConstructorDeVariables.Construir(contrato, razon, incidencia, Parametros, FechaDeReferencia);

        return _motor.Calcular(PlanImss, variables);
    }
}
