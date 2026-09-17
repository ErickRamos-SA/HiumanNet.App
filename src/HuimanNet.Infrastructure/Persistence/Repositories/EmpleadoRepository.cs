using System.Data;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Repositories;
using HuimanNet.Infrastructure.Persistence.Connections;
using HuimanNet.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace HuimanNet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de empleados y contratos basado en ADO.NET sobre SQL Server.
/// </summary>
/// <remarks>
/// Toda lectura filtra por empresa. La consulta de contratos vigentes —la
/// entrada del cálculo— la resuelve <c>Contratos_ListarVigentes</c> en una sola
/// sentencia apoyada en <c>IX_Contratos_Empresa_Vigencia</c>.
/// </remarks>
public sealed class EmpleadoRepository : RepositorioSqlBase, IEmpleadoRepository
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="EmpleadoRepository"/>.
    /// </summary>
    /// <param name="sesion">Sesión de base de datos de la petición en curso.</param>
    public EmpleadoRepository(ISesionSql sesion)
        : base(sesion)
    {
    }

    /// <inheritdoc/>
    public async Task<Empleado?> ObtenerPorIdAsync(Guid id, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Empleados.Obtener, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeEmpleados.Mapear(reader) : null;
    }

    /// <inheritdoc/>
    public async Task<Empleado?> ObtenerPorClaveAsync(Guid empresaId, string clave, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clave);

        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Empleados.ObtenerPorClave, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        comando.Parameters.Add(new SqlParameter("@Clave", SqlDbType.NVarChar, 20) { Value = clave.Trim() });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeEmpleados.Mapear(reader) : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Empleado>> ListarPorEmpresaAsync(
        Guid empresaId, bool soloActivos, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Empleados.ListarPorEmpresa, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        comando.Parameters.Add(new SqlParameter("@SoloActivos", SqlDbType.Bit) { Value = soloActivos });

        var lista = new List<Empleado>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            lista.Add(LectorDeEmpleados.Mapear(reader));
        }

        return lista;
    }

    /// <inheritdoc/>
    public async Task AgregarAsync(Empleado empleado, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(empleado);

        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Empleados.Insertar, cancellationToken);
        AgregarParametros(comando, empleado);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empleado.EmpresaId });
        comando.Parameters.Add(new SqlParameter("@FechaAlta", SqlDbType.DateTimeOffset) { Value = empleado.FechaAlta });
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarAsync(Empleado empleado, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(empleado);

        await using SqlCommand comando = await CrearProcedimientoAsync(Procedimientos.Empleados.Actualizar, cancellationToken);
        AgregarParametros(comando, empleado);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Contrato?> ObtenerContratoAsync(Guid contratoId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Empleados.ObtenerContrato, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = contratoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeContratos.Mapear(reader) : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Contrato>> ListarContratosDeEmpleadoAsync(
        Guid empleadoId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Empleados.ListarContratosDeEmpleado, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpleadoId", SqlDbType.UniqueIdentifier) { Value = empleadoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        return await LeerContratosAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Un contrato cuenta como vigente si se dio de alta antes o en la fecha y
    /// no se dio de baja antes del <b>primer día del mes</b> de esa fecha: así
    /// el trabajador que causa baja a mitad de mes entra en la corrida en la
    /// que se le paga el finiquito.
    /// </remarks>
    public async Task<IReadOnlyList<Contrato>> ListarContratosVigentesAsync(
        Guid empresaId, DateOnly fecha, CancellationToken cancellationToken = default)
    {
        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Empleados.ListarContratosVigentes, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        comando.Parameters.Add(new SqlParameter("@Fecha", SqlDbType.Date) { Value = fecha });
        comando.Parameters.Add(new SqlParameter("@InicioDeMes", SqlDbType.Date) { Value = new DateOnly(fecha.Year, fecha.Month, 1) });

        return await LeerContratosAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AgregarContratoAsync(Contrato contrato, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contrato);

        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Empleados.InsertarContrato, cancellationToken);
        AgregarParametros(comando, contrato);
        comando.Parameters.Add(new SqlParameter("@EmpleadoId", SqlDbType.UniqueIdentifier) { Value = contrato.EmpleadoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = contrato.EmpresaId });
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarContratoAsync(Contrato contrato, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contrato);

        await using SqlCommand comando = await CrearProcedimientoAsync(
            Procedimientos.Empleados.ActualizarContrato, cancellationToken);
        AgregarParametros(comando, contrato);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Agrega los valores de un empleado al comando.</summary>
    /// <param name="comando">Comando de inserción o actualización.</param>
    /// <param name="empleado">Empleado.</param>
    private static void AgregarParametros(SqlCommand comando, Empleado empleado)
    {
        DatosPersonales d = empleado.Datos;

        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = empleado.Id });
        comando.Parameters.Add(new SqlParameter("@Clave", SqlDbType.NVarChar, 20) { Value = empleado.Clave });
        comando.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 100) { Value = d.Nombre });
        comando.Parameters.Add(new SqlParameter("@ApellidoPaterno", SqlDbType.NVarChar, 100) { Value = d.ApellidoPaterno });
        comando.Parameters.Add(Texto("@ApellidoMaterno", 100, d.ApellidoMaterno));
        comando.Parameters.Add(Texto("@Rfc", 13, d.Rfc));
        comando.Parameters.Add(Texto("@Curp", 18, d.Curp));
        comando.Parameters.Add(Texto("@Nss", 11, d.Nss));
        comando.Parameters.Add(new SqlParameter("@FechaNacimiento", SqlDbType.Date) { Value = (object?)d.FechaNacimiento ?? DBNull.Value });
        comando.Parameters.Add(Texto("@Correo", 256, d.Correo));
        comando.Parameters.Add(Texto("@Telefono", 30, d.Telefono));
        comando.Parameters.Add(new SqlParameter("@Activo", SqlDbType.Bit) { Value = empleado.Activo });
    }

    /// <summary>Agrega los valores de un contrato al comando.</summary>
    /// <param name="comando">Comando de inserción o actualización.</param>
    /// <param name="contrato">Contrato.</param>
    private static void AgregarParametros(SqlCommand comando, Contrato contrato)
    {
        CondicionesDeContrato c = contrato.Condiciones;

        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = contrato.Id });
        comando.Parameters.Add(new SqlParameter("@RazonSocialId", SqlDbType.UniqueIdentifier) { Value = contrato.RazonSocialId });
        comando.Parameters.Add(new SqlParameter("@Esquema", SqlDbType.TinyInt) { Value = (byte)contrato.Esquema });
        comando.Parameters.Add(Texto("@NumeroTrabajador", 30, contrato.NumeroTrabajador));
        comando.Parameters.Add(Texto("@Puesto", 100, contrato.Puesto));
        comando.Parameters.Add(Texto("@Departamento", 100, contrato.Departamento));
        comando.Parameters.Add(Texto("@TipoContrato", 50, contrato.TipoDeContrato));
        comando.Parameters.Add(Importe("@SueldoPeriodoReal", c.SueldoPeriodoReal));
        comando.Parameters.Add(Importe("@SalarioDiarioFiscal", c.SalarioDiarioFiscal));
        comando.Parameters.Add(Importe("@SalarioDiarioIntegrado", c.SalarioDiarioIntegrado));
        comando.Parameters.Add(new SqlParameter("@Zona", SqlDbType.TinyInt) { Value = (byte)c.Zona });
        comando.Parameters.Add(new SqlParameter("@InfonavitTipo", SqlDbType.TinyInt) { Value = (byte)c.Infonavit.Tipo });
        comando.Parameters.Add(Tasa("@InfonavitValor", c.Infonavit.Valor));
        comando.Parameters.Add(Importe("@InfonavitSeguroVivienda", c.Infonavit.SeguroDeVivienda));
        comando.Parameters.Add(Importe("@FonacotMensual", c.FonacotMensual));
        comando.Parameters.Add(Importe("@PensionImporte", c.PensionAlimenticiaImporte));
        comando.Parameters.Add(Tasa("@PensionPorcentaje", c.PensionAlimenticiaPorcentaje));
        comando.Parameters.Add(Importe("@PrestamoPersonalFijo", c.PrestamoPersonalFijo));
        comando.Parameters.Add(Importe("@BonoFijo", c.BonoFijo));
        comando.Parameters.Add(new SqlParameter("@HonorariosAplicaIva", SqlDbType.Bit) { Value = c.HonorariosAplicaIva });
        comando.Parameters.Add(new SqlParameter("@PagaComplemento", SqlDbType.Bit) { Value = c.PagaComplementoSindical });
        comando.Parameters.Add(new SqlParameter("@FechaAlta", SqlDbType.Date) { Value = contrato.FechaAlta });
        comando.Parameters.Add(new SqlParameter("@FechaBaja", SqlDbType.Date) { Value = (object?)contrato.FechaBaja ?? DBNull.Value });
        comando.Parameters.Add(new SqlParameter("@FechaModificacion", SqlDbType.DateTimeOffset) { Value = contrato.FechaModificacion });
    }

    /// <summary>Crea un parámetro de texto opcional.</summary>
    /// <param name="nombre">Nombre del parámetro.</param>
    /// <param name="tamano">Longitud de la columna.</param>
    /// <param name="valor">Texto, o <c>null</c>.</param>
    /// <returns>El parámetro.</returns>
    private static SqlParameter Texto(string nombre, int tamano, string? valor)
        => new(nombre, SqlDbType.NVarChar, tamano) { Value = (object?)valor ?? DBNull.Value };

    /// <summary>Crea un parámetro de importe con cuatro decimales.</summary>
    /// <param name="nombre">Nombre del parámetro.</param>
    /// <param name="valor">Importe.</param>
    /// <returns>El parámetro.</returns>
    private static SqlParameter Importe(string nombre, decimal valor)
        => new(nombre, SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = valor };

    /// <summary>Crea un parámetro de tasa con ocho decimales.</summary>
    /// <param name="nombre">Nombre del parámetro.</param>
    /// <param name="valor">Tasa o factor.</param>
    /// <returns>El parámetro.</returns>
    private static SqlParameter Tasa(string nombre, decimal valor)
        => new(nombre, SqlDbType.Decimal) { Precision = 19, Scale = 8, Value = valor };

    /// <summary>Ejecuta un procedimiento de contratos y rehidrata cada fila.</summary>
    /// <param name="comando">Comando ya preparado.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Los contratos.</returns>
    private static async Task<IReadOnlyList<Contrato>> LeerContratosAsync(SqlCommand comando, CancellationToken cancellationToken)
    {
        var lista = new List<Contrato>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            lista.Add(LectorDeContratos.Mapear(reader));
        }

        return lista;
    }
}
