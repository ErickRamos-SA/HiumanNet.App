using System.Reflection;
using FluentAssertions;
using HuimanNet.Application.Common;
using HuimanNet.Infrastructure.Persistence.Semillas;
using Xunit;

namespace HuimanNet.Infrastructure.Tests;

/// <summary>
/// Comprueba que los scripts SQL incrustados coinciden con las constantes que
/// el código usa para referirse a sus filas.
/// </summary>
/// <remarks>
/// No toca la base de datos: lee los recursos del ensamblado. Un script SQL no
/// puede referirse a una constante de C#, así que esta prueba es la que ata
/// ambos valores.
/// </remarks>
public sealed class ScriptsDeBaseDeDatosTests
{
    private const string PrefijoDeRecursos = "HuimanNet.Infrastructure.Persistence.Scripts.";

    [Fact]
    public void DatosSemilla_CreanLaCuentaDeSistemaConElIdentificadorDelCodigo()
    {
        string script = LeerScript("0002_Datos_Semilla.sql");

        script.Should().Contain($"'{IdentidadesDelSistema.Sistema:D}'");
    }

    private static string LeerScript(string nombre)
    {
        Assembly ensamblado = typeof(CatalogoInicial).Assembly;

        using Stream flujo = ensamblado.GetManifestResourceStream(PrefijoDeRecursos + nombre)
            ?? throw new InvalidOperationException($"No se encontró el script incrustado '{nombre}'.");
        using var lector = new StreamReader(flujo);

        return lector.ReadToEnd();
    }
}
