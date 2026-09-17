using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;
using HuimanNet.Domain.Services;
using HuimanNet.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace HuimanNet.Application.Documentos.Commands;

/// <summary>
/// Ejecuta <see cref="SolicitarCargaDocumentoCommand"/>: autoriza, valida,
/// reserva el documento y emite la URL SAS de escritura.
/// </summary>
/// <remarks>
/// Orden deliberado de las comprobaciones — <b>todas ocurren antes de firmar
/// nada</b>:
/// <list type="number">
///   <item><description>El rol puede cargar ese tipo de documento.</description></item>
///   <item><description>La empresa objetivo es la del token, salvo rol transversal.</description></item>
///   <item><description>El período existe, pertenece a esa empresa y admite la carga.</description></item>
///   <item><description>La extensión y el tamaño cumplen la política.</description></item>
/// </list>
/// Sólo entonces se emite un SAS de un único blob, permiso de escritura y
/// vigencia de minutos (ARQUITECTURA.md §6.2).
/// </remarks>
public sealed class SolicitarCargaDocumentoHandler
    : IManejadorDeComando<SolicitarCargaDocumentoCommand, SolicitarCargaResponse>
{
    private readonly IDocumentoRepository _documentos;
    private readonly IPeriodoRepository _periodos;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IAlmacenDocumentos _almacen;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PoliticaDeAcceso _politicaDeAcceso;
    private readonly ValidadorDeDocumento _validador;
    private readonly IValidadorDeEntrada<SolicitarCargaDocumentoCommand> _validadorDeEntrada;
    private readonly TimeProvider _reloj;
    private readonly ILogger<SolicitarCargaDocumentoHandler> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SolicitarCargaDocumentoHandler"/>.
    /// </summary>
    /// <param name="documentos">Repositorio de documentos.</param>
    /// <param name="periodos">Repositorio de períodos.</param>
    /// <param name="auditoria">Repositorio de la bitácora de auditoría.</param>
    /// <param name="almacen">Almacén de documentos que emite las URL firmadas.</param>
    /// <param name="usuarioActual">Identidad efectiva del solicitante.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="politicaDeAcceso">Matriz de permisos por rol y tipo.</param>
    /// <param name="validador">Validador de la política de carga.</param>
    /// <param name="validadorDeEntrada">Validador de la forma del comando.</param>
    /// <param name="reloj">Proveedor de tiempo del sistema.</param>
    /// <param name="logger">Registro de eventos.</param>
    public SolicitarCargaDocumentoHandler(
        IDocumentoRepository documentos,
        IPeriodoRepository periodos,
        IAuditoriaRepository auditoria,
        IAlmacenDocumentos almacen,
        IUsuarioActual usuarioActual,
        IUnitOfWork unitOfWork,
        PoliticaDeAcceso politicaDeAcceso,
        ValidadorDeDocumento validador,
        IValidadorDeEntrada<SolicitarCargaDocumentoCommand> validadorDeEntrada,
        TimeProvider reloj,
        ILogger<SolicitarCargaDocumentoHandler> logger)
    {
        _documentos = documentos;
        _periodos = periodos;
        _auditoria = auditoria;
        _almacen = almacen;
        _usuarioActual = usuarioActual;
        _unitOfWork = unitOfWork;
        _politicaDeAcceso = politicaDeAcceso;
        _validador = validador;
        _validadorDeEntrada = validadorDeEntrada;
        _reloj = reloj;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="comando"/> es <c>null</c>.</exception>
    /// <exception cref="EntradaInvalidaException">Se lanza si el comando está incompleto o mal formado.</exception>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si el rol no puede cargar ese tipo de documento, si el usuario no
    /// tiene habilitada la acción de carga o si el período no pertenece a la
    /// empresa del solicitante.
    /// </exception>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si la extensión no está permitida o se excede el tamaño máximo.
    /// </exception>
    /// <exception cref="PeriodoCerradoException">
    /// Se lanza si el período ya no admite cargas de ese tipo.
    /// </exception>
    public async Task<SolicitarCargaResponse> EjecutarAsync(
        SolicitarCargaDocumentoCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        // La validación vive en el caso de uso: la web y la API la aplican igual.
        _validadorDeEntrada.Validar(comando).GarantizarValido();

        _politicaDeAcceso.GarantizarPuedeCargar(_usuarioActual.Rol, _usuarioActual.Permisos, comando.Tipo);

        Guid empresaId = _politicaDeAcceso.ResolverEmpresaObjetivo(
            _usuarioActual.Rol, _usuarioActual.EmpresaId, _usuarioActual.Empresas, comando.EmpresaId);

        PeriodoCarga periodo =
            await _periodos.ObtenerPorIdAsync(comando.PeriodoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException(
                $"El período '{comando.PeriodoId}' no existe o no pertenece a la empresa '{empresaId}'.");

        NombreArchivo nombre = NombreArchivo.Crear(comando.NombreArchivo);
        TamanoArchivo tamano = TamanoArchivo.DesdeBytes(comando.TamanoBytes);

        _validador.Validar(nombre, tamano, comando.Tipo, periodo);

        DateTimeOffset ahora = _reloj.GetUtcNow();

        Documento documento = Documento.Solicitar(
            empresaId,
            periodo.Id,
            comando.Tipo,
            nombre,
            tamano,
            _usuarioActual.UsuarioId,
            ahora);

        EnlaceTemporal enlace =
            await _almacen.CrearEnlaceDeEscrituraAsync(documento.RutaBlob, cancellationToken);

        await using ITransaccion transaccion =
            await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        await _documentos.AgregarAsync(documento, cancellationToken);

        await _auditoria.AgregarAsync(
            RegistroAuditoria.Exitoso(
                AccionAuditada.SolicitudDeCarga,
                _usuarioActual.UsuarioId,
                empresaId,
                nameof(Documento),
                documento.Id,
                ahora,
                $"tipo={comando.Tipo}; bytes={comando.TamanoBytes}",
                _usuarioActual.DireccionIp),
            cancellationToken);

        await transaccion.ConfirmarAsync(cancellationToken);

        _logger.LogInformation(
            "Carga autorizada. Documento {DocumentoId} del período {PeriodoId} (tipo {Tipo}).",
            documento.Id, periodo.Id, comando.Tipo);

        return new SolicitarCargaResponse(documento.Id, enlace.Url, enlace.ExpiraEn);
    }
}
