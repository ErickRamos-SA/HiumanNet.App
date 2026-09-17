using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Formulas;

namespace HuimanNet.Domain.Nomina;

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

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ContextoDeCalculo"/>.
        /// </summary>
        /// <param name="plan">Plan con los parámetros y tablas vigentes.</param>
        /// <param name="variables">Variables de entrada del trabajador.</param>
        public ContextoDeCalculo(PlanDeCalculo plan, IReadOnlyDictionary<string, decimal> variables)
        {
            _plan = plan;
            _variables = variables;
            Resultados = new Dictionary<string, decimal>(plan.Conceptos.Count, StringComparer.Ordinal);
        }

        /// <summary>Obtiene los importes de los conceptos ya calculados.</summary>
        /// <value>Diccionario por clave de concepto.</value>
        public Dictionary<string, decimal> Resultados { get; }

        /// <summary>Registra el importe de un concepto recién calculado.</summary>
        /// <param name="clave">Clave del concepto.</param>
        /// <param name="valor">Importe calculado.</param>
        public void Registrar(string clave, decimal valor) => Resultados[clave] = valor;

        /// <inheritdoc/>
        public bool TryObtenerValor(string nombre, out decimal valor)
            => Resultados.TryGetValue(nombre, out valor)
               || _variables.TryGetValue(nombre, out valor)
               || _plan.Parametros.TryGetValue(nombre, out valor);

        /// <inheritdoc/>
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
