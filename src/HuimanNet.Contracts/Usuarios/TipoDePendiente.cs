namespace HuimanNet.Contracts.Usuarios;

/// <summary>
/// Clase de tarea que el panel de inicio sugiere al usuario.
/// </summary>
public enum TipoDePendiente
{
    /// <summary>Valor no especificado. Nunca se envía.</summary>
    NoEspecificado = 0,

    /// <summary>La empresa cliente aún no subió documentos a un período abierto.</summary>
    SubirDocumentos = 1,

    /// <summary>Hay resultados disponibles para que la empresa cliente los descargue.</summary>
    DescargarResultados = 2,

    /// <summary>Un período recibió los documentos y espera a nómina.</summary>
    ProcesarPeriodo = 3,

    /// <summary>Una corrida calculada espera el cotejo contra el cálculo manual.</summary>
    CotejarCorrida = 4,
}
