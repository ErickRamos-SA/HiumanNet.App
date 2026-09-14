using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Formulas;

namespace HuimanNet.Domain.Nomina;

/// <summary>
/// Concepto del catálogo de cálculo: un valor con nombre que se obtiene
/// evaluando una fórmula sobre las variables, los parámetros y los conceptos
/// calculados antes.
/// </summary>
/// <remarks>
/// El catálogo de conceptos <b>es</b> el algoritmo de la nómina: añadir,
/// cambiar o corregir un cálculo consiste en editar fórmulas desde la
/// administración, sin tocar código. El motor ordena los conceptos por sus
/// dependencias, de modo que el administrador no necesita cuidar el orden
/// salvo para la presentación.
/// </remarks>
public sealed class ConceptoDeNomina
{
    /// <summary>Longitud máxima de la fórmula.</summary>
    public const int LongitudMaximaFormula = 4000;

    private ConceptoDeNomina(
        Guid id,
        string clave,
        string nombre,
        string descripcion,
        TipoDeConcepto tipo,
        EsquemasDePago esquemas,
        int orden,
        string formula,
        bool visibleEnRecibo,
        bool activo,
        Guid? empresaId,
        DateTimeOffset fechaModificacion)
    {
        Id = id;
        Clave = clave;
        Nombre = nombre;
        Descripcion = descripcion;
        Tipo = tipo;
        Esquemas = esquemas;
        Orden = orden;
        Formula = formula;
        VisibleEnRecibo = visibleEnRecibo;
        Activo = activo;
        EmpresaId = empresaId;
        FechaModificacion = fechaModificacion;
    }

    /// <summary>Obtiene el identificador único.</summary>
    /// <value>Clave primaria.</value>
    public Guid Id { get; }

    /// <summary>Obtiene la clave con la que otras fórmulas referencian el concepto.</summary>
    /// <value>Mayúsculas, dígitos y guion bajo; por ejemplo <c>BASE_GRAVABLE</c>.</value>
    public string Clave { get; }

    /// <summary>Obtiene el nombre corto para el recibo y los reportes.</summary>
    /// <value>Texto legible.</value>
    public string Nombre { get; private set; }

    /// <summary>Obtiene la explicación del concepto.</summary>
    /// <value>Texto que se muestra en la ayuda de cálculos.</value>
    public string Descripcion { get; private set; }

    /// <summary>Obtiene la naturaleza del concepto.</summary>
    /// <value>Base, percepción, deducción, patronal, costo o total.</value>
    public TipoDeConcepto Tipo { get; private set; }

    /// <summary>Obtiene los esquemas de pago a los que aplica.</summary>
    /// <value>Combinación de banderas.</value>
    public EsquemasDePago Esquemas { get; private set; }

    /// <summary>Obtiene el orden de presentación y de desempate al calcular.</summary>
    /// <value>Entero; menor primero.</value>
    public int Orden { get; private set; }

    /// <summary>Obtiene el texto de la fórmula.</summary>
    /// <value>Expresión en el lenguaje de fórmulas del sistema.</value>
    public string Formula { get; private set; }

    /// <summary>Indica si el concepto se muestra en el recibo o detalle del trabajador.</summary>
    /// <value><c>false</c> para valores intermedios.</value>
    public bool VisibleEnRecibo { get; private set; }

    /// <summary>Indica si el concepto participa en el cálculo.</summary>
    /// <value><c>false</c> para conceptos retirados que se conservan por historial.</value>
    public bool Activo { get; private set; }

    /// <summary>Obtiene la empresa a la que aplica el concepto.</summary>
    /// <value><c>null</c> para el catálogo global; una empresa cuando lo sobrescribe.</value>
    public Guid? EmpresaId { get; }

    /// <summary>Obtiene el instante de la última modificación.</summary>
    /// <value>Instante en UTC.</value>
    public DateTimeOffset FechaModificacion { get; private set; }

    /// <summary>
    /// Crea un concepto nuevo, validando su clave y compilando su fórmula.
    /// </summary>
    /// <param name="clave">Clave del concepto.</param>
    /// <param name="nombre">Nombre corto.</param>
    /// <param name="descripcion">Explicación del concepto.</param>
    /// <param name="tipo">Naturaleza del concepto.</param>
    /// <param name="esquemas">Esquemas a los que aplica.</param>
    /// <param name="orden">Orden de presentación.</param>
    /// <param name="formula">Texto de la fórmula.</param>
    /// <param name="visibleEnRecibo">Si se muestra en el recibo.</param>
    /// <param name="empresaId">Empresa a la que aplica, o <c>null</c>.</param>
    /// <param name="momento">Instante de creación, en UTC.</param>
    /// <returns>El concepto creado y activo.</returns>
    /// <exception cref="CatalogoInvalidoException">Se lanza si la clave, el tipo o los esquemas no son válidos.</exception>
    /// <exception cref="ErrorDeFormulaException">Se lanza si la fórmula tiene errores de sintaxis.</exception>
    public static ConceptoDeNomina Crear(
        string clave,
        string nombre,
        string descripcion,
        TipoDeConcepto tipo,
        EsquemasDePago esquemas,
        int orden,
        string formula,
        bool visibleEnRecibo,
        Guid? empresaId,
        DateTimeOffset momento)
    {
        string claveNormalizada = NormalizarClave(clave);
        ValidarTipoYEsquemas(tipo, esquemas);
        string formulaValidada = ValidarFormula(formula);

        return new ConceptoDeNomina(
            Guid.CreateVersion7(),
            claveNormalizada,
            Requerido(nombre, "nombre"),
            descripcion?.Trim() ?? string.Empty,
            tipo,
            esquemas,
            orden,
            formulaValidada,
            visibleEnRecibo,
            activo: true,
            empresaId,
            momento);
    }

    /// <summary>
    /// Reconstruye un concepto a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="clave">Clave.</param>
    /// <param name="nombre">Nombre corto.</param>
    /// <param name="descripcion">Descripción.</param>
    /// <param name="tipo">Tipo.</param>
    /// <param name="esquemas">Esquemas.</param>
    /// <param name="orden">Orden.</param>
    /// <param name="formula">Fórmula.</param>
    /// <param name="visibleEnRecibo">Visibilidad en el recibo.</param>
    /// <param name="activo">Estado.</param>
    /// <param name="empresaId">Empresa, si aplica.</param>
    /// <param name="fechaModificacion">Última modificación.</param>
    /// <returns>La entidad rehidratada.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static ConceptoDeNomina Rehidratar(
        Guid id,
        string clave,
        string nombre,
        string descripcion,
        TipoDeConcepto tipo,
        EsquemasDePago esquemas,
        int orden,
        string formula,
        bool visibleEnRecibo,
        bool activo,
        Guid? empresaId,
        DateTimeOffset fechaModificacion)
        => new(id, clave, nombre, descripcion, tipo, esquemas, orden, formula, visibleEnRecibo, activo, empresaId, fechaModificacion);

    /// <summary>
    /// Actualiza el concepto, validando la nueva fórmula.
    /// </summary>
    /// <param name="nombre">Nombre corto.</param>
    /// <param name="descripcion">Explicación.</param>
    /// <param name="tipo">Naturaleza.</param>
    /// <param name="esquemas">Esquemas a los que aplica.</param>
    /// <param name="orden">Orden de presentación.</param>
    /// <param name="formula">Texto de la fórmula.</param>
    /// <param name="visibleEnRecibo">Si se muestra en el recibo.</param>
    /// <param name="activo">Si participa en el cálculo.</param>
    /// <param name="momento">Instante de la modificación, en UTC.</param>
    /// <exception cref="CatalogoInvalidoException">Se lanza si el tipo o los esquemas no son válidos.</exception>
    /// <exception cref="ErrorDeFormulaException">Se lanza si la fórmula tiene errores de sintaxis.</exception>
    public void Actualizar(
        string nombre,
        string descripcion,
        TipoDeConcepto tipo,
        EsquemasDePago esquemas,
        int orden,
        string formula,
        bool visibleEnRecibo,
        bool activo,
        DateTimeOffset momento)
    {
        ValidarTipoYEsquemas(tipo, esquemas);
        string formulaValidada = ValidarFormula(formula);

        Nombre = Requerido(nombre, "nombre");
        Descripcion = descripcion?.Trim() ?? string.Empty;
        Tipo = tipo;
        Esquemas = esquemas;
        Orden = orden;
        Formula = formulaValidada;
        VisibleEnRecibo = visibleEnRecibo;
        Activo = activo;
        FechaModificacion = momento;
    }

    /// <summary>
    /// Compila la fórmula del concepto.
    /// </summary>
    /// <returns>La fórmula compilada.</returns>
    /// <exception cref="ErrorDeFormulaException">Se lanza si la fórmula tiene errores de sintaxis.</exception>
    public FormulaCompilada Compilar() => FormulaCompilada.Compilar(Formula);

    /// <summary>
    /// Normaliza y valida una clave de concepto, rechazando nombres reservados.
    /// </summary>
    /// <param name="clave">Clave aportada por el usuario.</param>
    /// <returns>La clave en mayúsculas.</returns>
    /// <exception cref="CatalogoInvalidoException">
    /// Se lanza si la clave no es válida, coincide con una variable de entrada
    /// o con el nombre de una función.
    /// </exception>
    public static string NormalizarClave(string? clave)
    {
        string normalizada = ParametroDeCalculo.NormalizarClave(clave);

        if (VariablesDeCalculo.EsVariable(normalizada))
        {
            throw new CatalogoInvalidoException(
                $"La clave '{normalizada}' está reservada para una variable de entrada del motor.");
        }

        if (FuncionesDeFormula.EsFuncion(normalizada))
        {
            throw new CatalogoInvalidoException(
                $"La clave '{normalizada}' coincide con el nombre de una función.");
        }

        return normalizada;
    }

    private static void ValidarTipoYEsquemas(TipoDeConcepto tipo, EsquemasDePago esquemas)
    {
        if (tipo == TipoDeConcepto.NoEspecificado || !Enum.IsDefined(tipo))
        {
            throw new CatalogoInvalidoException("Debe indicarse el tipo del concepto.");
        }

        if (esquemas == EsquemasDePago.Ninguno)
        {
            throw new CatalogoInvalidoException("El concepto debe aplicar al menos a un esquema de pago.");
        }
    }

    private static string ValidarFormula(string? formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
        {
            throw new ErrorDeFormulaException("La fórmula es obligatoria");
        }

        string texto = formula.Trim();

        if (texto.Length > LongitudMaximaFormula)
        {
            throw new ErrorDeFormulaException($"La fórmula no puede exceder {LongitudMaximaFormula} caracteres");
        }

        FormulaCompilada.Compilar(texto);
        return texto;
    }

    private static string Requerido(string? valor, string nombre)
        => string.IsNullOrWhiteSpace(valor)
            ? throw new CatalogoInvalidoException($"El {nombre} es obligatorio.")
            : valor.Trim();
}
