using HuimanNet.Domain.Enums;

namespace HuimanNet.Contracts.Documentos;

/// <summary>
/// Proyección de lectura de un documento, apta para listados en web y móvil.
/// </summary>
/// <param name="Id">Identificador único del documento.</param>
/// <param name="PeriodoId">Período al que pertenece.</param>
/// <param name="EmpresaId">Empresa propietaria.</param>
/// <param name="Tipo">Tipo funcional del documento.</param>
/// <param name="NombreArchivo">Nombre original aportado por quien lo cargó.</param>
/// <param name="TamanoBytes">Tamaño del archivo en bytes.</param>
/// <param name="Estado">Estado del ciclo de vida del documento.</param>
/// <param name="FechaSolicitud">Instante en que se solicitó la carga, en UTC.</param>
/// <param name="FechaCargaConfirmada">Instante en que se confirmó la carga, si ocurrió.</param>
/// <param name="CargadoPor">Nombre del usuario que realizó la carga.</param>
/// <param name="EsDescargable">Indica si el documento superó el escaneo y puede descargarse.</param>
public sealed record DocumentoDto(
    Guid Id,
    Guid PeriodoId,
    Guid EmpresaId,
    TipoDocumento Tipo,
    string NombreArchivo,
    long TamanoBytes,
    EstadoDocumento Estado,
    DateTimeOffset FechaSolicitud,
    DateTimeOffset? FechaCargaConfirmada,
    string CargadoPor,
    bool EsDescargable);
