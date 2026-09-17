namespace HuimanNet.Domain.Enums;

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
