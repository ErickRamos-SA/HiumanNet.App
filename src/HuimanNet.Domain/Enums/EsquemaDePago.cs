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
