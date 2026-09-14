namespace HuimanNet.Domain.Enums;

/// <summary>
/// Modalidad bajo la que se paga un contrato de trabajo y, por tanto, el
/// conjunto de reglas de cálculo que se le aplica.
/// </summary>
/// <remarks>
/// Un mismo empleado puede tener varios contratos con esquemas distintos
/// (sueldo mixto): por ejemplo, una parte registrada ante el IMSS y otra
/// pagada a través de un sindicato o cooperativa, o por honorarios.
/// </remarks>
public enum EsquemaDePago
{
    /// <summary>Valor no especificado. Nunca debe persistirse.</summary>
    NoEspecificado = 0,

    /// <summary>Nómina fiscal: salario registrado ante el IMSS, con ISR, cuotas obrero-patronales, INFONAVIT e ISN.</summary>
    Imss = 1,

    /// <summary>Complemento o pago íntegro vía sindicato o cooperativa, sin retenciones fiscales.</summary>
    Sindicato = 2,

    /// <summary>Pago por honorarios (servicios profesionales o asimilados), con retención de ISR y, en su caso, IVA.</summary>
    Honorarios = 3,
}

/// <summary>
/// Combinación de esquemas de pago, usada para indicar a qué modalidades
/// aplica un concepto del catálogo de cálculo.
/// </summary>
[Flags]
public enum EsquemasDePago
{
    /// <summary>Ningún esquema.</summary>
    Ninguno = 0,

    /// <summary>Aplica a la nómina fiscal IMSS.</summary>
    Imss = 1,

    /// <summary>Aplica al esquema sindical.</summary>
    Sindicato = 2,

    /// <summary>Aplica a honorarios.</summary>
    Honorarios = 4,

    /// <summary>Aplica a todos los esquemas.</summary>
    Todos = Imss | Sindicato | Honorarios,
}

/// <summary>
/// Utilidades de conversión entre <see cref="EsquemaDePago"/> y <see cref="EsquemasDePago"/>.
/// </summary>
public static class EsquemasDePagoExtensiones
{
    /// <summary>
    /// Convierte un esquema concreto en su bandera equivalente.
    /// </summary>
    /// <param name="esquema">Esquema a convertir.</param>
    /// <returns>La bandera correspondiente.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si el esquema no está especificado.</exception>
    public static EsquemasDePago ComoBandera(this EsquemaDePago esquema) => esquema switch
    {
        EsquemaDePago.Imss => EsquemasDePago.Imss,
        EsquemaDePago.Sindicato => EsquemasDePago.Sindicato,
        EsquemaDePago.Honorarios => EsquemasDePago.Honorarios,
        _ => throw new ArgumentOutOfRangeException(nameof(esquema), esquema, "Esquema de pago no especificado."),
    };

    /// <summary>
    /// Indica si un conjunto de esquemas incluye uno concreto.
    /// </summary>
    /// <param name="esquemas">Conjunto de esquemas.</param>
    /// <param name="esquema">Esquema buscado.</param>
    /// <returns><c>true</c> si el conjunto incluye el esquema.</returns>
    public static bool Incluye(this EsquemasDePago esquemas, EsquemaDePago esquema)
        => (esquemas & esquema.ComoBandera()) != 0;
}
