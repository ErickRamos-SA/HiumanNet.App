using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Nomina;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Mappers;

/// <summary>
/// Traduce filas de <c>dbo.RazonesSociales</c> a entidades <see cref="RazonSocial"/>.
/// </summary>
/// <remarks>Mapeo manual por ordinal, sin reflexión.</remarks>
public static class LectorDeRazonesSociales
{
    /// <summary>Columnas que debe proyectar cualquier consulta que alimente a <see cref="Mapear"/>.</summary>
    public const string Columnas =
        "r.Id, r.EmpresaId, r.Nombre, r.Rfc, r.RegistroPatronal, r.Zona, r.TipoServicio, r.SubsidioAbsorbido, " +
        "r.AplicaFaltasProporcionales, r.ModalidadComision, r.PorcentajeComision, r.ZonaIsn, r.TasaIva, " +
        "r.PorcentajeOtrosCostos, r.PrimaRiesgo, r.BancoDispersor, r.Activa, r.FechaAlta";

    /// <summary>
    /// Construye una entidad a partir de la fila actual del lector.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>La razón social rehidratada.</returns>
    public static RazonSocial Mapear(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var configuracion = new ConfiguracionDeRazonSocial(
            (TipoDeServicio)reader.GetByte(6),
            reader.GetBoolean(7),
            reader.GetBoolean(8),
            (ModalidadDeComision)reader.GetByte(9),
            reader.GetDecimal(10),
            (ZonaIsn)reader.GetByte(11),
            reader.GetDecimal(12),
            reader.GetDecimal(13),
            reader.IsDBNull(14) ? null : reader.GetDecimal(14));

        return RazonSocial.Rehidratar(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            (ZonaSalarioMinimo)reader.GetByte(5),
            configuracion,
            reader.IsDBNull(15) ? null : reader.GetString(15),
            reader.GetBoolean(16),
            reader.GetDateTimeOffset(17));
    }
}

/// <summary>
/// Traduce filas de <c>dbo.Empleados</c> a entidades <see cref="Empleado"/>.
/// </summary>
public static class LectorDeEmpleados
{
    /// <summary>Columnas que debe proyectar cualquier consulta que alimente a <see cref="Mapear"/>.</summary>
    public const string Columnas =
        "e.Id, e.EmpresaId, e.Clave, e.Nombre, e.ApellidoPaterno, e.ApellidoMaterno, e.Rfc, e.Curp, e.Nss, " +
        "e.FechaNacimiento, e.Correo, e.Telefono, e.Activo, e.FechaAlta";

    /// <summary>
    /// Construye una entidad a partir de la fila actual del lector.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>El empleado rehidratado.</returns>
    public static Empleado Mapear(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var datos = new DatosPersonales(
            reader.GetString(3),
            reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetFieldValue<DateOnly>(9),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.IsDBNull(11) ? null : reader.GetString(11));

        return Empleado.Rehidratar(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), datos, reader.GetBoolean(12), reader.GetDateTimeOffset(13));
    }
}

/// <summary>
/// Traduce filas de <c>dbo.Contratos</c> a entidades <see cref="Contrato"/>.
/// </summary>
public static class LectorDeContratos
{
    /// <summary>Columnas que debe proyectar cualquier consulta que alimente a <see cref="Mapear"/>.</summary>
    public const string Columnas =
        "c.Id, c.EmpleadoId, c.EmpresaId, c.RazonSocialId, c.Esquema, c.NumeroTrabajador, c.Puesto, c.Departamento, " +
        "c.TipoContrato, c.SueldoPeriodoReal, c.SalarioDiarioFiscal, c.SalarioDiarioIntegrado, c.Zona, c.InfonavitTipo, " +
        "c.InfonavitValor, c.InfonavitSeguroVivienda, c.FonacotMensual, c.PensionAlimenticiaImporte, " +
        "c.PensionAlimenticiaPorcentaje, c.PrestamoPersonalFijo, c.BonoFijo, c.HonorariosAplicaIva, c.FechaAlta, " +
        "c.FechaBaja, c.FechaModificacion, c.PagaComplementoSindical";

    /// <summary>
    /// Construye una entidad a partir de la fila actual del lector.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>El contrato rehidratado.</returns>
    public static Contrato Mapear(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var condiciones = new CondicionesDeContrato(
            reader.GetDecimal(9),
            reader.GetDecimal(10),
            reader.GetDecimal(11),
            (ZonaSalarioMinimo)reader.GetByte(12),
            new CreditoInfonavit((TipoDeCreditoInfonavit)reader.GetByte(13), reader.GetDecimal(14), reader.GetDecimal(15)),
            reader.GetDecimal(16),
            reader.GetDecimal(17),
            reader.GetDecimal(18),
            reader.GetDecimal(19),
            reader.GetDecimal(20),
            reader.GetBoolean(21),
            reader.GetBoolean(25));

        return Contrato.Rehidratar(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.GetGuid(3),
            (EsquemaDePago)reader.GetByte(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            condiciones,
            reader.GetFieldValue<DateOnly>(22),
            reader.IsDBNull(23) ? null : reader.GetFieldValue<DateOnly>(23),
            reader.GetDateTimeOffset(24));
    }
}

/// <summary>
/// Traduce filas de <c>dbo.Incidencias</c> a entidades <see cref="Incidencia"/>.
/// </summary>
public static class LectorDeIncidencias
{
    /// <summary>Columnas que debe proyectar cualquier consulta que alimente a <see cref="Mapear"/>.</summary>
    public const string Columnas =
        "i.Id, i.EmpresaId, i.PeriodoId, i.ContratoId, i.DiasPeriodo, i.Vacaciones, i.Ausentismos, i.Incapacidades, " +
        "i.Festivos, i.HorasDobles, i.HorasTriples, i.DomingosTrabajados, i.Gratificacion, i.Reembolsos, i.Teletrabajo, " +
        "i.Finiquito, i.Cafeteria, i.HorasDescontadas, i.OtrosDescuentos, i.PrestamoPersonal, i.Aguinaldo, " +
        "i.DescuentosFiscales, i.FonacotCapturado, i.DescuentoSindicalAdicional, i.AjusteSindical, i.IsrManual, " +
        "i.TipoMovimiento, i.Observaciones, i.CapturadoPorUsuarioId, i.FechaCaptura";

    /// <summary>
    /// Construye una entidad a partir de la fila actual del lector.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>La incidencia rehidratada.</returns>
    public static Incidencia Mapear(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return Incidencia.Rehidratar(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3), MapearDatos(reader, 4),
            reader.GetGuid(28), reader.GetDateTimeOffset(29));
    }

    /// <summary>
    /// Lee las cantidades e importes de una incidencia a partir de un ordinal inicial.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <param name="inicio">Ordinal de <c>DiasPeriodo</c>.</param>
    /// <returns>Los datos de la incidencia.</returns>
    public static DatosDeIncidencia MapearDatos(SqlDataReader reader, int inicio)
    {
        ArgumentNullException.ThrowIfNull(reader);
        int o = inicio;

        return new DatosDeIncidencia(
            reader.GetDecimal(o), reader.GetDecimal(o + 1), reader.GetDecimal(o + 2), reader.GetDecimal(o + 3),
            reader.GetDecimal(o + 4), reader.GetDecimal(o + 5), reader.GetDecimal(o + 6), reader.GetDecimal(o + 7),
            reader.GetDecimal(o + 8), reader.GetDecimal(o + 9), reader.GetDecimal(o + 10), reader.GetDecimal(o + 11),
            reader.GetDecimal(o + 12), reader.GetDecimal(o + 13), reader.GetDecimal(o + 14), reader.GetDecimal(o + 15),
            reader.GetDecimal(o + 16), reader.GetDecimal(o + 17), reader.GetDecimal(o + 18), reader.GetDecimal(o + 19),
            reader.GetDecimal(o + 20),
            reader.IsDBNull(o + 21) ? null : reader.GetDecimal(o + 21),
            (TipoDeMovimiento)reader.GetByte(o + 22),
            reader.IsDBNull(o + 23) ? null : reader.GetString(o + 23));
    }
}

/// <summary>
/// Traduce filas de los catálogos de cálculo a sus entidades.
/// </summary>
public static class LectorDeCatalogos
{
    /// <summary>Columnas de <c>dbo.ParametrosDeCalculo</c> (alias <c>p</c>).</summary>
    public const string ColumnasDeParametro =
        "p.Id, p.Clave, p.Descripcion, p.Grupo, p.Valor, p.Unidad, p.EmpresaId, p.VigenteDesde, p.VigenteHasta, p.FechaModificacion";

    /// <summary>Columnas de <c>dbo.TablasDeRangos</c> (alias <c>t</c>).</summary>
    public const string ColumnasDeTabla =
        "t.Id, t.Clave, t.Descripcion, t.EmpresaId, t.VigenteDesde, t.VigenteHasta, t.FechaModificacion";

    /// <summary>Columnas de <c>dbo.RangosDeTabla</c> (alias <c>r</c>).</summary>
    public const string ColumnasDeRango =
        "r.TablaId, r.Orden, r.LimiteInferior, r.LimiteSuperior, r.CuotaFija, r.Porcentaje, r.Valor";

    /// <summary>Columnas de <c>dbo.ConceptosDeNomina</c> (alias <c>c</c>).</summary>
    public const string ColumnasDeConcepto =
        "c.Id, c.Clave, c.Nombre, c.Descripcion, c.Tipo, c.Esquemas, c.Orden, c.Formula, c.VisibleEnRecibo, c.Activo, c.EmpresaId, c.FechaModificacion";

    /// <summary>Columnas de <c>dbo.ExplicacionesDeCalculo</c> (alias <c>x</c>).</summary>
    public const string ColumnasDeExplicacion =
        "x.Id, x.Esquema, x.Idioma, x.Orden, x.Titulo, x.Cuerpo, x.FechaModificacion";

    /// <summary>
    /// Construye un parámetro a partir de la fila actual.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>El parámetro rehidratado.</returns>
    public static ParametroDeCalculo MapearParametro(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return ParametroDeCalculo.Rehidratar(
            reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetDecimal(4),
            reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetGuid(6), reader.GetFieldValue<DateOnly>(7),
            reader.IsDBNull(8) ? null : reader.GetFieldValue<DateOnly>(8), reader.GetDateTimeOffset(9));
    }

    /// <summary>
    /// Construye un rango a partir de la fila actual.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>El identificador de la tabla y el rango.</returns>
    public static (Guid TablaId, RangoDeTabla Rango) MapearRango(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return (reader.GetGuid(0), new RangoDeTabla(
            reader.GetDecimal(2), reader.IsDBNull(3) ? null : reader.GetDecimal(3), reader.GetDecimal(4),
            reader.GetDecimal(5), reader.GetDecimal(6)));
    }

    /// <summary>
    /// Construye una tabla a partir de la fila actual y de sus rangos.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <param name="rangos">Rangos de la tabla.</param>
    /// <returns>La tabla rehidratada.</returns>
    public static TablaDeRangos MapearTabla(SqlDataReader reader, IEnumerable<RangoDeTabla> rangos)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return TablaDeRangos.Rehidratar(
            reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetGuid(3),
            reader.GetFieldValue<DateOnly>(4), reader.IsDBNull(5) ? null : reader.GetFieldValue<DateOnly>(5),
            reader.GetDateTimeOffset(6), rangos);
    }

    /// <summary>
    /// Construye un concepto a partir de la fila actual.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>El concepto rehidratado.</returns>
    public static ConceptoDeNomina MapearConcepto(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return ConceptoDeNomina.Rehidratar(
            reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            (TipoDeConcepto)reader.GetByte(4), (EsquemasDePago)reader.GetByte(5), reader.GetInt32(6), reader.GetString(7),
            reader.GetBoolean(8), reader.GetBoolean(9), reader.IsDBNull(10) ? null : reader.GetGuid(10), reader.GetDateTimeOffset(11));
    }

    /// <summary>
    /// Construye una sección de explicación a partir de la fila actual.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>La sección rehidratada.</returns>
    public static ExplicacionDeCalculo MapearExplicacion(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return ExplicacionDeCalculo.Rehidratar(
            reader.GetGuid(0), (EsquemaDePago)reader.GetByte(1), (Idioma)reader.GetByte(2), reader.GetInt32(3),
            reader.GetString(4), reader.GetString(5), reader.GetDateTimeOffset(6));
    }
}

/// <summary>
/// Traduce filas de corridas, resultados y cotejos a sus entidades.
/// </summary>
public static class LectorDeCorridas
{
    /// <summary>Columnas de <c>dbo.CorridasDeNomina</c> (alias <c>k</c>).</summary>
    public const string ColumnasDeCorrida =
        "k.Id, k.EmpresaId, k.PeriodoId, k.Numero, k.Estado, k.FechaReferencia, k.FechaCalculo, k.CalculadaPorUsuarioId, " +
        "k.Trabajadores, k.TotalBruto, k.TotalPercepciones, k.TotalDeducciones, k.TotalNeto, k.TotalIsr, k.TotalImssTrabajador, " +
        "k.TotalImssPatronal, k.TotalInfonavit, k.TotalIsn, k.TotalComplemento, k.TotalFacturable, k.TotalComision, " +
        "k.TotalCosto, k.DuracionMs, k.Observaciones, k.Advertencias";

    /// <summary>Columnas de <c>dbo.ResultadosDeNomina</c> (alias <c>n</c>).</summary>
    public const string ColumnasDeResultado =
        "n.Id, n.CorridaId, n.EmpresaId, n.ContratoId, n.EmpleadoId, n.RazonSocialId, n.Esquema, n.ClaveEmpleado, " +
        "n.NombreEmpleado, n.TipoMovimiento, n.Bruto, n.TotalPercepciones, n.TotalDeducciones, n.Neto, n.Isr, n.Subsidio, " +
        "n.ImssTrabajador, n.ImssPatronal, n.InfonavitPatronal, n.InfonavitTrabajador, n.Fonacot, n.Isn, n.ComplementoSindical, " +
        "n.Facturable, n.Comision, n.CostoTotal, n.CostoIsr, n.CostoImss, n.CostoInfonavit, n.CostoOtros, n.Advertencia";

    /// <summary>Columnas de <c>dbo.CotejosDeNomina</c> (alias <c>j</c>).</summary>
    public const string ColumnasDeCotejo =
        "j.Id, j.CorridaId, j.EmpresaId, j.FechaCotejo, j.UsuarioId, j.NombreArchivo, j.ToleranciaAbsoluta, " +
        "j.TotalComparaciones, j.TotalFueraDeTolerancia";

    /// <summary>
    /// Construye una corrida a partir de la fila actual.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <returns>La corrida rehidratada.</returns>
    public static CorridaDeNomina MapearCorrida(SqlDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var totales = new TotalesDeCorrida(
            reader.GetInt32(8), reader.GetDecimal(9), reader.GetDecimal(10), reader.GetDecimal(11), reader.GetDecimal(12),
            reader.GetDecimal(13), reader.GetDecimal(14), reader.GetDecimal(15), reader.GetDecimal(16), reader.GetDecimal(17),
            reader.GetDecimal(18), reader.GetDecimal(19), reader.GetDecimal(20), reader.GetDecimal(21));

        return CorridaDeNomina.Rehidratar(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetInt32(3), (EstadoDeCorrida)reader.GetByte(4),
            reader.GetFieldValue<DateOnly>(5), reader.GetDateTimeOffset(6), reader.GetGuid(7), totales, reader.GetInt64(22),
            reader.IsDBNull(23) ? null : reader.GetString(23), reader.IsDBNull(24) ? null : reader.GetString(24));
    }

    /// <summary>
    /// Construye el resumen de un resultado a partir de la fila actual.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <param name="inicio">Ordinal de la columna <c>Bruto</c>.</param>
    /// <returns>El resumen.</returns>
    public static ResumenDeResultado MapearResumen(SqlDataReader reader, int inicio)
    {
        ArgumentNullException.ThrowIfNull(reader);
        int o = inicio;

        return new ResumenDeResultado(
            reader.GetDecimal(o), reader.GetDecimal(o + 1), reader.GetDecimal(o + 2), reader.GetDecimal(o + 3),
            reader.GetDecimal(o + 4), reader.GetDecimal(o + 5), reader.GetDecimal(o + 6), reader.GetDecimal(o + 7),
            reader.GetDecimal(o + 8), reader.GetDecimal(o + 9), reader.GetDecimal(o + 10), reader.GetDecimal(o + 11),
            reader.GetDecimal(o + 12), reader.GetDecimal(o + 13), reader.GetDecimal(o + 14), reader.GetDecimal(o + 15),
            reader.GetDecimal(o + 16), reader.GetDecimal(o + 17), reader.GetDecimal(o + 18), reader.GetDecimal(o + 19));
    }

    /// <summary>
    /// Construye un resultado a partir de la fila actual y de sus conceptos.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <param name="conceptos">Detalle de conceptos.</param>
    /// <returns>El resultado rehidratado.</returns>
    public static ResultadoDeNomina MapearResultado(SqlDataReader reader, IReadOnlyList<ValorDeConcepto> conceptos)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return ResultadoDeNomina.Rehidratar(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3), reader.GetGuid(4), reader.GetGuid(5),
            (EsquemaDePago)reader.GetByte(6), reader.GetString(7), reader.GetString(8), (TipoDeMovimiento)reader.GetByte(9),
            MapearResumen(reader, 10), conceptos, reader.IsDBNull(30) ? null : reader.GetString(30));
    }

    /// <summary>
    /// Construye un cotejo a partir de la fila actual y de sus diferencias.
    /// </summary>
    /// <param name="reader">Lector posicionado en una fila válida.</param>
    /// <param name="diferencias">Detalle de diferencias.</param>
    /// <returns>El cotejo rehidratado.</returns>
    public static CotejoDeNomina MapearCotejo(SqlDataReader reader, IReadOnlyList<DiferenciaDeCotejo> diferencias)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return CotejoDeNomina.Rehidratar(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetDateTimeOffset(3), reader.GetGuid(4),
            reader.GetString(5), reader.GetDecimal(6), reader.GetInt32(7), reader.GetInt32(8), diferencias);
    }
}
