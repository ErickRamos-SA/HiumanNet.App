using FluentAssertions;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Nomina;
using Xunit;

namespace HuimanNet.Domain.Tests;

/// <summary>
/// Pruebas del plan de cálculo: orden por dependencias, detección de ciclos y
/// validación de referencias.
/// </summary>
public sealed class PlanDeCalculoTests
{
    private static readonly DateTimeOffset Momento = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

    private static ConceptoDeNomina Concepto(string clave, int orden, string formula, EsquemasDePago esquemas = EsquemasDePago.Todos)
        => ConceptoDeNomina.Crear(clave, clave, "Prueba", TipoDeConcepto.Percepcion, esquemas, orden, formula, true, null, Momento);

    private static PlanDeCalculo Construir(params ConceptoDeNomina[] conceptos)
        => PlanDeCalculo.Construir(EsquemaDePago.Imss, conceptos, new Dictionary<string, decimal> { ["TASA"] = 0.5m }, new Dictionary<string, TablaDeRangos>());

    [Fact]
    public void Construir_ConceptoQueDependeDeOtroPosterior_LoCalculaDespues()
    {
        PlanDeCalculo plan = Construir(
            Concepto("DOBLE", 10, "BASE_CALCULADA * 2"),
            Concepto("BASE_CALCULADA", 20, "100 * TASA"));

        ResultadoDeCalculo resultado = new MotorDeCalculo().Calcular(plan, new Dictionary<string, decimal>());

        resultado.Obtener("BASE_CALCULADA").Should().Be(50m);
        resultado.Obtener("DOBLE").Should().Be(100m);
    }

    [Fact]
    public void Construir_ReferenciasCirculares_SeRechazan()
    {
        Action construir = () => Construir(Concepto("PRIMERO", 1, "SEGUNDO + 1"), Concepto("SEGUNDO", 2, "PRIMERO + 1"));

        construir.Should().Throw<CatalogoInvalidoException>();
    }

    [Fact]
    public void Construir_ReferenciaDesconocida_SeRechaza()
    {
        Action construir = () => Construir(Concepto("TOTAL_X", 1, "NO_EXISTE * 2"));

        construir.Should().Throw<CatalogoInvalidoException>();
    }

    [Fact]
    public void Construir_ConceptosDeOtroEsquema_SeOmiten()
    {
        PlanDeCalculo plan = Construir(
            Concepto("COMUN", 1, "1"),
            Concepto("SOLO_SINDICATO", 2, "2", EsquemasDePago.Sindicato));

        plan.Conceptos.Select(c => c.Concepto.Clave).Should().Equal("COMUN");
    }
}
