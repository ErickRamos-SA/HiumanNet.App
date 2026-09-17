namespace HuimanNet.Domain.Enums;

/// <summary>
/// Reglas del ciclo de un período que dependen sólo de su estado.
/// </summary>
/// <remarks>
/// Las comparten la entidad <see cref="Entities.PeriodoCarga"/> y las
/// interfaces (web y app), para que ninguna reimplemente la secuencia del ciclo
/// ni el momento en que se admite el cálculo.
/// </remarks>
public static class EstadoPeriodoExtensiones
{
    /// <summary>
    /// Devuelve el estado que sigue en el ciclo de intercambio.
    /// </summary>
    /// <param name="estado">Estado actual.</param>
    /// <returns>
    /// El siguiente estado, o <c>null</c> si el período está cerrado o el estado
    /// no está especificado.
    /// </returns>
    /// <remarks>
    /// Es el avance que ofrecen las interfaces. La entidad admite además saltar
    /// estados hacia adelante (por ejemplo, de <see cref="EstadoPeriodo.Abierto"/>
    /// a <see cref="EstadoPeriodo.EnProceso"/> cuando la empresa captura sus
    /// incidencias sin enviar archivos), pero nunca retroceder.
    /// </remarks>
    public static EstadoPeriodo? Siguiente(this EstadoPeriodo estado) => estado switch
    {
        EstadoPeriodo.Abierto => EstadoPeriodo.Recibido,
        EstadoPeriodo.Recibido => EstadoPeriodo.EnProceso,
        EstadoPeriodo.EnProceso => EstadoPeriodo.ResultadosDisponibles,
        EstadoPeriodo.ResultadosDisponibles => EstadoPeriodo.Cerrado,
        _ => null,
    };

    /// <summary>
    /// Indica si un período en este estado admite calcular o reprocesar la nómina.
    /// </summary>
    /// <param name="estado">Estado del período.</param>
    /// <returns>
    /// <c>true</c> desde que hay al menos un archivo de la empresa
    /// (<see cref="EstadoPeriodo.Recibido"/>) hasta que el período se cierra.
    /// </returns>
    public static bool AdmiteCalculo(this EstadoPeriodo estado)
        => estado is EstadoPeriodo.Recibido or EstadoPeriodo.EnProceso or EstadoPeriodo.ResultadosDisponibles;
}
