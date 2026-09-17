namespace HuimanNet.Domain.Nomina;

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
