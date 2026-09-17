using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Catalogos;

/// <summary>
/// Explicación completa de un esquema: texto narrativo más el catálogo
/// vigente con el que el sistema calcula.
/// </summary>
/// <param name="Esquema">Esquema explicado.</param>
/// <param name="Idioma">Idioma del texto.</param>
/// <param name="FechaDeReferencia">Fecha para la que se resolvieron las vigencias.</param>
/// <param name="Secciones">Secciones narrativas.</param>
/// <param name="Conceptos">Conceptos en orden de evaluación.</param>
/// <param name="Parametros">Parámetros vigentes.</param>
/// <param name="Tablas">Tablas vigentes.</param>
/// <param name="Variables">Variables de entrada disponibles.</param>
/// <param name="Funciones">Funciones disponibles.</param>
/// <param name="ErrorDeCatalogo">Mensaje si el catálogo no permite construir el plan, o <c>null</c>.</param>
public sealed record ExplicacionCompletaDto(
    EsquemaDePago Esquema,
    Idioma Idioma,
    DateOnly FechaDeReferencia,
    IReadOnlyList<ExplicacionDeCalculoDto> Secciones,
    IReadOnlyList<ConceptoDeNominaDto> Conceptos,
    IReadOnlyList<ParametroDeCalculoDto> Parametros,
    IReadOnlyList<TablaDeRangosDto> Tablas,
    IReadOnlyList<VariableDeCalculoDto> Variables,
    IReadOnlyList<FuncionDeFormulaDto> Funciones,
    string? ErrorDeCatalogo);
