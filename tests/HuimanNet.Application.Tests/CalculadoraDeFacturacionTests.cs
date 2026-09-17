using FluentAssertions;
using HuimanNet.Application.Nomina;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using Xunit;

namespace HuimanNet.Application.Tests;

/// <summary>
/// Pruebas de la facturación estimada: sin fórmulas propias, con el costo que
/// calcula el catálogo y la tasa de IVA de la razón social o la general.
/// </summary>
public sealed class CalculadoraDeFacturacionTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmpresaId = Guid.CreateVersion7();

    [Fact]
    public void Calcular_ElSubtotalEsLaSumaDelCostoTotalDelCatalogo()
    {
        RazonSocial razon = Razon(tasaIva: 0.16m);

        FacturacionDeCorridaDto factura = CalculadoraDeFacturacion.Calcular(
            [Resultado(razon.Id, facturable: 1_000m, costoTotal: 1_500m), Resultado(razon.Id, facturable: 2_000m, costoTotal: 2_500m)],
            [razon], tasaIvaGeneral: 0m).Single();

        factura.BaseNomina.Should().Be(3_000m);
        factura.Subtotal.Should().Be(4_000m);
        factura.Iva.Should().Be(640m);
        factura.Total.Should().Be(4_640m);
    }

    [Fact]
    public void Calcular_RazonSocialSinTasa_UsaLaTasaGeneral()
    {
        RazonSocial razon = Razon(tasaIva: null);

        FacturacionDeCorridaDto factura = CalculadoraDeFacturacion.Calcular(
            [Resultado(razon.Id, facturable: 1_000m, costoTotal: 1_000m)], [razon], tasaIvaGeneral: 0.08m).Single();

        factura.Iva.Should().Be(80m);
    }

    /// <summary>Crea una razón social de prueba.</summary>
    /// <param name="tasaIva">Tasa propia, o <c>null</c> para la general.</param>
    /// <returns>La razón social.</returns>
    private static RazonSocial Razon(decimal? tasaIva)
        => RazonSocial.Crear(
            EmpresaId, "Servicios de prueba", "SPR210104AB1", null, ZonaSalarioMinimo.A,
            new ConfiguracionDeRazonSocial(
                TipoDeServicio.Nomina, false, false, ModalidadDeComision.SobreCosto, 0.08m, ZonaIsn.SegunZonaDelTrabajador, tasaIva, 0m, null),
            null, Ahora);

    /// <summary>Crea el resultado de un trabajador con los importes indicados y el resto en cero.</summary>
    /// <param name="razonSocialId">Razón social pagadora.</param>
    /// <param name="facturable">Base facturable.</param>
    /// <param name="costoTotal">Costo total que calculó el catálogo.</param>
    /// <returns>El resultado.</returns>
    private static ResultadoDeNominaDto Resultado(Guid razonSocialId, decimal facturable, decimal costoTotal)
        => new(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), razonSocialId,
            "Servicios de prueba", EsquemaDePago.Imss, "E001", "Ana Gómez", TipoDeMovimiento.Ordinaria,
            0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, facturable, 0m, costoTotal, 0m, 0m, 0m, 0m, null);
}
