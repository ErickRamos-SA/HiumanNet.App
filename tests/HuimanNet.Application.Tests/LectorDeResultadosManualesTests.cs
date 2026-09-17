using FluentAssertions;
using HuimanNet.Application.Interfaces;
using HuimanNet.Application.Nomina;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Nomina;
using Xunit;

namespace HuimanNet.Application.Tests;

/// <summary>
/// Pruebas del índice de alias del cotejo, que ahora sale del catálogo.
/// </summary>
public sealed class LectorDeResultadosManualesTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IndiceDeAlias_NormalizaLosAliasDeLosConceptosActivos()
    {
        IReadOnlyDictionary<string, string> indice = LectorDeResultadosManuales.IndiceDeAlias(
            [Concepto("NETO_PAGADO", 10, "Neto pagado", "Neto"), Concepto("DIAS_TRABAJADOS_FISCAL", 20, "Días trabajados")]);

        indice[TablaLeida.Normalizar("NETO PAGADO")].Should().Be("NETO_PAGADO");
        indice[TablaLeida.Normalizar("dias trabajados")].Should().Be("DIAS_TRABAJADOS_FISCAL");
    }

    [Fact]
    public void IndiceDeAlias_IgnoraLosConceptosInactivos()
    {
        ConceptoDeNomina inactivo = Concepto("NETO_PAGADO", 10, "Neto");
        inactivo.Actualizar(
            inactivo.Nombre, inactivo.Descripcion, inactivo.Tipo, inactivo.Esquemas, inactivo.Orden, inactivo.Formula,
            inactivo.VisibleEnRecibo, activo: false, inactivo.AliasDeCotejo, Ahora);

        LectorDeResultadosManuales.IndiceDeAlias([inactivo]).Should().BeEmpty();
    }

    [Fact]
    public void IndiceDeAlias_AliasRepetido_GanaElPrimeroEnOrden()
    {
        IReadOnlyDictionary<string, string> indice = LectorDeResultadosManuales.IndiceDeAlias(
            [Concepto("COSTO_TOTAL", 20, "Suma"), Concepto("BRUTO_INCIDENCIAS", 10, "Suma")]);

        indice[TablaLeida.Normalizar("Suma")].Should().Be("BRUTO_INCIDENCIAS");
    }

    /// <summary>Crea un concepto de prueba con alias.</summary>
    /// <param name="clave">Clave del concepto.</param>
    /// <param name="orden">Orden de presentación.</param>
    /// <param name="alias">Alias de cotejo.</param>
    /// <returns>El concepto.</returns>
    private static ConceptoDeNomina Concepto(string clave, int orden, params string[] alias)
        => ConceptoDeNomina.Crear(clave, clave, "Prueba", TipoDeConcepto.Total, EsquemasDePago.Todos, orden, "0", true, null, Ahora, alias);
}
