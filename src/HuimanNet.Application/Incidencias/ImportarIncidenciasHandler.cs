using HuimanNet.Application.Common;
using HuimanNet.Application.Documentos;
using HuimanNet.Application.Interfaces;
using HuimanNet.Application.Nomina;
using HuimanNet.Contracts.Incidencias;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Incidencias;

/// <summary>
/// Ejecuta <see cref="ImportarIncidenciasCommand"/>: lee un archivo de
/// incidencias del período, con el diseño de la hoja de incidencias del modelo
/// de referencia, y crea o actualiza la incidencia de cada trabajador.
/// </summary>
/// <remarks>
/// Los trabajadores se emparejan por la columna <c>Clave</c>. Si un trabajador
/// tiene varios contratos vigentes, la incidencia se aplica a todos salvo que
/// el archivo incluya una columna <c>Esquema</c> o <c>Razon Social</c> que
/// permita distinguirlos. Las filas con error se reportan sin detener la
/// importación del resto.
/// </remarks>
public sealed class ImportarIncidenciasHandler : IManejadorDeComando<ImportarIncidenciasCommand, ResultadoDeImportacionDto>
{
    private readonly IIncidenciaRepository _incidencias;
    private readonly IPeriodoRepository _periodos;
    private readonly IEmpleadoRepository _empleados;
    private readonly IRazonSocialRepository _razonesSociales;
    private readonly ArchivosDelPeriodo _archivos;
    private readonly ILectorDeArchivosTabulares _lector;
    private readonly ConstructorDePlanDeCalculo _constructor;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly RegistradorDeAuditoria _auditoria;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ImportarIncidenciasHandler"/>.
    /// </summary>
    /// <param name="incidencias">Repositorio de incidencias.</param>
    /// <param name="periodos">Repositorio de períodos.</param>
    /// <param name="empleados">Repositorio de empleados y contratos.</param>
    /// <param name="razonesSociales">Repositorio de razones sociales.</param>
    /// <param name="archivos">Lectura de los archivos del período.</param>
    /// <param name="lector">Lector de archivos tabulares.</param>
    /// <param name="constructor">Resolución del catálogo.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="auditoria">Registro de auditoría.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    public ImportarIncidenciasHandler(
        IIncidenciaRepository incidencias,
        IPeriodoRepository periodos,
        IEmpleadoRepository empleados,
        IRazonSocialRepository razonesSociales,
        ArchivosDelPeriodo archivos,
        ILectorDeArchivosTabulares lector,
        ConstructorDePlanDeCalculo constructor,
        AutorizadorDeCasosDeUso autorizador,
        RegistradorDeAuditoria auditoria,
        IUnitOfWork unitOfWork,
        TimeProvider reloj)
    {
        _incidencias = incidencias;
        _periodos = periodos;
        _empleados = empleados;
        _razonesSociales = razonesSociales;
        _archivos = archivos;
        _lector = lector;
        _constructor = constructor;
        _autorizador = autorizador;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    /// <inheritdoc/>
    public async Task<ResultadoDeImportacionDto> EjecutarAsync(
        ImportarIncidenciasCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        _autorizador.Exigir(AccionDelSistema.CapturarIncidencias);
        Guid empresaId = _autorizador.ResolverEmpresa(comando.EmpresaId);

        ArchivoDelPeriodo archivo = await _archivos.LeerAsync(
            comando.DocumentoId, empresaId, [TipoDocumento.Incidencia], cancellationToken);

        PeriodoCarga periodo = await _periodos.ObtenerPorIdAsync(archivo.Documento.PeriodoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El período '{archivo.Documento.PeriodoId}' no existe para la empresa.");

        if (periodo.Estado == EstadoPeriodo.Cerrado)
        {
            throw new PeriodoCerradoException(periodo.Id, periodo.Estado);
        }

        DateOnly fecha = periodo.FechaDeReferencia;
        TablaLeida tabla = _lector.Leer(archivo.Documento.NombreOriginal.Valor, archivo.Contenido, hojasPreferidas: ["Incidencias"]);

        IReadOnlyList<Contrato> contratos = await _empleados.ListarContratosVigentesAsync(empresaId, fecha, cancellationToken);
        IReadOnlyList<Empleado> empleados = await _empleados.ListarPorEmpresaAsync(empresaId, soloActivos: true, cancellationToken);
        IReadOnlyList<RazonSocial> razonesSociales = await _razonesSociales.ListarPorEmpresaAsync(empresaId, soloActivas: false, cancellationToken);
        IReadOnlyList<Incidencia> existentes = await _incidencias.ListarPorPeriodoAsync(periodo.Id, empresaId, cancellationToken);
        CatalogoResuelto catalogo = await _constructor.ResolverAsync(empresaId, fecha, cancellationToken);

        decimal diasPredeterminados = catalogo.ParametroObligatorio(ClavesDeParametro.DiasPeriodoPredeterminados);

        var contexto = new ContextoDeImportacion(
            empleados.ToDictionary(static e => e.Clave, StringComparer.OrdinalIgnoreCase),
            contratos.ToLookup(static c => c.EmpleadoId),
            razonesSociales.ToDictionary(static r => r.Id),
            existentes.ToDictionary(static i => i.ContratoId));

        ImportadorDeIncidencias.Plan plan = ImportadorDeIncidencias.Interpretar(tabla, contexto, diasPredeterminados);

        int creadas = 0, actualizadas = 0;
        DateTimeOffset ahora = _reloj.GetUtcNow();

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        foreach ((Guid contratoId, DatosDeIncidencia datos) in plan.Incidencias)
        {
            if (contexto.Existentes.TryGetValue(contratoId, out Incidencia? existente))
            {
                existente.Actualizar(datos, _autorizador.Usuario.UsuarioId, ahora);
                await _incidencias.ActualizarAsync(existente, cancellationToken);
                actualizadas++;
            }
            else
            {
                Incidencia nueva = Incidencia.Registrar(empresaId, periodo.Id, contratoId, datos, _autorizador.Usuario.UsuarioId, ahora);
                await _incidencias.AgregarAsync(nueva, cancellationToken);
                creadas++;
            }
        }

        await _auditoria.ExitoAsync(
            AccionAuditada.CapturaDeIncidencias, empresaId, nameof(PeriodoCarga), periodo.Id,
            $"importacion; documento={archivo.Documento.Id}; filas={plan.FilasLeidas}; creadas={creadas}; actualizadas={actualizadas}; errores={plan.Errores.Count}",
            cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return new ResultadoDeImportacionDto(plan.FilasLeidas, creadas, actualizadas, plan.Errores);
    }
}
