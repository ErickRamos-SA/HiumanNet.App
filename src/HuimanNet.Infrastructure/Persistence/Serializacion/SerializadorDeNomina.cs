using System.Buffers;
using System.Text;
using System.Text.Json;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Nomina;

namespace HuimanNet.Infrastructure.Persistence.Serializacion;

/// <summary>
/// Serializa el detalle de conceptos de un resultado y las diferencias de un
/// cotejo al formato JSON compacto con el que se guardan en columnas
/// <c>NVARCHAR(MAX)</c>.
/// </summary>
/// <remarks>
/// Se escribe y se lee con <see cref="Utf8JsonWriter"/> y <see cref="Utf8JsonReader"/>
/// directamente: sin reflexión (compatible con Native AOT) y sin materializar
/// objetos intermedios, lo que importa en corridas de miles de trabajadores.
/// El orden de los conceptos se conserva: es el orden de evaluación del motor.
/// </remarks>
public static class SerializadorDeNomina
{
    /// <summary>
    /// Serializa los conceptos como un objeto <c>{"CLAVE": importe, ...}</c>.
    /// </summary>
    /// <param name="conceptos">Conceptos en orden de evaluación.</param>
    /// <returns>El texto JSON.</returns>
    public static string SerializarConceptos(IReadOnlyList<ValorDeConcepto> conceptos)
    {
        ArgumentNullException.ThrowIfNull(conceptos);

        var buffer = new ArrayBufferWriter<byte>(conceptos.Count * 32 + 2);

        using (var escritor = new Utf8JsonWriter(buffer))
        {
            escritor.WriteStartObject();

            foreach (ValorDeConcepto valor in conceptos)
            {
                escritor.WriteNumber(valor.Clave, Math.Round(valor.Importe, 6, MidpointRounding.AwayFromZero));
            }

            escritor.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Lee los conceptos serializados por <see cref="SerializarConceptos"/>.
    /// </summary>
    /// <param name="json">Texto JSON.</param>
    /// <returns>Los conceptos en su orden original.</returns>
    public static IReadOnlyList<ValorDeConcepto> LeerConceptos(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

        byte[] bytes = Encoding.UTF8.GetBytes(json);
        var lector = new Utf8JsonReader(bytes);
        var lista = new List<ValorDeConcepto>(64);
        string? clave = null;

        while (lector.Read())
        {
            if (lector.TokenType == JsonTokenType.PropertyName)
            {
                clave = lector.GetString();
            }
            else if (lector.TokenType == JsonTokenType.Number && clave is not null)
            {
                lista.Add(new ValorDeConcepto(clave, lector.GetDecimal()));
                clave = null;
            }
        }

        return lista;
    }

    /// <summary>
    /// Serializa las diferencias de un cotejo como un arreglo de arreglos
    /// <c>[clave, contratoId, concepto, sistema, manual, diferencia, dentro]</c>.
    /// </summary>
    /// <param name="diferencias">Diferencias del cotejo.</param>
    /// <returns>El texto JSON.</returns>
    public static string SerializarDiferencias(IReadOnlyList<DiferenciaDeCotejo> diferencias)
    {
        ArgumentNullException.ThrowIfNull(diferencias);

        var buffer = new ArrayBufferWriter<byte>(diferencias.Count * 96 + 2);

        using (var escritor = new Utf8JsonWriter(buffer))
        {
            escritor.WriteStartArray();

            foreach (DiferenciaDeCotejo d in diferencias)
            {
                escritor.WriteStartArray();
                escritor.WriteStringValue(d.ClaveEmpleado);

                if (d.ContratoId is { } contratoId)
                {
                    escritor.WriteStringValue(contratoId);
                }
                else
                {
                    escritor.WriteNullValue();
                }

                escritor.WriteStringValue(d.ConceptoClave);
                EscribirNumero(escritor, d.ImporteSistema);
                EscribirNumero(escritor, d.ImporteManual);
                escritor.WriteNumberValue(d.Diferencia);
                escritor.WriteBooleanValue(d.DentroDeTolerancia);
                escritor.WriteEndArray();
            }

            escritor.WriteEndArray();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Lee las diferencias serializadas por <see cref="SerializarDiferencias"/>.
    /// </summary>
    /// <param name="json">Texto JSON.</param>
    /// <returns>Las diferencias en su orden original.</returns>
    public static IReadOnlyList<DiferenciaDeCotejo> LeerDiferencias(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

        using JsonDocument documento = JsonDocument.Parse(json);
        var lista = new List<DiferenciaDeCotejo>(documento.RootElement.GetArrayLength());

        foreach (JsonElement fila in documento.RootElement.EnumerateArray())
        {
            lista.Add(new DiferenciaDeCotejo(
                fila[0].GetString() ?? string.Empty,
                fila[1].ValueKind == JsonValueKind.Null ? null : fila[1].GetGuid(),
                fila[2].GetString() ?? string.Empty,
                fila[3].ValueKind == JsonValueKind.Null ? null : fila[3].GetDecimal(),
                fila[4].ValueKind == JsonValueKind.Null ? null : fila[4].GetDecimal(),
                fila[5].GetDecimal(),
                fila[6].GetBoolean()));
        }

        return lista;
    }

    /// <summary>Escribe un número opcional en el JSON.</summary>
    /// <param name="escritor">Escritor JSON.</param>
    /// <param name="valor">Número, o <c>null</c> para escribir <c>null</c>.</param>
    private static void EscribirNumero(Utf8JsonWriter escritor, decimal? valor)
    {
        if (valor is { } v)
        {
            escritor.WriteNumberValue(v);
        }
        else
        {
            escritor.WriteNullValue();
        }
    }
}
