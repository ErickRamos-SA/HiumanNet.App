using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Catalogos;

/// <summary>
/// Concepto del catálogo de cálculo.
/// </summary>
/// <param name="Id">Identificador.</param>
/// <param name="Clave">Clave usada por otras fórmulas.</param>
/// <param name="Nombre">Nombre corto.</param>
/// <param name="Descripcion">Explicación.</param>
/// <param name="Tipo">Naturaleza.</param>
/// <param name="Esquemas">Esquemas a los que aplica.</param>
/// <param name="Orden">Orden de presentación.</param>
/// <param name="Formula">Fórmula.</param>
/// <param name="VisibleEnRecibo">Si se muestra en el recibo.</param>
/// <param name="Activo">Si participa en el cálculo.</param>
/// <param name="EmpresaId">Empresa a la que aplica, o <c>null</c>.</param>
/// <param name="FechaModificacion">Última modificación, en UTC.</param>
/// <param name="AliasDeCotejo">Otros encabezados del concepto en el archivo de resultados manual.</param>
public sealed record ConceptoDeNominaDto(
    Guid Id,
    string Clave,
    string Nombre,
    string Descripcion,
    TipoDeConcepto Tipo,
    EsquemasDePago Esquemas,
    int Orden,
    string Formula,
    bool VisibleEnRecibo,
    bool Activo,
    Guid? EmpresaId,
    DateTimeOffset FechaModificacion,
    IReadOnlyList<string> AliasDeCotejo);
