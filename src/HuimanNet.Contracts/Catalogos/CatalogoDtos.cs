using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Catalogos;

/// <summary>
/// Parámetro del catálogo de cálculo.
/// </summary>
/// <param name="Id">Identificador.</param>
/// <param name="Clave">Clave usada en las fórmulas.</param>
/// <param name="Descripcion">Descripción.</param>
/// <param name="Grupo">Grupo de presentación.</param>
/// <param name="Valor">Valor.</param>
/// <param name="Unidad">Unidad de presentación.</param>
/// <param name="EmpresaId">Empresa a la que aplica, o <c>null</c> para el global.</param>
/// <param name="VigenteDesde">Inicio de vigencia.</param>
/// <param name="VigenteHasta">Fin de vigencia, o <c>null</c>.</param>
/// <param name="FechaModificacion">Última modificación, en UTC.</param>
public sealed record ParametroDeCalculoDto(
    Guid Id,
    string Clave,
    string Descripcion,
    string Grupo,
    decimal Valor,
    string Unidad,
    Guid? EmpresaId,
    DateOnly VigenteDesde,
    DateOnly? VigenteHasta,
    DateTimeOffset FechaModificacion);

/// <summary>
/// Petición para crear o actualizar un parámetro.
/// </summary>
/// <param name="Clave">Clave usada en las fórmulas (no se cambia al actualizar).</param>
/// <param name="Descripcion">Descripción.</param>
/// <param name="Grupo">Grupo de presentación.</param>
/// <param name="Valor">Valor.</param>
/// <param name="Unidad">Unidad de presentación.</param>
/// <param name="EmpresaId">Empresa a la que aplica, o <c>null</c> para el global.</param>
/// <param name="VigenteDesde">Inicio de vigencia.</param>
/// <param name="VigenteHasta">Fin de vigencia, o <c>null</c>.</param>
public sealed record GuardarParametroRequest(
    string Clave,
    string Descripcion,
    string Grupo,
    decimal Valor,
    string Unidad,
    Guid? EmpresaId,
    DateOnly VigenteDesde,
    DateOnly? VigenteHasta);

/// <summary>
/// Renglón de una tabla por rangos.
/// </summary>
/// <param name="LimiteInferior">Límite inferior.</param>
/// <param name="LimiteSuperior">Límite superior, o <c>null</c> para "en adelante".</param>
/// <param name="CuotaFija">Cuota fija.</param>
/// <param name="Porcentaje">Porcentaje como fracción.</param>
/// <param name="Valor">Valor directo.</param>
public sealed record RangoDeTablaDto(
    decimal LimiteInferior,
    decimal? LimiteSuperior,
    decimal CuotaFija,
    decimal Porcentaje,
    decimal Valor);

/// <summary>
/// Tabla por rangos con sus renglones.
/// </summary>
/// <param name="Id">Identificador.</param>
/// <param name="Clave">Clave usada en <c>TABLA("CLAVE"; ...)</c>.</param>
/// <param name="Descripcion">Descripción.</param>
/// <param name="EmpresaId">Empresa a la que aplica, o <c>null</c>.</param>
/// <param name="VigenteDesde">Inicio de vigencia.</param>
/// <param name="VigenteHasta">Fin de vigencia, o <c>null</c>.</param>
/// <param name="FechaModificacion">Última modificación, en UTC.</param>
/// <param name="Rangos">Renglones ordenados por límite inferior.</param>
public sealed record TablaDeRangosDto(
    Guid Id,
    string Clave,
    string Descripcion,
    Guid? EmpresaId,
    DateOnly VigenteDesde,
    DateOnly? VigenteHasta,
    DateTimeOffset FechaModificacion,
    IReadOnlyList<RangoDeTablaDto> Rangos);

/// <summary>
/// Petición para crear o actualizar una tabla por rangos.
/// </summary>
/// <param name="Clave">Clave (no se cambia al actualizar).</param>
/// <param name="Descripcion">Descripción.</param>
/// <param name="EmpresaId">Empresa a la que aplica, o <c>null</c>.</param>
/// <param name="VigenteDesde">Inicio de vigencia.</param>
/// <param name="VigenteHasta">Fin de vigencia, o <c>null</c>.</param>
/// <param name="Rangos">Renglones.</param>
public sealed record GuardarTablaRequest(
    string Clave,
    string Descripcion,
    Guid? EmpresaId,
    DateOnly VigenteDesde,
    DateOnly? VigenteHasta,
    IReadOnlyList<RangoDeTablaDto> Rangos);

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
    DateTimeOffset FechaModificacion);

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
    Guid? EmpresaId);

/// <summary>
/// Petición para probar una fórmula con valores de ejemplo.
/// </summary>
/// <param name="Formula">Fórmula a evaluar.</param>
/// <param name="Esquema">Esquema cuyo catálogo se usa como contexto.</param>
/// <param name="EmpresaId">Empresa cuyo catálogo se usa, o <c>null</c> para el global.</param>
/// <param name="Valores">Valores de las variables de entrada; las no indicadas valen cero.</param>
public sealed record ProbarFormulaRequest(
    string Formula,
    EsquemaDePago Esquema,
    Guid? EmpresaId,
    IReadOnlyList<Contracts.Nomina.ValorDto> Valores);

/// <summary>
/// Resultado de probar una fórmula.
/// </summary>
/// <param name="Valida">Si la fórmula compiló y se evaluó.</param>
/// <param name="Resultado">Valor obtenido, si es válida.</param>
/// <param name="Error">Mensaje de error, si no es válida.</param>
/// <param name="Referencias">Identificadores que la fórmula referencia.</param>
/// <param name="ConceptosPrevios">Valores de los conceptos del catálogo evaluados con los mismos valores de ejemplo.</param>
public sealed record ProbarFormulaResponse(
    bool Valida,
    decimal? Resultado,
    string? Error,
    IReadOnlyList<string> Referencias,
    IReadOnlyList<Contracts.Nomina.ValorDto> ConceptosPrevios);

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

/// <summary>
/// Variable de entrada disponible para las fórmulas.
/// </summary>
/// <param name="Clave">Identificador.</param>
/// <param name="Descripcion">Qué representa.</param>
/// <param name="Origen">De dónde toma su valor.</param>
public sealed record VariableDeCalculoDto(string Clave, string Descripcion, string Origen);

/// <summary>
/// Función disponible en las fórmulas.
/// </summary>
/// <param name="Firma">Firma de ejemplo.</param>
/// <param name="Descripcion">Qué hace.</param>
public sealed record FuncionDeFormulaDto(string Firma, string Descripcion);

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
