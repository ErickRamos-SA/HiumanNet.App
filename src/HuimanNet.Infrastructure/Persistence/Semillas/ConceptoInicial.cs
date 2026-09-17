using HuimanNet.Domain.Enums;

namespace HuimanNet.Infrastructure.Persistence.Semillas;

/// <summary>Concepto del catálogo inicial.</summary>
/// <param name="Clave">Clave.</param>
/// <param name="Nombre">Nombre corto.</param>
/// <param name="Descripcion">Explicación.</param>
/// <param name="Tipo">Naturaleza.</param>
/// <param name="Esquemas">Esquemas a los que aplica.</param>
/// <param name="Orden">Orden.</param>
/// <param name="Formula">Fórmula.</param>
/// <param name="VisibleEnRecibo">Si se muestra en el recibo.</param>
public sealed record ConceptoInicial(
    string Clave, string Nombre, string Descripcion, TipoDeConcepto Tipo, EsquemasDePago Esquemas, int Orden, string Formula, bool VisibleEnRecibo);
