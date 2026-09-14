using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Entities;

/// <summary>
/// Diferencia detectada entre el cálculo del sistema y el resultado manual
/// para un concepto de un trabajador.
/// </summary>
/// <param name="ClaveEmpleado">Clave del trabajador en el archivo manual.</param>
/// <param name="ContratoId">Contrato del sistema con el que se emparejó, o <c>null</c> si no se encontró.</param>
/// <param name="ConceptoClave">Concepto comparado.</param>
/// <param name="ImporteSistema">Importe calculado por el sistema, o <c>null</c> si el sistema no lo calculó.</param>
/// <param name="ImporteManual">Importe del archivo manual, o <c>null</c> si no venía.</param>
/// <param name="Diferencia">Sistema menos manual.</param>
/// <param name="DentroDeTolerancia">Si la diferencia absoluta no supera la tolerancia del cotejo.</param>
public sealed record DiferenciaDeCotejo(
    string ClaveEmpleado,
    Guid? ContratoId,
    string ConceptoClave,
    decimal? ImporteSistema,
    decimal? ImporteManual,
    decimal Diferencia,
    bool DentroDeTolerancia);

/// <summary>
/// Comparación de una corrida del sistema contra el resultado calculado
/// manualmente por el equipo de nómina.
/// </summary>
/// <remarks>
/// El cotejo es la herramienta de la primera etapa: permite depurar el catálogo
/// de fórmulas hasta que el sistema reproduce el resultado manual. Se guarda
/// completo (incluidas las coincidencias fuera de tolerancia) para tener
/// trazabilidad de cada iteración.
/// </remarks>
public sealed class CotejoDeNomina
{
    private CotejoDeNomina(
        Guid id,
        Guid corridaId,
        Guid empresaId,
        DateTimeOffset fechaCotejo,
        Guid usuarioId,
        string nombreArchivo,
        decimal toleranciaAbsoluta,
        int totalComparaciones,
        int totalFueraDeTolerancia,
        IReadOnlyList<DiferenciaDeCotejo> diferencias)
    {
        Id = id;
        CorridaId = corridaId;
        EmpresaId = empresaId;
        FechaCotejo = fechaCotejo;
        UsuarioId = usuarioId;
        NombreArchivo = nombreArchivo;
        ToleranciaAbsoluta = toleranciaAbsoluta;
        TotalComparaciones = totalComparaciones;
        TotalFueraDeTolerancia = totalFueraDeTolerancia;
        Diferencias = diferencias;
    }

    /// <summary>Obtiene el identificador único.</summary>
    /// <value>Clave primaria.</value>
    public Guid Id { get; }

    /// <summary>Obtiene la corrida cotejada.</summary>
    /// <value>Identificador de <see cref="CorridaDeNomina"/>.</value>
    public Guid CorridaId { get; }

    /// <summary>Obtiene la empresa cliente.</summary>
    /// <value>Discriminante de aislamiento.</value>
    public Guid EmpresaId { get; }

    /// <summary>Obtiene el instante del cotejo.</summary>
    /// <value>Instante en UTC.</value>
    public DateTimeOffset FechaCotejo { get; }

    /// <summary>Obtiene el usuario que cotejó.</summary>
    /// <value>Identificador local de <see cref="Usuario"/>.</value>
    public Guid UsuarioId { get; }

    /// <summary>Obtiene el nombre del archivo manual.</summary>
    /// <value>Nombre original aportado por el usuario.</value>
    public string NombreArchivo { get; }

    /// <summary>Obtiene la tolerancia absoluta aceptada por concepto.</summary>
    /// <value>Importe; las diferencias menores o iguales se consideran coincidencia.</value>
    public decimal ToleranciaAbsoluta { get; }

    /// <summary>Obtiene el número de comparaciones realizadas.</summary>
    /// <value>Una por trabajador y concepto presente en el archivo.</value>
    public int TotalComparaciones { get; }

    /// <summary>Obtiene el número de comparaciones fuera de tolerancia.</summary>
    /// <value>Cero cuando el sistema reproduce el resultado manual.</value>
    public int TotalFueraDeTolerancia { get; }

    /// <summary>Obtiene el detalle de comparaciones.</summary>
    /// <value>Lista de sólo lectura, ordenada por trabajador y concepto.</value>
    public IReadOnlyList<DiferenciaDeCotejo> Diferencias { get; }

    /// <summary>
    /// Registra un cotejo.
    /// </summary>
    /// <param name="corridaId">Corrida cotejada.</param>
    /// <param name="empresaId">Empresa cliente.</param>
    /// <param name="usuarioId">Usuario que coteja.</param>
    /// <param name="nombreArchivo">Nombre del archivo manual.</param>
    /// <param name="toleranciaAbsoluta">Tolerancia absoluta por concepto.</param>
    /// <param name="diferencias">Comparaciones realizadas.</param>
    /// <param name="momento">Instante del cotejo, en UTC.</param>
    /// <returns>El cotejo listo para persistirse.</returns>
    /// <exception cref="NominaInvalidaException">Se lanza si no hubo comparaciones o la tolerancia es negativa.</exception>
    public static CotejoDeNomina Registrar(
        Guid corridaId,
        Guid empresaId,
        Guid usuarioId,
        string nombreArchivo,
        decimal toleranciaAbsoluta,
        IReadOnlyList<DiferenciaDeCotejo> diferencias,
        DateTimeOffset momento)
    {
        ArgumentNullException.ThrowIfNull(diferencias);

        if (toleranciaAbsoluta < 0)
        {
            throw new NominaInvalidaException("La tolerancia no puede ser negativa.");
        }

        if (diferencias.Count == 0)
        {
            throw new NominaInvalidaException(
                "El archivo no produjo ninguna comparación: compruebe que contiene las columnas Clave, Concepto e Importe.");
        }

        return new CotejoDeNomina(
            Guid.CreateVersion7(),
            corridaId,
            empresaId,
            momento,
            usuarioId,
            string.IsNullOrWhiteSpace(nombreArchivo) ? "resultado-manual" : nombreArchivo.Trim(),
            toleranciaAbsoluta,
            diferencias.Count,
            diferencias.Count(static d => !d.DentroDeTolerancia),
            diferencias);
    }

    /// <summary>
    /// Reconstruye un cotejo a partir de los datos persistidos.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="corridaId">Corrida.</param>
    /// <param name="empresaId">Empresa.</param>
    /// <param name="fechaCotejo">Instante del cotejo.</param>
    /// <param name="usuarioId">Usuario.</param>
    /// <param name="nombreArchivo">Nombre del archivo.</param>
    /// <param name="toleranciaAbsoluta">Tolerancia.</param>
    /// <param name="totalComparaciones">Total de comparaciones.</param>
    /// <param name="totalFueraDeTolerancia">Total fuera de tolerancia.</param>
    /// <param name="diferencias">Detalle.</param>
    /// <returns>La entidad rehidratada.</returns>
    /// <remarks>Uso exclusivo de la capa de persistencia.</remarks>
    public static CotejoDeNomina Rehidratar(
        Guid id,
        Guid corridaId,
        Guid empresaId,
        DateTimeOffset fechaCotejo,
        Guid usuarioId,
        string nombreArchivo,
        decimal toleranciaAbsoluta,
        int totalComparaciones,
        int totalFueraDeTolerancia,
        IReadOnlyList<DiferenciaDeCotejo> diferencias)
        => new(id, corridaId, empresaId, fechaCotejo, usuarioId, nombreArchivo, toleranciaAbsoluta,
            totalComparaciones, totalFueraDeTolerancia, diferencias);
}
