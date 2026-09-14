using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Formulas;

namespace HuimanNet.Domain.Nomina;

/// <summary>
/// Valor calculado de un concepto.
/// </summary>
/// <param name="Clave">Clave del concepto.</param>
/// <param name="Importe">Importe calculado, sin redondeo adicional al de la fórmula.</param>
public sealed record ValorDeConcepto(string Clave, decimal Importe);

/// <summary>
/// Resultado de evaluar un plan de cálculo para un trabajador.
/// </summary>
public sealed class ResultadoDeCalculo
{
    private readonly Dictionary<string, decimal> _porClave;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ResultadoDeCalculo"/>.
    /// </summary>
    /// <param name="valores">Valores en orden de evaluación.</param>
    /// <param name="porClave">Índice por clave.</param>
    internal ResultadoDeCalculo(IReadOnlyList<ValorDeConcepto> valores, Dictionary<string, decimal> porClave)
    {
        Valores = valores;
        _porClave = porClave;
    }

    /// <summary>Obtiene un resultado sin conceptos.</summary>
    /// <value>Instancia compartida que devuelve cero para cualquier clave.</value>
    public static ResultadoDeCalculo Vacio { get; } = new([], new Dictionary<string, decimal>(StringComparer.Ordinal));

    /// <summary>Obtiene los valores en el orden en que se evaluaron.</summary>
    /// <value>Lista de sólo lectura con todos los conceptos del plan.</value>
    public IReadOnlyList<ValorDeConcepto> Valores { get; }

    /// <summary>
    /// Obtiene el importe de un concepto.
    /// </summary>
    /// <param name="clave">Clave del concepto.</param>
    /// <returns>El importe, o cero si el plan no incluye el concepto.</returns>
    public decimal Obtener(string clave) => _porClave.TryGetValue(clave, out decimal valor) ? valor : 0m;

    /// <summary>
    /// Indica si el plan incluye un concepto.
    /// </summary>
    /// <param name="clave">Clave del concepto.</param>
    /// <returns><c>true</c> si se calculó.</returns>
    public bool Contiene(string clave) => _porClave.ContainsKey(clave);
}

/// <summary>
/// Evalúa un <see cref="PlanDeCalculo"/> para un trabajador concreto.
/// </summary>
/// <remarks>
/// Servicio de dominio puro y sin estado: es seguro compartirlo entre hilos y
/// ejecutar el cálculo de miles de trabajadores en paralelo con el mismo plan.
/// Cada evaluación crea su propio contexto, así que ninguna estructura se
/// comparte entre trabajadores.
/// </remarks>
public sealed class MotorDeCalculo
{
    /// <summary>
    /// Calcula todos los conceptos del plan para las variables de un trabajador.
    /// </summary>
    /// <param name="plan">Plan validado del esquema del contrato.</param>
    /// <param name="variables">Variables de entrada del trabajador y del período, por clave en mayúsculas.</param>
    /// <returns>Los importes de todos los conceptos, en orden de evaluación.</returns>
    /// <exception cref="ArgumentNullException">Se lanza si algún argumento es <c>null</c>.</exception>
    /// <exception cref="ErrorDeFormulaException">
    /// Se lanza si una fórmula no puede evaluarse; el mensaje identifica el concepto.
    /// </exception>
    public ResultadoDeCalculo Calcular(PlanDeCalculo plan, IReadOnlyDictionary<string, decimal> variables)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(variables);

        var contexto = new ContextoDeCalculo(plan, variables);
        var valores = new List<ValorDeConcepto>(plan.Conceptos.Count);

        foreach (ConceptoCompilado concepto in plan.Conceptos)
        {
            decimal importe;

            try
            {
                importe = concepto.Formula.Evaluar(contexto);
            }
            catch (ErrorDeFormulaException excepcion)
            {
                throw new ErrorDeFormulaException(
                    $"Error al evaluar el concepto '{concepto.Concepto.Clave}': {excepcion.Message}");
            }

            contexto.Registrar(concepto.Concepto.Clave, importe);
            valores.Add(new ValorDeConcepto(concepto.Concepto.Clave, importe));
        }

        return new ResultadoDeCalculo(valores, contexto.Resultados);
    }

    /// <summary>
    /// Contexto de evaluación de un trabajador: conceptos calculados, variables
    /// de entrada y parámetros, en ese orden de prioridad.
    /// </summary>
    private sealed class ContextoDeCalculo : IContextoDeEvaluacion
    {
        private readonly PlanDeCalculo _plan;
        private readonly IReadOnlyDictionary<string, decimal> _variables;

        public ContextoDeCalculo(PlanDeCalculo plan, IReadOnlyDictionary<string, decimal> variables)
        {
            _plan = plan;
            _variables = variables;
            Resultados = new Dictionary<string, decimal>(plan.Conceptos.Count, StringComparer.Ordinal);
        }

        public Dictionary<string, decimal> Resultados { get; }

        public void Registrar(string clave, decimal valor) => Resultados[clave] = valor;

        public bool TryObtenerValor(string nombre, out decimal valor)
            => Resultados.TryGetValue(nombre, out valor)
               || _variables.TryGetValue(nombre, out valor)
               || _plan.Parametros.TryGetValue(nombre, out valor);

        public decimal ConsultarTabla(string tabla, decimal valor, string campo)
        {
            if (!_plan.Tablas.TryGetValue(tabla, out TablaDeRangos? definicion))
            {
                throw new ErrorDeFormulaException($"La tabla '{tabla}' no está vigente para el período");
            }

            RangoDeTabla? rango = definicion.Buscar(valor);
            return rango is null ? 0m : TablaDeRangos.Campo(rango, campo);
        }
    }
}
