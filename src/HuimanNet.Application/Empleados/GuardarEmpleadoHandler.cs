using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Empleados;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;

namespace HuimanNet.Application.Empleados;

/// <summary>
/// Ejecuta <see cref="GuardarEmpleadoCommand"/> y <see cref="GuardarContratoCommand"/>.
/// </summary>
/// <remarks>
/// La clave del empleado es única por empresa: es el identificador con el que
/// los archivos de incidencias y de resultados manuales se refieren al trabajador.
/// </remarks>
public sealed class GuardarEmpleadoHandler
    : IManejadorDeComando<GuardarEmpleadoCommand, EmpleadoDto>,
      IManejadorDeComando<GuardarContratoCommand, ContratoDto>
{
    private readonly IEmpleadoRepository _empleados;
    private readonly IRazonSocialRepository _razonesSociales;
    private readonly IConsultasEmpleados _consultas;
    private readonly AutorizadorDeCasosDeUso _autorizador;
    private readonly RegistradorDeAuditoria _auditoria;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="GuardarEmpleadoHandler"/>.
    /// </summary>
    /// <param name="empleados">Repositorio de empleados.</param>
    /// <param name="razonesSociales">Repositorio de razones sociales.</param>
    /// <param name="consultas">Lado de lectura de empleados.</param>
    /// <param name="autorizador">Autorización del solicitante.</param>
    /// <param name="auditoria">Registro de auditoría.</param>
    /// <param name="unitOfWork">Coordinador de transacciones.</param>
    /// <param name="reloj">Proveedor de tiempo.</param>
    public GuardarEmpleadoHandler(
        IEmpleadoRepository empleados,
        IRazonSocialRepository razonesSociales,
        IConsultasEmpleados consultas,
        AutorizadorDeCasosDeUso autorizador,
        RegistradorDeAuditoria auditoria,
        IUnitOfWork unitOfWork,
        TimeProvider reloj)
    {
        _empleados = empleados;
        _razonesSociales = razonesSociales;
        _consultas = consultas;
        _autorizador = autorizador;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    /// <inheritdoc/>
    public async Task<EmpleadoDto> EjecutarAsync(
        GuardarEmpleadoCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ArgumentNullException.ThrowIfNull(comando.Datos);

        _autorizador.Exigir(AccionDelSistema.AdministrarEmpleados);
        Guid empresaId = _autorizador.ResolverEmpresa(comando.Datos.EmpresaId);

        GuardarEmpleadoRequest d = comando.Datos;
        var datos = new DatosPersonales(
            d.Nombre, d.ApellidoPaterno, d.ApellidoMaterno, d.Rfc, d.Curp, d.Nss, d.FechaNacimiento, d.Correo, d.Telefono);

        Empleado? existentePorClave = await _empleados.ObtenerPorClaveAsync(empresaId, d.Clave, cancellationToken);

        if (existentePorClave is not null && existentePorClave.Id != comando.EmpleadoId)
        {
            throw new CatalogoInvalidoException($"Ya existe un empleado con la clave '{d.Clave.Trim()}' en la empresa.");
        }

        Empleado empleado;

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        if (comando.EmpleadoId is null)
        {
            empleado = Empleado.Crear(empresaId, d.Clave, datos, _reloj.GetUtcNow());
            await _empleados.AgregarAsync(empleado, cancellationToken);
        }
        else
        {
            empleado = await _empleados.ObtenerPorIdAsync(comando.EmpleadoId.Value, empresaId, cancellationToken)
                ?? throw new AccesoNoAutorizadoException($"El empleado '{comando.EmpleadoId}' no existe.");
            empleado.Actualizar(d.Clave, datos, d.Activo);
            await _empleados.ActualizarAsync(empleado, cancellationToken);
        }

        await _auditoria.ExitoAsync(
            AccionAuditada.AdministracionDeCatalogo, empresaId, nameof(Empleado), empleado.Id,
            comando.EmpleadoId is null ? "alta" : "actualizacion", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return await _consultas.ObtenerAsync(empleado.Id, empresaId, cancellationToken)
            ?? Mapeadores.ADto(empleado, []);
    }

    /// <inheritdoc/>
    public async Task<ContratoDto> EjecutarAsync(
        GuardarContratoCommand comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ArgumentNullException.ThrowIfNull(comando.Datos);

        _autorizador.Exigir(AccionDelSistema.AdministrarEmpleados);
        Guid empresaId = _autorizador.ResolverEmpresa(comando.Datos.EmpresaId);

        Empleado empleado = await _empleados.ObtenerPorIdAsync(comando.EmpleadoId, empresaId, cancellationToken)
            ?? throw new AccesoNoAutorizadoException($"El empleado '{comando.EmpleadoId}' no existe.");

        GuardarContratoRequest d = comando.Datos;

        RazonSocial razonSocial = await _razonesSociales.ObtenerPorIdAsync(d.RazonSocialId, empresaId, cancellationToken)
            ?? throw new CatalogoInvalidoException("La razón social indicada no existe en la empresa.");

        var condiciones = new CondicionesDeContrato(
            d.SueldoPeriodoReal, d.SalarioDiarioFiscal, d.SalarioDiarioIntegrado, d.Zona,
            new CreditoInfonavit(d.InfonavitTipo, d.InfonavitValor, d.InfonavitSeguroVivienda),
            d.FonacotMensual, d.PensionAlimenticiaImporte, d.PensionAlimenticiaPorcentaje, d.PrestamoPersonalFijo,
            d.BonoFijo, d.HonorariosAplicaIva, d.PagaComplementoSindical);

        Contrato contrato;

        await using ITransaccion transaccion = await _unitOfWork.IniciarTransaccionAsync(cancellationToken);

        if (comando.ContratoId is null)
        {
            contrato = Contrato.Crear(
                empleado.Id, empresaId, razonSocial.Id, d.Esquema, d.NumeroTrabajador, d.Puesto, d.Departamento,
                d.TipoDeContrato, condiciones, d.FechaAlta, _reloj.GetUtcNow());

            if (d.FechaBaja is not null)
            {
                contrato.Actualizar(
                    razonSocial.Id, d.Esquema, d.NumeroTrabajador, d.Puesto, d.Departamento, d.TipoDeContrato,
                    condiciones, d.FechaAlta, d.FechaBaja, _reloj.GetUtcNow());
            }

            await _empleados.AgregarContratoAsync(contrato, cancellationToken);
        }
        else
        {
            contrato = await _empleados.ObtenerContratoAsync(comando.ContratoId.Value, empresaId, cancellationToken)
                ?? throw new AccesoNoAutorizadoException($"El contrato '{comando.ContratoId}' no existe.");

            if (contrato.EmpleadoId != empleado.Id)
            {
                throw new AccesoNoAutorizadoException("El contrato no pertenece al empleado indicado.");
            }

            contrato.Actualizar(
                razonSocial.Id, d.Esquema, d.NumeroTrabajador, d.Puesto, d.Departamento, d.TipoDeContrato,
                condiciones, d.FechaAlta, d.FechaBaja, _reloj.GetUtcNow());
            await _empleados.ActualizarContratoAsync(contrato, cancellationToken);
        }

        await _auditoria.ExitoAsync(
            AccionAuditada.AdministracionDeCatalogo, empresaId, nameof(Contrato), contrato.Id,
            $"esquema={contrato.Esquema}", cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return Mapeadores.ADto(contrato, razonSocial.Nombre);
    }
}
