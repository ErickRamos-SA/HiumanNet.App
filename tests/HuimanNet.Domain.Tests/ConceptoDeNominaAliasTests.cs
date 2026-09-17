using FluentAssertions;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Nomina;
using Xunit;

namespace HuimanNet.Domain.Tests;

/// <summary>
/// Pruebas de los alias de cotejo de un concepto, que sustituyen la tabla de
/// equivalencias que antes estaba escrita en el código.
/// </summary>
public sealed class ConceptoDeNominaAliasTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Crear_DepuraLosAlias()
    {
        ConceptoDeNomina concepto = Concepto(["  Neto pagado ", "NETO PAGADO", "", "Neto"]);

        concepto.AliasDeCotejo.Should().Equal("Neto pagado", "Neto");
    }

    [Fact]
    public void Crear_SinAlias_DejaLaListaVacia()
        => Concepto(null).AliasDeCotejo.Should().BeEmpty();

    [Fact]
    public void Crear_AliasConBarraVertical_EsRechazado()
    {
        Action accion = () => Concepto(["NETO|PAGADO"]);

        accion.Should().Throw<CatalogoInvalidoException>();
    }

    [Fact]
    public void Crear_DemasiadosAlias_EsRechazado()
    {
        Action accion = () => Concepto(Enumerable.Range(1, ConceptoDeNomina.MaximoDeAlias + 1).Select(static i => $"A{i}"));

        accion.Should().Throw<CatalogoInvalidoException>();
    }

    [Fact]
    public void Actualizar_SustituyeLosAlias()
    {
        ConceptoDeNomina concepto = Concepto(["NETO"]);

        concepto.Actualizar(
            concepto.Nombre, concepto.Descripcion, concepto.Tipo, concepto.Esquemas, concepto.Orden, concepto.Formula,
            concepto.VisibleEnRecibo, concepto.Activo, ["Neto pagado"], Ahora);

        concepto.AliasDeCotejo.Should().Equal("Neto pagado");
    }

    /// <summary>Crea un concepto de prueba con los alias indicados.</summary>
    /// <param name="alias">Alias de cotejo, o <c>null</c>.</param>
    /// <returns>El concepto.</returns>
    private static ConceptoDeNomina Concepto(IEnumerable<string>? alias)
        => ConceptoDeNomina.Crear(
            "NETO_PAGADO", "Neto pagado", "Prueba", TipoDeConcepto.Total, EsquemasDePago.Todos, 10, "0", true, null, Ahora, alias);
}
