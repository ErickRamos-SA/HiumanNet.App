using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Nomina;

namespace HuimanNet.Application.Nomina;

/// <summary>
/// Interpreta el archivo de resultados manual y lo compara con los resultados del sistema.
/// </summary>
/// <remarks>
/// Los encabezados del archivo se reconocen por la clave del concepto o por
/// los alias de cotejo que el administrador define en cada concepto del
/// catálogo; ninguna equivalencia vive en el código.
/// </remarks>
public static class LectorDeResultadosManuales
{
    /// <summary>
    /// Construye el índice de alias de cotejo de los conceptos activos.
    /// </summary>
    /// <param name="conceptos">Conceptos efectivos del catálogo de la empresa.</param>
    /// <returns>
    /// La clave del concepto por alias normalizado; si dos conceptos comparten
    /// un alias, gana el primero en orden de presentación.
    /// </returns>
    public static IReadOnlyDictionary<string, string> IndiceDeAlias(IEnumerable<ConceptoDeNomina> conceptos)
    {
        ArgumentNullException.ThrowIfNull(conceptos);

        var indice = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (ConceptoDeNomina concepto in conceptos.Where(static c => c.Activo).OrderBy(static c => c.Orden))
        {
            foreach (string alias in concepto.AliasDeCotejo)
            {
                string normalizado = TablaLeida.Normalizar(alias);

                if (normalizado.Length > 0)
                {
                    indice.TryAdd(normalizado, concepto.Clave);
                }
            }
        }

        return indice;
    }

    /// <summary>
    /// Compara el archivo manual con los resultados del sistema.
    /// </summary>
    /// <param name="tabla">Archivo manual leído.</param>
    /// <param name="resultados">Resultados de la corrida.</param>
    /// <param name="alias">Índice de alias de cotejo, de <see cref="IndiceDeAlias"/>.</param>
    /// <param name="tolerancia">Diferencia absoluta aceptada.</param>
    /// <returns>Una comparación por trabajador y concepto presente en el archivo.</returns>
    /// <exception cref="NominaInvalidaException">Se lanza si el archivo no tiene columna de clave de trabajador.</exception>
    public static IReadOnlyList<DiferenciaDeCotejo> Comparar(
        TablaLeida tabla, IReadOnlyList<ResultadoDeNomina> resultados, IReadOnlyDictionary<string, string> alias, decimal tolerancia)
    {
        ArgumentNullException.ThrowIfNull(tabla);
        ArgumentNullException.ThrowIfNull(resultados);
        ArgumentNullException.ThrowIfNull(alias);

        int columnaClave = tabla.IndiceDe("Clave", "Clave empleado", "ClaveEmpleado", "Trabajador", "Empleado", "NOI", "Numero");

        if (columnaClave < 0)
        {
            throw new NominaInvalidaException(
                "El archivo debe tener una columna 'Clave' con la clave del trabajador.");
        }

        ILookup<string, ResultadoDeNomina> porClave = resultados.ToLookup(static r => r.ClaveEmpleado, StringComparer.OrdinalIgnoreCase);
        var conceptosDelSistema = new HashSet<string>(
            resultados.SelectMany(static r => r.Conceptos.Select(static c => c.Clave)), StringComparer.Ordinal);

        int columnaConcepto = tabla.IndiceDe("Concepto", "Clave concepto", "ConceptoClave");
        int columnaImporte = tabla.IndiceDe("Importe", "Valor", "Monto");
        var diferencias = new List<DiferenciaDeCotejo>();

        if (columnaConcepto >= 0 && columnaImporte >= 0)
        {
            foreach (string?[] fila in tabla.Filas)
            {
                string? clave = TablaLeida.Texto(fila, columnaClave);
                string? concepto = TablaLeida.Texto(fila, columnaConcepto);

                if (clave is null || concepto is null || !TablaLeida.Numero(fila, columnaImporte, out decimal importe))
                {
                    continue;
                }

                string claveConcepto = ResolverConcepto(concepto, conceptosDelSistema, alias) ?? concepto.Trim().ToUpperInvariant();
                diferencias.Add(Comparar(clave, claveConcepto, importe, porClave[clave], tolerancia));
            }
        }
        else
        {
            var columnas = new List<(int Indice, string Concepto)>();

            for (int i = 0; i < tabla.Encabezados.Count; i++)
            {
                if (i == columnaClave)
                {
                    continue;
                }

                string? concepto = ResolverConcepto(tabla.Encabezados[i], conceptosDelSistema, alias);

                if (concepto is not null)
                {
                    columnas.Add((i, concepto));
                }
            }

            foreach (string?[] fila in tabla.Filas)
            {
                string? clave = TablaLeida.Texto(fila, columnaClave);

                if (clave is null)
                {
                    continue;
                }

                foreach ((int indice, string concepto) in columnas)
                {
                    if (TablaLeida.Numero(fila, indice, out decimal importe))
                    {
                        diferencias.Add(Comparar(clave, concepto, importe, porClave[clave], tolerancia));
                    }
                }
            }
        }

        return diferencias
            .OrderBy(static d => d.ClaveEmpleado, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static d => d.ConceptoClave, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Relaciona un encabezado del archivo manual con un concepto del sistema,
    /// por su clave normalizada o por un alias de cotejo del catálogo.
    /// </summary>
    /// <param name="encabezado">Encabezado de la columna.</param>
    /// <param name="conceptosDelSistema">Claves que produjo la corrida.</param>
    /// <param name="alias">Índice de alias de cotejo.</param>
    /// <returns>La clave del concepto, o <c>null</c> si la columna no corresponde a ninguno.</returns>
    private static string? ResolverConcepto(
        string encabezado, HashSet<string> conceptosDelSistema, IReadOnlyDictionary<string, string> alias)
    {
        string normalizado = TablaLeida.Normalizar(encabezado);

        if (normalizado.Length == 0)
        {
            return null;
        }

        foreach (string clave in conceptosDelSistema)
        {
            if (TablaLeida.Normalizar(clave) == normalizado)
            {
                return clave;
            }
        }

        return alias.TryGetValue(normalizado, out string? conceptoDelAlias) ? conceptoDelAlias : null;
    }

    /// <summary>
    /// Compara el importe manual de un concepto con la suma de lo que calculó el
    /// sistema para todos los contratos del empleado. Ambos se redondean a dos decimales.
    /// </summary>
    /// <param name="clave">Clave del empleado.</param>
    /// <param name="concepto">Clave del concepto.</param>
    /// <param name="importeManual">Importe del archivo manual.</param>
    /// <param name="resultados">Resultados del empleado en la corrida.</param>
    /// <param name="tolerancia">Diferencia absoluta aceptada.</param>
    /// <returns>
    /// La diferencia; fuera de tolerancia si el sistema no calculó el concepto.
    /// </returns>
    private static DiferenciaDeCotejo Comparar(
        string clave, string concepto, decimal importeManual, IEnumerable<ResultadoDeNomina> resultados, decimal tolerancia)
    {
        decimal? importeSistema = null;
        Guid? contratoId = null;

        foreach (ResultadoDeNomina resultado in resultados)
        {
            foreach (ValorDeConcepto valor in resultado.Conceptos)
            {
                if (valor.Clave == concepto)
                {
                    importeSistema = (importeSistema ?? 0m) + valor.Importe;
                    contratoId ??= resultado.ContratoId;
                }
            }
        }

        decimal manual = Math.Round(importeManual, 2, MidpointRounding.AwayFromZero);
        decimal sistema = Math.Round(importeSistema ?? 0m, 2, MidpointRounding.AwayFromZero);
        decimal diferencia = sistema - manual;

        return new DiferenciaDeCotejo(
            clave.Trim(),
            contratoId,
            concepto,
            importeSistema is null ? null : sistema,
            manual,
            diferencia,
            importeSistema is not null && Math.Abs(diferencia) <= tolerancia);
    }
}
