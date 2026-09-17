using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Catalogos;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Nomina;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Catalogos;

/// <summary>
/// Ejecuta los comandos de mantenimiento de parámetros, tablas, conceptos y explicaciones.
/// </summary>
/// <remarks>
/// Reservado a quien tenga la acción <see cref="AccionDelSistema.AdministrarCatalogosDeCalculo"/>.
/// Toda modificación queda en la bitácora: el catálogo <b>es</b> el algoritmo de la nómina.
/// </remarks>
public sealed class AdministrarCatalogosHandler
    : IManejadorDeComando<GuardarParametroCommand, ParametroDeCalculoDto>,
      IManejadorDeComando<EliminarParametroCommand>,
      IManejadorDeComando<GuardarTablaCommand, TablaDeRangosDto>,
      IManejadorDeComando<EliminarTablaCommand>,
      IManejadorDeComando<GuardarConceptoCommand, ConceptoDeNominaDto>,
      IManejadorDeComando<EliminarConceptoCommand>,
      IManejadorDeComando<GuardarExplicacionCommand, ExplicacionDeCalculoDto>,
      IManejadorDeComando<EliminarExplicacionCommand>
{
    private readonly ICatalogoDeCalculoRepository _catalogos;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly RegistradorDeAuditoria _auditoria;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AdministrarCatalogosHandler"/>.
    /// </summary>
    /// <param name="catalogos">Repositorio de catálogos.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="auditoria">Registro de auditoría.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    public AdministrarCatalogosHandler(
        ICatalogoDeCalculoRepository catalogos,
        AutorizadorDeCasosDeUso autorizador,
        RegistradorDeAuditoria auditoria,
        IUnitOfWork unitOfWork,
        TimeProvider reloj)
    {
        _catalogos = catalogos;
        _autorizador = autorizador;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    /// <inheritdoc/>
    public async Task<ParametroDeCalculoDto> EjecutarAsync(GuardarParametroCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ArgumentNullException.ThrowIfNull(comando.Datos);
        Exigir();

        GuardarParametroRequest d = comando.Datos;
        ParametroDeCalculo parametro;

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        if (comando.ParametroId is null)
        {
            parametro = ParametroDeCalculo.Crear(
                d.Clave, d.Descripcion, d.Grupo, d.Valor, d.Unidad, d.EmpresaId, d.VigenteDesde, d.VigenteHasta, _reloj.GetUtcNow());
            await _catalogos.AgregarParametroAsync(parametro, cancellationToken);
        }
        else
        {
            parametro = await _catalogos.ObtenerParametroAsync(comando.ParametroId.Value, cancellationToken)
                ?? throw new AccesoNoAutorizadoException($"El parámetro '{comando.ParametroId}' no existe.");
            parametro.Actualizar(d.Descripcion, d.Grupo, d.Valor, d.Unidad, d.VigenteDesde, d.VigenteHasta, _reloj.GetUtcNow());
            await _catalogos.ActualizarParametroAsync(parametro, cancellationToken);
        }

        await AuditarAsync("Parametro", parametro.Id, parametro.EmpresaId, $"clave={parametro.Clave}; valor={parametro.Valor}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return Mapeadores.ADto(parametro);
    }

    /// <inheritdoc/>
    public async Task EjecutarAsync(EliminarParametroCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        Exigir();

        ParametroDeCalculo parametro = await _catalogos.ObtenerParametroAsync(comando.ParametroId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El parámetro '{comando.ParametroId}' no existe.");

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _catalogos.EliminarParametroAsync(parametro.Id, cancellationToken);
        await AuditarAsync("Parametro", parametro.Id, parametro.EmpresaId, $"eliminacion; clave={parametro.Clave}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TablaDeRangosDto> EjecutarAsync(GuardarTablaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ArgumentNullException.ThrowIfNull(comando.Datos);
        Exigir();

        GuardarTablaRequest d = comando.Datos;
        IEnumerable<RangoDeTabla> rangos = (d.Rangos ?? [])
            .Select(static r => new RangoDeTabla(r.LimiteInferior, r.LimiteSuperior, r.CuotaFija, r.Porcentaje, r.Valor));

        TablaDeRangos tabla;

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        if (comando.TablaId is null)
        {
            tabla = TablaDeRangos.Crear(d.Clave, d.Descripcion, d.EmpresaId, d.VigenteDesde, d.VigenteHasta, rangos, _reloj.GetUtcNow());
            await _catalogos.AgregarTablaAsync(tabla, cancellationToken);
        }
        else
        {
            tabla = await _catalogos.ObtenerTablaAsync(comando.TablaId.Value, cancellationToken)
                ?? throw new AccesoNoAutorizadoException($"La tabla '{comando.TablaId}' no existe.");
            tabla.Actualizar(d.Descripcion, d.VigenteDesde, d.VigenteHasta, rangos, _reloj.GetUtcNow());
            await _catalogos.ActualizarTablaAsync(tabla, cancellationToken);
        }

        await AuditarAsync("Tabla", tabla.Id, tabla.EmpresaId, $"clave={tabla.Clave}; rangos={tabla.Rangos.Count}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return Mapeadores.ADto(tabla);
    }

    /// <inheritdoc/>
    public async Task EjecutarAsync(EliminarTablaCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        Exigir();

        TablaDeRangos tabla = await _catalogos.ObtenerTablaAsync(comando.TablaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La tabla '{comando.TablaId}' no existe.");

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _catalogos.EliminarTablaAsync(tabla.Id, cancellationToken);
        await AuditarAsync("Tabla", tabla.Id, tabla.EmpresaId, $"eliminacion; clave={tabla.Clave}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ConceptoDeNominaDto> EjecutarAsync(GuardarConceptoCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ArgumentNullException.ThrowIfNull(comando.Datos);
        Exigir();

        GuardarConceptoRequest d = comando.Datos;
        ConceptoDeNomina concepto;

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        if (comando.ConceptoId is null)
        {
            concepto = ConceptoDeNomina.Crear(
                d.Clave, d.Nombre, d.Descripcion, d.Tipo, d.Esquemas, d.Orden, d.Formula, d.VisibleEnRecibo, d.EmpresaId, _reloj.GetUtcNow(),
                d.AliasDeCotejo);

            if (!d.Activo)
            {
                concepto.Actualizar(
                    d.Nombre, d.Descripcion, d.Tipo, d.Esquemas, d.Orden, d.Formula, d.VisibleEnRecibo, false, d.AliasDeCotejo, _reloj.GetUtcNow());
            }

            await _catalogos.AgregarConceptoAsync(concepto, cancellationToken);
        }
        else
        {
            concepto = await _catalogos.ObtenerConceptoAsync(comando.ConceptoId.Value, cancellationToken)
                ?? throw new AccesoNoAutorizadoException($"El concepto '{comando.ConceptoId}' no existe.");
            concepto.Actualizar(
                d.Nombre, d.Descripcion, d.Tipo, d.Esquemas, d.Orden, d.Formula, d.VisibleEnRecibo, d.Activo, d.AliasDeCotejo, _reloj.GetUtcNow());
            await _catalogos.ActualizarConceptoAsync(concepto, cancellationToken);
        }

        await AuditarAsync("Concepto", concepto.Id, concepto.EmpresaId, $"clave={concepto.Clave}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return Mapeadores.ADto(concepto);
    }

    /// <inheritdoc/>
    public async Task EjecutarAsync(EliminarConceptoCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        Exigir();

        ConceptoDeNomina concepto = await _catalogos.ObtenerConceptoAsync(comando.ConceptoId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El concepto '{comando.ConceptoId}' no existe.");

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _catalogos.EliminarConceptoAsync(concepto.Id, cancellationToken);
        await AuditarAsync("Concepto", concepto.Id, concepto.EmpresaId, $"eliminacion; clave={concepto.Clave}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ExplicacionDeCalculoDto> EjecutarAsync(GuardarExplicacionCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ArgumentNullException.ThrowIfNull(comando.Datos);
        Exigir();

        GuardarExplicacionRequest d = comando.Datos;
        ExplicacionDeCalculo explicacion;

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        if (comando.ExplicacionId is null)
        {
            explicacion = ExplicacionDeCalculo.Crear(d.Esquema, d.Idioma, d.Orden, d.Titulo, d.Cuerpo, _reloj.GetUtcNow());
            await _catalogos.AgregarExplicacionAsync(explicacion, cancellationToken);
        }
        else
        {
            explicacion = await _catalogos.ObtenerExplicacionAsync(comando.ExplicacionId.Value, cancellationToken)
                ?? throw new AccesoNoAutorizadoException($"La explicación '{comando.ExplicacionId}' no existe.");
            explicacion.Actualizar(d.Orden, d.Titulo, d.Cuerpo, _reloj.GetUtcNow());
            await _catalogos.ActualizarExplicacionAsync(explicacion, cancellationToken);
        }

        await AuditarAsync("Explicacion", explicacion.Id, null, $"esquema={explicacion.Esquema}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return Mapeadores.ADto(explicacion);
    }

    /// <inheritdoc/>
    public async Task EjecutarAsync(EliminarExplicacionCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        Exigir();

        ExplicacionDeCalculo explicacion = await _catalogos.ObtenerExplicacionAsync(comando.ExplicacionId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"La explicación '{comando.ExplicacionId}' no existe.");

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);
        await _catalogos.EliminarExplicacionAsync(explicacion.Id, cancellationToken);
        await AuditarAsync("Explicacion", explicacion.Id, null, "eliminacion", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);
    }

    /// <summary>Exige el permiso de administrar los catálogos de cálculo.</summary>
    /// <exception cref="AccesoNoAutorizadoException">Se lanza si el usuario no lo tiene.</exception>
    private void Exigir() => _autorizador.Exigir(AccionDelSistema.AdministrarCatalogosDeCalculo);

    /// <summary>Registra en la bitácora una modificación del catálogo.</summary>
    /// <param name="recurso">Tipo de elemento modificado.</param>
    /// <param name="id">Identificador del elemento.</param>
    /// <param name="empresaId">Empresa del elemento, o <c>null</c> si es del catálogo general.</param>
    /// <param name="detalle">Operación realizada.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Tarea que finaliza al registrar el asiento.</returns>
    private Task AuditarAsync(string recurso, Guid id, Guid? empresaId, string detalle, CancellationToken cancellationToken)
        => _auditoria.ExitoAsync(AccionAuditada.AdministracionDeCatalogo, empresaId, recurso, id, detalle, cancellationToken);
}
