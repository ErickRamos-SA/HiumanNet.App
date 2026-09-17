using HuimanNet.Domain.Enums;

namespace HuimanNet.Infrastructure.Persistence.Semillas;

/// <summary>Sección de explicación del catálogo inicial.</summary>
/// <param name="Esquema">Esquema.</param>
/// <param name="Idioma">Idioma.</param>
/// <param name="Orden">Orden.</param>
/// <param name="Titulo">Título.</param>
/// <param name="Cuerpo">Cuerpo.</param>
public sealed record ExplicacionInicial(EsquemaDePago Esquema, Idioma Idioma, int Orden, string Titulo, string Cuerpo);
