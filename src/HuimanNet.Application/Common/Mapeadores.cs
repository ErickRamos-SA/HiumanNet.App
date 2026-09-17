using HuimanNet.Contracts.Catalogos;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empleados;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Nomina;

namespace HuimanNet.Application.Common;

/// <summary>
/// Traduce entidades del dominio a sus contratos de transporte.
/// </summary>
/// <remarks>
/// Mapeo escrito a mano por requisito de compatibilidad con Native AOT: las
/// bibliotecas de mapeo por convención usan reflexión en tiempo de ejecución y
/// no sobreviven al recorte (ESPECIFICACION.md §2).
/// </remarks>
public static class Mapeadores
{
    /// <summary>
    /// Proyecta una razón social.
    /// </summary>
    /// <param name="razonSocial">Entidad de origen.</param>
    /// <returns>El DTO equivalente.</returns>
    public static RazonSocialDto ADto(RazonSocial razonSocial)
    {
        ArgumentNullException.ThrowIfNull(razonSocial);
        ConfiguracionDeRazonSocial c = razonSocial.Configuracion;

        return new RazonSocialDto(
            razonSocial.Id, razonSocial.EmpresaId, razonSocial.Nombre, razonSocial.Rfc, razonSocial.RegistroPatronal,
            razonSocial.Zona, c.TipoDeServicio, c.SubsidioAbsorbido, c.AplicaFaltasProporcionales,
            c.ModalidadDeComision, c.PorcentajeComision, c.ZonaIsn, c.TasaIva, c.PorcentajeOtrosCostos,
            c.PrimaDeRiesgo, razonSocial.BancoDispersor, razonSocial.Activa);
    }

    /// <summary>
    /// Proyecta un contrato.
    /// </summary>
    /// <param name="contrato">Entidad de origen.</param>
    /// <param name="razonSocialNombre">Nombre de la razón social pagadora.</param>
    /// <returns>El DTO equivalente.</returns>
    public static ContratoDto ADto(Contrato contrato, string razonSocialNombre)
    {
        ArgumentNullException.ThrowIfNull(contrato);
        CondicionesDeContrato c = contrato.Condiciones;

        return new ContratoDto(
            contrato.Id, contrato.EmpleadoId, contrato.RazonSocialId, razonSocialNombre, contrato.Esquema,
            contrato.NumeroTrabajador, contrato.Puesto, contrato.Departamento, contrato.TipoDeContrato,
            c.SueldoPeriodoReal, c.SalarioDiarioFiscal, c.SalarioDiarioIntegrado, c.Zona,
            c.Infonavit.Tipo, c.Infonavit.Valor, c.Infonavit.SeguroDeVivienda, c.FonacotMensual,
            c.PensionAlimenticiaImporte, c.PensionAlimenticiaPorcentaje, c.PrestamoPersonalFijo, c.BonoFijo,
            c.HonorariosAplicaIva, c.PagaComplementoSindical, contrato.FechaAlta, contrato.FechaBaja, contrato.Activo);
    }

    /// <summary>
    /// Proyecta un empleado con sus contratos.
    /// </summary>
    /// <param name="empleado">Entidad de origen.</param>
    /// <param name="contratos">Contratos ya proyectados.</param>
    /// <returns>El DTO equivalente.</returns>
    public static EmpleadoDto ADto(Empleado empleado, IReadOnlyList<ContratoDto> contratos)
    {
        ArgumentNullException.ThrowIfNull(empleado);
        DatosPersonales d = empleado.Datos;

        return new EmpleadoDto(
            empleado.Id, empleado.EmpresaId, empleado.Clave, d.Nombre, d.ApellidoPaterno, d.ApellidoMaterno,
            empleado.NombreCompleto, d.Rfc, d.Curp, d.Nss, d.FechaNacimiento, d.Correo, d.Telefono,
            empleado.Activo, empleado.FechaAlta, contratos);
    }

    /// <summary>
    /// Proyecta un parámetro.
    /// </summary>
    /// <param name="parametro">Entidad de origen.</param>
    /// <returns>El DTO equivalente.</returns>
    public static ParametroDeCalculoDto ADto(ParametroDeCalculo parametro)
    {
        ArgumentNullException.ThrowIfNull(parametro);

        return new ParametroDeCalculoDto(
            parametro.Id, parametro.Clave, parametro.Descripcion, parametro.Grupo, parametro.Valor, parametro.Unidad,
            parametro.EmpresaId, parametro.VigenteDesde, parametro.VigenteHasta, parametro.FechaModificacion);
    }

    /// <summary>
    /// Proyecta una tabla por rangos.
    /// </summary>
    /// <param name="tabla">Entidad de origen.</param>
    /// <returns>El DTO equivalente.</returns>
    public static TablaDeRangosDto ADto(TablaDeRangos tabla)
    {
        ArgumentNullException.ThrowIfNull(tabla);

        return new TablaDeRangosDto(
            tabla.Id, tabla.Clave, tabla.Descripcion, tabla.EmpresaId, tabla.VigenteDesde, tabla.VigenteHasta,
            tabla.FechaModificacion,
            tabla.Rangos.Select(static r => new RangoDeTablaDto(r.LimiteInferior, r.LimiteSuperior, r.CuotaFija, r.Porcentaje, r.Valor)).ToList());
    }

    /// <summary>
    /// Proyecta un concepto.
    /// </summary>
    /// <param name="concepto">Entidad de origen.</param>
    /// <returns>El DTO equivalente.</returns>
    public static ConceptoDeNominaDto ADto(ConceptoDeNomina concepto)
    {
        ArgumentNullException.ThrowIfNull(concepto);

        return new ConceptoDeNominaDto(
            concepto.Id, concepto.Clave, concepto.Nombre, concepto.Descripcion, concepto.Tipo, concepto.Esquemas,
            concepto.Orden, concepto.Formula, concepto.VisibleEnRecibo, concepto.Activo, concepto.EmpresaId,
            concepto.FechaModificacion, concepto.AliasDeCotejo);
    }

    /// <summary>
    /// Proyecta una sección de explicación.
    /// </summary>
    /// <param name="explicacion">Entidad de origen.</param>
    /// <returns>El DTO equivalente.</returns>
    public static ExplicacionDeCalculoDto ADto(ExplicacionDeCalculo explicacion)
    {
        ArgumentNullException.ThrowIfNull(explicacion);

        return new ExplicacionDeCalculoDto(
            explicacion.Id, explicacion.Esquema, explicacion.Idioma, explicacion.Orden, explicacion.Titulo, explicacion.Cuerpo);
    }

    /// <summary>
    /// Proyecta una corrida.
    /// </summary>
    /// <param name="corrida">Entidad de origen.</param>
    /// <param name="periodoClave">Clave canónica del período.</param>
    /// <param name="periodoDescripcion">Descripción del período.</param>
    /// <param name="calculadaPor">Nombre del usuario que calculó.</param>
    /// <returns>El DTO equivalente.</returns>
    public static CorridaDeNominaDto ADto(
        CorridaDeNomina corrida, string periodoClave, string periodoDescripcion, string calculadaPor)
    {
        ArgumentNullException.ThrowIfNull(corrida);
        TotalesDeCorrida t = corrida.Totales;

        return new CorridaDeNominaDto(
            corrida.Id, corrida.EmpresaId, corrida.PeriodoId, periodoClave, periodoDescripcion, corrida.Numero,
            corrida.Estado, corrida.FechaDeReferencia, corrida.FechaCalculo, calculadaPor, t.Trabajadores,
            t.Bruto, t.Percepciones, t.Deducciones, t.Neto, t.Isr, t.ImssTrabajador, t.ImssPatronal, t.Infonavit,
            t.Isn, t.ComplementoSindical, t.Facturable, t.Comision, t.CostoTotal, corrida.DuracionMs,
            corrida.Observaciones, corrida.Advertencias);
    }

    /// <summary>
    /// Proyecta el resumen de un resultado.
    /// </summary>
    /// <param name="resultado">Entidad de origen.</param>
    /// <param name="razonSocialNombre">Nombre de la razón social.</param>
    /// <returns>El DTO equivalente.</returns>
    public static ResultadoDeNominaDto ADto(ResultadoDeNomina resultado, string razonSocialNombre)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        ResumenDeResultado r = resultado.Resumen;

        return new ResultadoDeNominaDto(
            resultado.Id, resultado.CorridaId, resultado.ContratoId, resultado.EmpleadoId, resultado.RazonSocialId,
            razonSocialNombre, resultado.Esquema, resultado.ClaveEmpleado, resultado.NombreEmpleado,
            resultado.TipoDeMovimiento, r.Bruto, r.TotalPercepciones, r.TotalDeducciones, r.Neto, r.Isr, r.Subsidio,
            r.ImssTrabajador, r.ImssPatronal, r.InfonavitPatronal, r.InfonavitTrabajador, r.Fonacot, r.Isn,
            r.ComplementoSindical, r.Facturable, r.Comision, r.CostoTotal, r.CostoIsr, r.CostoImss, r.CostoInfonavit,
            r.CostoOtros, resultado.Advertencia);
    }

    /// <summary>
    /// Proyecta un cotejo.
    /// </summary>
    /// <param name="cotejo">Entidad de origen.</param>
    /// <param name="usuario">Nombre del usuario que cotejó.</param>
    /// <param name="incluirDetalle">Si se incluyen las diferencias.</param>
    /// <returns>El DTO equivalente.</returns>
    public static CotejoDto ADto(CotejoDeNomina cotejo, string usuario, bool incluirDetalle)
    {
        ArgumentNullException.ThrowIfNull(cotejo);

        IReadOnlyList<DiferenciaDeCotejoDto> diferencias = incluirDetalle
            ? cotejo.Diferencias.Select(static d => new DiferenciaDeCotejoDto(
                d.ClaveEmpleado, d.ConceptoClave, d.ImporteSistema, d.ImporteManual, d.Diferencia, d.DentroDeTolerancia)).ToList()
            : [];

        return new CotejoDto(
            cotejo.Id, cotejo.CorridaId, cotejo.FechaCotejo, usuario, cotejo.NombreArchivo, cotejo.ToleranciaAbsoluta,
            cotejo.TotalComparaciones, cotejo.TotalFueraDeTolerancia, diferencias);
    }

    /// <summary>
    /// Proyecta un usuario para la administración.
    /// </summary>
    /// <param name="usuario">Entidad de origen.</param>
    /// <param name="empresaRazonSocial">Razón social de su empresa, si aplica.</param>
    /// <param name="accionesEfectivas">Acciones habilitadas.</param>
    /// <returns>El DTO equivalente.</returns>
    public static UsuarioDto ADto(Usuario usuario, string? empresaRazonSocial, IReadOnlySet<AccionDelSistema> accionesEfectivas)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        ArgumentNullException.ThrowIfNull(accionesEfectivas);

        return new UsuarioDto(
            usuario.Id, usuario.NombreCompleto, usuario.Correo, usuario.Rol, usuario.EmpresaId, empresaRazonSocial,
            usuario.Activo, usuario.Idioma, usuario.TieneContrasenaLocal, usuario.RequiereCambioDeContrasena,
            usuario.FechaAlta,
            usuario.Permisos.Select(static p => new PermisoDto(p.Accion, p.Habilitado)).ToList(),
            accionesEfectivas.OrderBy(static a => (int)a).ToList(),
            usuario.EmpresasAdicionales.ToList());
    }

    /// <summary>
    /// Proyecta la identidad efectiva de un usuario.
    /// </summary>
    /// <param name="usuario">Entidad de origen.</param>
    /// <param name="empresas">Empresas del usuario, la principal primero; vacía para los roles transversales.</param>
    /// <param name="accionesEfectivas">Acciones habilitadas.</param>
    /// <returns>El DTO equivalente.</returns>
    public static UsuarioActualDto AUsuarioActual(
        Usuario usuario, IReadOnlyList<EmpresaDto> empresas, IReadOnlySet<AccionDelSistema> accionesEfectivas)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        ArgumentNullException.ThrowIfNull(empresas);
        ArgumentNullException.ThrowIfNull(accionesEfectivas);

        return new UsuarioActualDto(
            usuario.Id, usuario.NombreCompleto, usuario.Correo, usuario.Rol, usuario.EmpresaId, RazonSocialDe(usuario.EmpresaId, empresas),
            usuario.Idioma, accionesEfectivas.OrderBy(static a => (int)a).ToList(), usuario.RequiereCambioDeContrasena, empresas);
    }

    /// <summary>
    /// Proyecta la identidad de la petición en curso.
    /// </summary>
    /// <param name="usuario">Identidad efectiva.</param>
    /// <param name="empresas">Empresas del usuario, la principal primero; vacía para los roles transversales.</param>
    /// <param name="accionesEfectivas">Acciones habilitadas.</param>
    /// <returns>El DTO equivalente.</returns>
    public static UsuarioActualDto AUsuarioActual(
        Interfaces.IUsuarioActual usuario, IReadOnlyList<EmpresaDto> empresas, IReadOnlySet<AccionDelSistema> accionesEfectivas)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        ArgumentNullException.ThrowIfNull(empresas);
        ArgumentNullException.ThrowIfNull(accionesEfectivas);

        return new UsuarioActualDto(
            usuario.UsuarioId, usuario.NombreCompleto, usuario.Correo, usuario.Rol, usuario.EmpresaId, RazonSocialDe(usuario.EmpresaId, empresas),
            usuario.Idioma, accionesEfectivas.OrderBy(static a => (int)a).ToList(), usuario.RequiereCambioDeContrasena, empresas);
    }

    /// <summary>Busca la razón social de la empresa principal del usuario.</summary>
    /// <param name="empresaId">Empresa principal.</param>
    /// <param name="empresas">Empresas visibles para el usuario.</param>
    /// <returns>La razón social, o <c>null</c> si no tiene empresa o no está en la lista.</returns>
    private static string? RazonSocialDe(Guid? empresaId, IReadOnlyList<EmpresaDto> empresas)
        => empresaId is { } id ? empresas.FirstOrDefault(e => e.Id == id)?.RazonSocial : null;
}
