using HuimanNet.Application.Common;
using HuimanNet.Application.Documentos;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Nomina;

/// <summary>
/// Ejecuta <see cref="CotejarNominaCommand"/>: lee el archivo manual, lo
/// empareja con los resultados del sistema por clave de trabajador y concepto,
/// y registra las diferencias.
/// </summary>
/// <remarks>
/// El archivo puede venir en formato <b>largo</b> (columnas Clave, Concepto e
/// Importe) o <b>ancho</b> (columna Clave y una columna por concepto, con la
/// clave del catálogo o con el encabezado de la hoja de nómina del modelo de
/// referencia). Si un trabajador tiene varios contratos que calculan el mismo
/// concepto, el sistema compara la suma.
/// <para>
/// El archivo manual no viaja en la petición: es un documento de resultados o
/// de ajuste publicado en el mismo período que la corrida.
/// </para>
/// </remarks>
public sealed class CotejarNominaHandler : IManejadorDeComando<CotejarNominaCommand, CotejoDto>
{
    private readonly ICorridaDeNominaRepository _corridas;
    private readonly ICotejoRepository _cotejos;
    private readonly ArchivosDelPeriodo _archivos;
    private readonly ILectorDeArchivosTabulares _lector;
    private readonly ConstructorDePlanDeCalculo _constructor;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly RegistradorDeAuditoria _auditoria;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CotejarNominaHandler"/>.
    /// </summary>
    /// <param name="corridas">Repositorio de corridas.</param>
    /// <param name="cotejos">Repositorio de cotejos.</param>
    /// <param name="archivos">Lectura de los archivos del período.</param>
    /// <param name="lector">Lector de archivos tabulares.</param>
    /// <param name="constructor">Resolución del catálogo, para los alias de cotejo de los conceptos.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="auditoria">Registro de auditoría.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    public CotejarNominaHandler(
        ICorridaDeNominaRepository corridas,
        ICotejoRepository cotejos,
        ArchivosDelPeriodo archivos,
        ILectorDeArchivosTabulares lector,
        ConstructorDePlanDeCalculo constructor,
        AutorizadorDeCasosDeUso autorizador,
        RegistradorDeAuditoria auditoria,
        IUnitOfWork unitOfWork,
        TimeProvider reloj)
    {
        _corridas = corridas;
        _cotejos = cotejos;
        _archivos = archivos;
        _lector = lector;
        _constructor = constructor;
        _autorizador = autorizador;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    /// <inheritdoc/>
    public async Task<CotejoDto> EjecutarAsync(CotejarNominaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        _autorizador.Exigir(AccionDelSistema.CotejarNomina);
        Guid empresaId = _autorizador.ResolverEmpresa(comando.EmpresaId);

        CorridaDeNomina corrida = await _corridas.ObtenerAsync(comando.CorridaId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La corrida '{comando.CorridaId}' no existe.");

        // Falla antes de leer el archivo si la corrida ya no admite cotejo.
        corrida.MarcarCotejada();

        ArchivoDelPeriodo archivo = await _archivos.LeerAsync(
            comando.DocumentoId, empresaId, [TipoDocumento.Resultado, TipoDocumento.Ajuste], cancellationToken);

        if (archivo.Documento.PeriodoId != corrida.PeriodoId)
        {
            throw new DocumentoInvalidoException("El archivo del cálculo manual debe pertenecer al mismo período que la corrida.");
        }

        string nombreArchivo = archivo.Documento.NombreOriginal.Valor;
        IReadOnlyList<ResultadoDeNomina> resultados = await _corridas.ListarResultadosAsync(corrida.Id, empresaId, cancellationToken);
        TablaLeida tabla = _lector.Leer(nombreArchivo, archivo.Contenido, hojasPreferidas: ["Nomina", "Resultados"]);
        CatalogoResuelto catalogo = await _constructor.ResolverAsync(empresaId, corrida.FechaDeReferencia, cancellationToken);

        IReadOnlyList<DiferenciaDeCotejo> diferencias = LectorDeResultadosManuales.Comparar(
            tabla, resultados, LectorDeResultadosManuales.IndiceDeAlias(catalogo.Conceptos), comando.ToleranciaAbsoluta);

        CotejoDeNomina cotejo = CotejoDeNomina.Registrar(
            corrida.Id, empresaId, _autorizador.Usuario.UsuarioId, nombreArchivo,
            comando.ToleranciaAbsoluta, diferencias, _reloj.GetUtcNow());

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _cotejos.AgregarAsync(cotejo, cancellationToken);
        await _corridas.ActualizarAsync(corrida, cancellationToken);
        await _auditoria.ExitoAsync(
            AccionAuditada.CotejoDeNomina, empresaId, nameof(CorridaDeNomina), corrida.Id,
            $"documento={archivo.Documento.Id}; comparaciones={cotejo.TotalComparaciones}; fuera={cotejo.TotalFueraDeTolerancia}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return Mapeadores.ADto(cotejo, _autorizador.Usuario.NombreCompleto, incluirDetalle: true);
    }
}
