using FluentAssertions;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Formulas;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Services;
using Xunit;

namespace HuimanNet.Domain.Tests;

/// <summary>
/// Pruebas del lenguaje de fórmulas: es la pieza que permite cambiar el cálculo
/// sin tocar código, así que su comportamiento debe estar fijado por pruebas.
/// </summary>
public sealed class FormulasTests
{
    private sealed class Contexto(Dictionary<string, decimal> valores) : IContextoDeEvaluacion
    {
        public List<(string Tabla, decimal Valor, string Campo)> Consultas { get; } = [];

        public bool TryObtenerValor(string nombre, out decimal valor) => valores.TryGetValue(nombre, out valor);

        public decimal ConsultarTabla(string tabla, decimal valor, string campo)
        {
            Consultas.Add((tabla, valor, campo));
            return 42m;
        }
    }

    private static decimal Evaluar(string formula, params (string Clave, decimal Valor)[] valores)
        => FormulaCompilada.Compilar(formula).Evaluar(new Contexto(valores.ToDictionary(v => v.Clave, v => v.Valor)));

    [Theory]
    [InlineData("2 + 3 * 4", 14)]
    [InlineData("(2 + 3) * 4", 20)]
    [InlineData("10 - 4 - 3", 3)]
    [InlineData("2 ^ 3", 8)]
    [InlineData("7 / 2", 3.5)]
    [InlineData("10 / 0", 0)]
    [InlineData("MAX(1; 7)", 7)]
    [InlineData("MIN(4, 2)", 2)]
    [InlineData("REDONDEAR(2.346; 2)", 2.35)]
    [InlineData("TRUNCAR(2.349; 2)", 2.34)]
    [InlineData("ABS(3 - 8)", 5)]
    [InlineData("ENTERO(7.9)", 7)]
    [InlineData("TECHO(7.1)", 8)]
    [InlineData("ENTRE(5; 1; 10)", 1)]
    [InlineData("ENTRE(15; 1; 10)", 0)]
    [InlineData("SI(3 > 2; 10; 20)", 10)]
    [InlineData("SI(3 < 2, 10, 20)", 20)]
    public void Evaluar_ExpresionesConstantes_DevuelveElValorEsperado(string formula, double esperado)
        => Evaluar(formula).Should().Be((decimal)esperado);

    [Fact]
    public void Evaluar_OperadoresLogicosInfijos_CombinanCondiciones()
    {
        Evaluar("SI(A = 1 Y B = 2; 1; 0)", ("A", 1m), ("B", 2m)).Should().Be(1m);
        Evaluar("SI(A = 1 Y B = 3; 1; 0)", ("A", 1m), ("B", 2m)).Should().Be(0m);
        Evaluar("SI(A = 9 O B = 2; 1; 0)", ("A", 1m), ("B", 2m)).Should().Be(1m);
        Evaluar("NO(A = 1)", ("A", 1m)).Should().Be(0m);
    }

    [Fact]
    public void Evaluar_ConVariables_UsaElContexto()
        => Evaluar("SUELDO_PERIODO_REAL / DIAS_PERIODO", ("SUELDO_PERIODO_REAL", 22000m), ("DIAS_PERIODO", 8m)).Should().Be(2750m);

    [Fact]
    public void Compilar_DeclaraVariablesYTablasReferidas()
    {
        FormulaCompilada formula = FormulaCompilada.Compilar("TABLA(\"ISR\"; BASE * 2; \"CUOTA_FIJA\") + SDI");

        formula.Variables.Should().Contain(["BASE", "SDI"]);
        formula.Tablas.Should().Contain("ISR");
    }

    [Fact]
    public void Evaluar_Tabla_ConsultaElCampoConElValorCalculado()
    {
        var contexto = new Contexto(new Dictionary<string, decimal> { ["BASE"] = 1750m });

        decimal resultado = FormulaCompilada.Compilar("TABLA(\"ISR\"; BASE * 2; \"CUOTA_FIJA\")").Evaluar(contexto);

        resultado.Should().Be(42m);
        contexto.Consultas.Should().ContainSingle().Which.Should().Be(("ISR", 3500m, "CUOTA_FIJA"));
    }

    [Theory]
    [InlineData("2 +")]
    [InlineData("(2 + 3")]
    [InlineData("FUNCION_INEXISTENTE(1)")]
    [InlineData("SI(1; 2)")]
    [InlineData("2 $ 3")]
    public void Compilar_FormulaMalFormada_LanzaErrorDeFormula(string formula)
    {
        Action compilar = () => FormulaCompilada.Compilar(formula);

        compilar.Should().Throw<ErrorDeFormulaException>();
    }
}

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
