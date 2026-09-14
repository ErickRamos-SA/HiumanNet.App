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
/// Toda lectura filtra por empresa en la cláusula <c>WHERE</c>. La consulta de
/// contratos vigentes —la entrada del cálculo— se resuelve en una sola
/// sentencia apoyada en el índice <c>IX_Contratos_Empresa_Vigencia</c>.
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
        string sql = $"SELECT {LectorDeEmpleados.Columnas} FROM dbo.Empleados AS e WHERE e.Id = @Id AND e.EmpresaId = @EmpresaId;";

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeEmpleados.Mapear(reader) : null;
    }

    /// <inheritdoc/>
    public async Task<Empleado?> ObtenerPorClaveAsync(Guid empresaId, string clave, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clave);

        string sql = $"SELECT {LectorDeEmpleados.Columnas} FROM dbo.Empleados AS e WHERE e.EmpresaId = @EmpresaId AND e.Clave = @Clave;";

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        comando.Parameters.Add(new SqlParameter("@Clave", SqlDbType.NVarChar, 20) { Value = clave.Trim() });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeEmpleados.Mapear(reader) : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Empleado>> ListarPorEmpresaAsync(
        Guid empresaId, bool soloActivos, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {LectorDeEmpleados.Columnas}
            FROM   dbo.Empleados AS e
            WHERE  e.EmpresaId = @EmpresaId AND (@SoloActivos = 0 OR e.Activo = 1)
            ORDER BY LEN(e.Clave), e.Clave;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
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

        const string sql = """
            INSERT INTO dbo.Empleados
                (Id, EmpresaId, Clave, Nombre, ApellidoPaterno, ApellidoMaterno, Rfc, Curp, Nss, FechaNacimiento,
                 Correo, Telefono, Activo, FechaAlta)
            VALUES
                (@Id, @EmpresaId, @Clave, @Nombre, @ApellidoPaterno, @ApellidoMaterno, @Rfc, @Curp, @Nss, @FechaNacimiento,
                 @Correo, @Telefono, @Activo, @FechaAlta);
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametros(comando, empleado);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empleado.EmpresaId });
        comando.Parameters.Add(new SqlParameter("@FechaAlta", SqlDbType.DateTimeOffset) { Value = empleado.FechaAlta });
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarAsync(Empleado empleado, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(empleado);

        const string sql = """
            UPDATE dbo.Empleados
            SET    Clave = @Clave, Nombre = @Nombre, ApellidoPaterno = @ApellidoPaterno, ApellidoMaterno = @ApellidoMaterno,
                   Rfc = @Rfc, Curp = @Curp, Nss = @Nss, FechaNacimiento = @FechaNacimiento, Correo = @Correo,
                   Telefono = @Telefono, Activo = @Activo
            WHERE  Id = @Id;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametros(comando, empleado);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Contrato?> ObtenerContratoAsync(Guid contratoId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        string sql = $"SELECT {LectorDeContratos.Columnas} FROM dbo.Contratos AS c WHERE c.Id = @Id AND c.EmpresaId = @EmpresaId;";

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = contratoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });

        await using SqlDataReader reader = await comando.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? LectorDeContratos.Mapear(reader) : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Contrato>> ListarContratosDeEmpleadoAsync(
        Guid empleadoId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT {LectorDeContratos.Columnas}
            FROM   dbo.Contratos AS c
            WHERE  c.EmpleadoId = @EmpleadoId AND c.EmpresaId = @EmpresaId
            ORDER BY c.FechaAlta DESC;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
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
        string sql = $"""
            SELECT {LectorDeContratos.Columnas}
            FROM   dbo.Contratos AS c
            INNER JOIN dbo.Empleados AS e ON e.Id = c.EmpleadoId AND e.Activo = 1
            WHERE  c.EmpresaId = @EmpresaId
              AND  c.FechaAlta <= @Fecha
              AND  (c.FechaBaja IS NULL OR c.FechaBaja >= @InicioDeMes)
            ORDER BY LEN(e.Clave), e.Clave, c.Esquema;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = empresaId });
        comando.Parameters.Add(new SqlParameter("@Fecha", SqlDbType.Date) { Value = fecha });
        comando.Parameters.Add(new SqlParameter("@InicioDeMes", SqlDbType.Date) { Value = new DateOnly(fecha.Year, fecha.Month, 1) });

        return await LeerContratosAsync(comando, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AgregarContratoAsync(Contrato contrato, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contrato);

        const string sql = """
            INSERT INTO dbo.Contratos
                (Id, EmpleadoId, EmpresaId, RazonSocialId, Esquema, NumeroTrabajador, Puesto, Departamento, TipoContrato,
                 SueldoPeriodoReal, SalarioDiarioFiscal, SalarioDiarioIntegrado, Zona, InfonavitTipo, InfonavitValor,
                 InfonavitSeguroVivienda, FonacotMensual, PensionAlimenticiaImporte, PensionAlimenticiaPorcentaje,
                 PrestamoPersonalFijo, BonoFijo, HonorariosAplicaIva, PagaComplementoSindical, FechaAlta, FechaBaja,
                 FechaModificacion)
            VALUES
                (@Id, @EmpleadoId, @EmpresaId, @RazonSocialId, @Esquema, @NumeroTrabajador, @Puesto, @Departamento, @TipoContrato,
                 @SueldoPeriodoReal, @SalarioDiarioFiscal, @SalarioDiarioIntegrado, @Zona, @InfonavitTipo, @InfonavitValor,
                 @InfonavitSeguroVivienda, @FonacotMensual, @PensionImporte, @PensionPorcentaje,
                 @PrestamoPersonalFijo, @BonoFijo, @HonorariosAplicaIva, @PagaComplemento, @FechaAlta, @FechaBaja,
                 @FechaModificacion);
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametros(comando, contrato);
        comando.Parameters.Add(new SqlParameter("@EmpleadoId", SqlDbType.UniqueIdentifier) { Value = contrato.EmpleadoId });
        comando.Parameters.Add(new SqlParameter("@EmpresaId", SqlDbType.UniqueIdentifier) { Value = contrato.EmpresaId });
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ActualizarContratoAsync(Contrato contrato, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contrato);

        const string sql = """
            UPDATE dbo.Contratos
            SET    RazonSocialId = @RazonSocialId, Esquema = @Esquema, NumeroTrabajador = @NumeroTrabajador,
                   Puesto = @Puesto, Departamento = @Departamento, TipoContrato = @TipoContrato,
                   SueldoPeriodoReal = @SueldoPeriodoReal, SalarioDiarioFiscal = @SalarioDiarioFiscal,
                   SalarioDiarioIntegrado = @SalarioDiarioIntegrado, Zona = @Zona, InfonavitTipo = @InfonavitTipo,
                   InfonavitValor = @InfonavitValor, InfonavitSeguroVivienda = @InfonavitSeguroVivienda,
                   FonacotMensual = @FonacotMensual, PensionAlimenticiaImporte = @PensionImporte,
                   PensionAlimenticiaPorcentaje = @PensionPorcentaje, PrestamoPersonalFijo = @PrestamoPersonalFijo,
                   BonoFijo = @BonoFijo, HonorariosAplicaIva = @HonorariosAplicaIva,
                   PagaComplementoSindical = @PagaComplemento, FechaAlta = @FechaAlta, FechaBaja = @FechaBaja,
                   FechaModificacion = @FechaModificacion
            WHERE  Id = @Id;
            """;

        await using SqlCommand comando = await CrearComandoAsync(sql, cancellationToken);
        AgregarParametros(comando, contrato);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

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

    private static SqlParameter Texto(string nombre, int tamano, string? valor)
        => new(nombre, SqlDbType.NVarChar, tamano) { Value = (object?)valor ?? DBNull.Value };

    private static SqlParameter Importe(string nombre, decimal valor)
        => new(nombre, SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = valor };

    private static SqlParameter Tasa(string nombre, decimal valor)
        => new(nombre, SqlDbType.Decimal) { Precision = 19, Scale = 8, Value = valor };

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
