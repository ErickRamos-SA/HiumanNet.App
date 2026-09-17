using System.Globalization;
using System.Text;
using HuimanNet.Domain.Entities;

namespace HuimanNet.Application.Nomina;

/// <summary>
/// Genera el archivo CSV de una corrida: una fila por contrato con todos los
/// conceptos como columnas.
/// </summary>
/// <remarks>
/// El archivo usa el mismo formato que acepta el cotejo (formato ancho), de
/// modo que el equipo de nómina puede tomarlo como plantilla para su resultado
/// manual.
/// </remarks>
public static class ExportadorDeCorridas
{
    /// <summary>
    /// Serializa los resultados a CSV con codificación UTF-8 y marca de orden de bytes.
    /// </summary>
    /// <param name="resultados">Resultados de la corrida.</param>
    /// <param name="nombresDeRazonSocial">Nombres de razón social por identificador.</param>
    /// <returns>Bytes del archivo.</returns>
    public static byte[] ACsv(IReadOnlyList<ResultadoDeNomina> resultados, IReadOnlyDictionary<Guid, string> nombresDeRazonSocial)
    {
        ArgumentNullException.ThrowIfNull(resultados);
        ArgumentNullException.ThrowIfNull(nombresDeRazonSocial);

        List<string> columnas = resultados
            .SelectMany(static r => r.Conceptos.Select(static c => c.Clave))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var sb = new StringBuilder();
        sb.Append("Clave,Nombre,RazonSocial,Esquema,Movimiento");

        foreach (string columna in columnas)
        {
            sb.Append(',').Append(columna);
        }

        sb.AppendLine();

        foreach (ResultadoDeNomina r in resultados)
        {
            var valores = r.Conceptos.ToDictionary(static c => c.Clave, static c => c.Importe, StringComparer.Ordinal);

            sb.Append(Escapar(r.ClaveEmpleado)).Append(',')
              .Append(Escapar(r.NombreEmpleado)).Append(',')
              .Append(Escapar(nombresDeRazonSocial.TryGetValue(r.RazonSocialId, out string? nombre) ? nombre : string.Empty)).Append(',')
              .Append(r.Esquema).Append(',')
              .Append(r.TipoDeMovimiento);

            foreach (string columna in columnas)
            {
                sb.Append(',');

                if (valores.TryGetValue(columna, out decimal importe))
                {
                    sb.Append(Math.Round(importe, 4, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture));
                }
            }

            sb.AppendLine();
        }

        byte[] bom = Encoding.UTF8.GetPreamble();
        byte[] cuerpo = Encoding.UTF8.GetBytes(sb.ToString());
        byte[] resultado = new byte[bom.Length + cuerpo.Length];
        bom.CopyTo(resultado, 0);
        cuerpo.CopyTo(resultado, bom.Length);
        return resultado;
    }

    /// <summary>Escapa un valor para CSV según RFC 4180.</summary>
    /// <param name="valor">Texto de la celda.</param>
    /// <returns>El texto entre comillas, con las comillas duplicadas, si contiene comas o comillas; si no, el mismo texto.</returns>
    private static string Escapar(string valor)
        => valor.Contains(',', StringComparison.Ordinal) || valor.Contains('"', StringComparison.Ordinal)
            ? "\"" + valor.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : valor;
}
