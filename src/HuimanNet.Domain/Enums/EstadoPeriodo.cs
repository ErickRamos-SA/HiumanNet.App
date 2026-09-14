namespace HuimanNet.Domain.Enums;

/// <summary>
/// Estado del ciclo de intercambio de un período de carga.
/// </summary>
/// <remarks>
/// Transiciones válidas:
/// <c>Abierto → Recibido → EnProceso → ResultadosDisponibles → Cerrado</c>.
/// El retroceso de estado no está permitido; consulte
/// <see cref="Entities.PeriodoCarga"/>.
/// </remarks>
public enum EstadoPeriodo
{
    /// <summary>Valor no especificado. Nunca debe persistirse.</summary>
    NoEspecificado = 0,

    /// <summary>Admite cargas de la empresa cliente; aún no se ha recibido nada.</summary>
    Abierto = 1,

    /// <summary>Hay al menos un documento disponible del cliente. Sigue admitiendo cargas.</summary>
    Recibido = 2,

    /// <summary>El operador de nómina descargó los documentos y trabaja fuera del sistema. No admite cargas del cliente.</summary>
    EnProceso = 3,

    /// <summary>El operador publicó los archivos de resultado o ajuste.</summary>
    ResultadosDisponibles = 4,

    /// <summary>Ciclo terminado. No admite cargas de ningún rol.</summary>
    Cerrado = 5,
}
