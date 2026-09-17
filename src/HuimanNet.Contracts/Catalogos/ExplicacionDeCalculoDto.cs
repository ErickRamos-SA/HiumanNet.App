using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Catalogos;

/// <summary>
/// Sección de la explicación de cálculos.
/// </summary>
/// <param name="Id">Identificador.</param>
/// <param name="Esquema">Esquema de pago.</param>
/// <param name="Idioma">Idioma.</param>
/// <param name="Orden">Orden.</param>
/// <param name="Titulo">Título.</param>
/// <param name="Cuerpo">Cuerpo.</param>
public sealed record ExplicacionDeCalculoDto(
    Guid Id,
    EsquemaDePago Esquema,
    Idioma Idioma,
    int Orden,
    string Titulo,
    string Cuerpo);
