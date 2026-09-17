using System.Reflection;
using FluentAssertions;
using HuimanNet.Infrastructure;
using HuimanNet.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static HuimanNet.Infrastructure.Tests.EntornoSqlDePrueba;

namespace HuimanNet.Infrastructure.Tests;

/// <summary>
/// Verifica que los procedimientos almacenados del código y los de la base de
/// datos son exactamente los mismos.
/// </summary>
/// <remarks>
/// Todo el acceso a datos pasa por <see cref="Procedimientos"/>: si alguien
/// añade una constante sin escribir el procedimiento, o deja un procedimiento
/// que ya nadie invoca, esta prueba lo señala. Usa una base de datos exclusiva,
/// <c>HuimanNetProcedimientos_Pruebas</c>, que se borra y se recrea en cada
/// ejecución.
/// </remarks>
public sealed class ProcedimientosAlmacenadosTests
{
    [Fact]
    public async Task Base_TieneExactamenteLosProcedimientosQueUsaElCodigo()
    {
        string cadena = Cadena("HuimanNetProcedimientos_Pruebas");
        string? motivo = await RecrearBaseDeDatosAsync(cadena);
        Assert.SkipWhen(motivo is not null, $"SQL Server de pruebas no disponible: {motivo}");

        await using ServiceProvider servicios = ConstruirServicios(cadena, "admin.procedimientos@huimannet.local");
        (await PreparacionDelEntorno.EjecutarAsync(servicios, Ct)).Should().BeTrue();

        IReadOnlyList<string> enLaBase = await LeerProcedimientosAsync(cadena);
        IReadOnlyList<string> enElCodigo = NombresDelCodigo();

        enElCodigo.Should().OnlyHaveUniqueItems();
        enLaBase.Should().BeEquivalentTo(
            enElCodigo,
            "cada procedimiento de la base debe usarse desde el código y cada constante debe existir en la base");
    }

    /// <summary>Lee los procedimientos que existen en la base de datos.</summary>
    /// <param name="cadena">Cadena de conexión de la base de pruebas.</param>
    /// <returns>Los nombres con su esquema, como <c>dbo.Empresas_Obtener</c>.</returns>
    private static async Task<IReadOnlyList<string>> LeerProcedimientosAsync(string cadena)
    {
        await using var conexion = new SqlConnection(cadena);
        await conexion.OpenAsync(Ct);

        await using SqlCommand comando = conexion.CreateCommand();
        comando.CommandText = """
            SELECT s.name + N'.' + p.name
            FROM   sys.procedures AS p
            INNER JOIN sys.schemas AS s ON s.schema_id = p.schema_id;
            """;

        var nombres = new List<string>();
        await using SqlDataReader reader = await comando.ExecuteReaderAsync(Ct);

        while (await reader.ReadAsync(Ct))
        {
            nombres.Add(reader.GetString(0));
        }

        return nombres;
    }

    /// <summary>
    /// Reúne los nombres declarados en <see cref="Procedimientos"/>, recorriendo
    /// las clases anidadas de cada módulo.
    /// </summary>
    /// <returns>Los nombres que el código invoca.</returns>
    private static IReadOnlyList<string> NombresDelCodigo()
        => typeof(Procedimientos)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(modulo => modulo.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(campo => campo.IsLiteral && campo.FieldType == typeof(string))
            .Select(campo => (string)campo.GetRawConstantValue()!)
            .ToList();
}
