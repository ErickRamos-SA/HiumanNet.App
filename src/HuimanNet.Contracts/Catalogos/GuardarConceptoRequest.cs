using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Catalogos;

/// <summary>
/// Petición para crear o actualizar un concepto.
/// </summary>
/// <param name="Clave">Clave (no se cambia al actualizar).</param>
/// <param name="Nombre">Nombre corto.</param>
/// <param name="Descripcion">Explicación.</param>
/// <param name="Tipo">Naturaleza.</param>
/// <param name="Esquemas">Esquemas a los que aplica.</param>
/// <param name="Orden">Orden de presentación.</param>
/// <param name="Formula">Fórmula.</param>
/// <param name="VisibleEnRecibo">Si se muestra en el recibo.</param>
/// <param name="Activo">Si participa en el cálculo.</param>
/// <param name="EmpresaId">Empresa a la que aplica, o <c>null</c>.</param>
/// <param name="AliasDeCotejo">
/// Otros encabezados del concepto en el archivo de resultados manual;
/// <c>null</c> o vacía si el archivo usa la clave.
/// </param>
public sealed record GuardarConceptoRequest(
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
    IReadOnlyList<string>? AliasDeCotejo = null);
