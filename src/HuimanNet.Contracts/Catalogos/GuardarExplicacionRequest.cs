using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Catalogos;

/// <summary>
/// Petición para crear o actualizar una sección de explicación.
/// </summary>
/// <param name="Esquema">Esquema de pago.</param>
/// <param name="Idioma">Idioma.</param>
/// <param name="Orden">Orden.</param>
/// <param name="Titulo">Título.</param>
/// <param name="Cuerpo">Cuerpo.</param>
public sealed record GuardarExplicacionRequest(
    EsquemaDePago Esquema,
    Idioma Idioma,
    int Orden,
    string Titulo,
    string Cuerpo);
