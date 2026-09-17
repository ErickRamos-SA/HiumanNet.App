using HuimanNet.Application.Interfaces;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Nomina;

namespace HuimanNet.Application.Incidencias;

/// <summary>
/// Interpreta la hoja de incidencias del modelo de referencia.
/// </summary>
/// <remarks>
/// Las columnas se reconocen por su encabezado, sin distinguir mayúsculas,
/// acentos ni espacios. Se aceptan los encabezados de la hoja
/// <i>Incidencias</i> del archivo de cálculo y las claves de variable del
/// motor, de modo que un archivo exportado por el propio sistema también es
/// importable.
/// </remarks>
public static class ImportadorDeIncidencias
{
    /// <summary>
    /// Resultado de interpretar el archivo.
    /// </summary>
    /// <param name="FilasLeidas">Filas con clave de trabajador.</param>
    /// <param name="Incidencias">Incidencias a guardar, por contrato.</param>
    /// <param name="Errores">Filas rechazadas, con el motivo.</param>
    public sealed record Plan(int FilasLeidas, IReadOnlyList<(Guid ContratoId, DatosDeIncidencia Datos)> Incidencias, IReadOnlyList<string> Errores);

    /// <summary>
    /// Interpreta la tabla y produce las incidencias a guardar.
    /// </summary>
    /// <param name="tabla">Archivo leído.</param>
    /// <param name="contexto">Datos de referencia de la empresa.</param>
    /// <param name="diasPredeterminados">Días del período cuando el archivo no los trae.</param>
    /// <returns>El plan de importación.</returns>
    /// <exception cref="NominaInvalidaException">Se lanza si el archivo no tiene columna de clave.</exception>
    public static Plan Interpretar(TablaLeida tabla, ContextoDeImportacion contexto, decimal diasPredeterminados)
    {
        ArgumentNullException.ThrowIfNull(tabla);
        ArgumentNullException.ThrowIfNull(contexto);

        int cClave = tabla.IndiceDe("Clave", "Clave empleado", "Trabajador", "NOI", "Numero", "No");

        if (cClave < 0)
        {
            throw new NominaInvalidaException("El archivo debe tener una columna 'Clave' con la clave del trabajador.");
        }

        int cDias = tabla.IndiceDe("Dias de Periodo", "Dias Periodo", "Dias del periodo", VariablesDeCalculo.DiasPeriodo);
        int cVac = tabla.IndiceDe("Vacaciones", VariablesDeCalculo.Vacaciones);
        int cAus = tabla.IndiceDe("Ausentismos", "Faltas", "Ausencias", VariablesDeCalculo.Ausentismos);
        int cInc = tabla.IndiceDe("Incapacidades", "Incapacidad", VariablesDeCalculo.Incapacidades);
        int cFes = tabla.IndiceDe("Cantidad festivo", "Festivos", "Cantidad festivos", VariablesDeCalculo.Festivos);
        int cDob = tabla.IndiceDe("Cantidad dobles", "Horas dobles", VariablesDeCalculo.HorasDobles);
        int cTri = tabla.IndiceDe("Cantidad triples", "Horas triples", VariablesDeCalculo.HorasTriples);
        int cDom = tabla.IndiceDe("Cantidad p dominical", "Domingos", "Prima dominical cantidad", VariablesDeCalculo.Domingos);
        int cGra = tabla.IndiceDe("Gratificacion / Bonos", "Gratificacion", "Bonos", VariablesDeCalculo.Gratificacion);
        int cRee = tabla.IndiceDe("Reembolsos y otros", "Reembolsos", VariablesDeCalculo.Reembolsos);
        int cTel = tabla.IndiceDe("Teletrabajo", VariablesDeCalculo.Teletrabajo);
        int cFin = tabla.IndiceDe("Finiquito", VariablesDeCalculo.Finiquito);
        int cCaf = tabla.IndiceDe("Gastos Cafeteria", "Cafeteria", VariablesDeCalculo.Cafeteria);
        int cHde = tabla.IndiceDe("Cantidad Horas", "Horas descontadas", VariablesDeCalculo.HorasDescontadas);
        int cOtr = tabla.IndiceDe("Otros Desc.", "Otros descuentos", "Otros Desc", VariablesDeCalculo.OtrosDescuentos);
        int cPre = tabla.IndiceDe("Descuento Prestamo Personal", "Prestamo personal", "Prestamo", VariablesDeCalculo.PrestamoPersonal);
        int cAgu = tabla.IndiceDe("Aguinaldo", VariablesDeCalculo.Aguinaldo);
        int cDfi = tabla.IndiceDe("Descuentos fiscales", "Otros", VariablesDeCalculo.DescuentosFiscales);
        int cFon = tabla.IndiceDe("Fonacot", "Credito Fonacot", VariablesDeCalculo.FonacotCapturado);
        int cDsa = tabla.IndiceDe("Descuento sindical adicional", "Descuentos", VariablesDeCalculo.DescuentoSindicalAdicional);
        int cAjs = tabla.IndiceDe("Ajuste sindical", VariablesDeCalculo.AjusteSindical);
        int cIsr = tabla.IndiceDe("ISR manual", "Ret Real", VariablesDeCalculo.IsrManual);
        int cMov = tabla.IndiceDe("Tipo de movimiento", "Movimiento", "Tipo movimiento");
        int cObs = tabla.IndiceDe("Observaciones", "Observacion", "Notas");
        int cEsq = tabla.IndiceDe("Esquema");
        int cRaz = tabla.IndiceDe("Razon Social", "RazonSocial");

        var incidencias = new List<(Guid, DatosDeIncidencia)>();
        var errores = new List<string>();
        int filasLeidas = 0;

        for (int i = 0; i < tabla.Filas.Count; i++)
        {
            string?[] fila = tabla.Filas[i];
            int numeroDeFila = tabla.PrimeraFilaDeDatos + i;
            string? clave = TablaLeida.Texto(fila, cClave);

            if (clave is null)
            {
                continue;
            }

            filasLeidas++;

            if (!contexto.EmpleadosPorClave.TryGetValue(clave, out Empleado? empleado))
            {
                errores.Add($"Fila {numeroDeFila}: no existe un empleado activo con clave '{clave}'.");
                continue;
            }

            List<Contrato> contratos = FiltrarContratos(contexto, empleado, TablaLeida.Texto(fila, cEsq), TablaLeida.Texto(fila, cRaz));

            if (contratos.Count == 0)
            {
                errores.Add($"Fila {numeroDeFila}: el empleado '{clave}' no tiene contratos vigentes que coincidan.");
                continue;
            }

            try
            {
                DatosDeIncidencia datos = Leer(
                    fila, diasPredeterminados, cDias, cVac, cAus, cInc, cFes, cDob, cTri, cDom, cGra, cRee, cTel, cFin,
                    cCaf, cHde, cOtr, cPre, cAgu, cDfi, cFon, cDsa, cAjs, cIsr, cMov, cObs);

                foreach (Contrato contrato in contratos)
                {
                    incidencias.Add((contrato.Id, datos));
                }
            }
            catch (FormatException)
            {
                errores.Add($"Fila {numeroDeFila}: alguna celda numérica contiene texto no válido.");
            }
            catch (NominaInvalidaException excepcion)
            {
                errores.Add($"Fila {numeroDeFila}: {excepcion.Message}");
            }
        }

        return new Plan(filasLeidas, incidencias, errores);
    }

    /// <summary>
    /// Elige los contratos del empleado a los que se aplica una fila; filtra por
    /// esquema y razón social cuando la fila los indica.
    /// </summary>
    /// <param name="contexto">Contratos y razones sociales de la empresa.</param>
    /// <param name="empleado">Empleado de la fila.</param>
    /// <param name="esquema">Esquema indicado en la fila, o <c>null</c>.</param>
    /// <param name="razonSocial">Razón social indicada en la fila, o <c>null</c>.</param>
    /// <returns>Los contratos que coinciden.</returns>
    private static List<Contrato> FiltrarContratos(ContextoDeImportacion contexto, Empleado empleado, string? esquema, string? razonSocial)
    {
        IEnumerable<Contrato> contratos = contexto.ContratosPorEmpleado[empleado.Id];

        if (esquema is not null)
        {
            string e = TablaLeida.Normalizar(esquema);
            contratos = contratos.Where(c => TablaLeida.Normalizar(c.Esquema.ToString()) == e);
        }

        if (razonSocial is not null)
        {
            string r = TablaLeida.Normalizar(razonSocial);
            contratos = contratos.Where(c =>
                contexto.RazonesSociales.TryGetValue(c.RazonSocialId, out RazonSocial? rs)
                && TablaLeida.Normalizar(rs.Nombre) == r);
        }

        return contratos.ToList();
    }

    /// <summary>
    /// Convierte una fila del archivo en las cantidades de una incidencia. Las
    /// columnas ausentes o vacías se leen como cero.
    /// </summary>
    /// <param name="fila">Celdas de la fila.</param>
    /// <param name="diasPredeterminados">Días del período cuando la fila no los indica.</param>
    /// <param name="cDias">Índice de la columna de días del período.</param>
    /// <param name="cVac">Índice de la columna de vacaciones.</param>
    /// <param name="cAus">Índice de la columna de ausentismos.</param>
    /// <param name="cInc">Índice de la columna de incapacidades.</param>
    /// <param name="cFes">Índice de la columna de festivos laborados.</param>
    /// <param name="cDob">Índice de la columna de horas dobles.</param>
    /// <param name="cTri">Índice de la columna de horas triples.</param>
    /// <param name="cDom">Índice de la columna de domingos trabajados.</param>
    /// <param name="cGra">Índice de la columna de gratificación.</param>
    /// <param name="cRee">Índice de la columna de reembolsos.</param>
    /// <param name="cTel">Índice de la columna de teletrabajo.</param>
    /// <param name="cFin">Índice de la columna de finiquito.</param>
    /// <param name="cCaf">Índice de la columna de cafetería.</param>
    /// <param name="cHde">Índice de la columna de horas descontadas.</param>
    /// <param name="cOtr">Índice de la columna de otros descuentos.</param>
    /// <param name="cPre">Índice de la columna de préstamo personal.</param>
    /// <param name="cAgu">Índice de la columna de aguinaldo.</param>
    /// <param name="cDfi">Índice de la columna de descuentos fiscales.</param>
    /// <param name="cFon">Índice de la columna de FONACOT.</param>
    /// <param name="cDsa">Índice de la columna de descuento sindical adicional.</param>
    /// <param name="cAjs">Índice de la columna de ajuste sindical.</param>
    /// <param name="cIsr">Índice de la columna de ISR manual; vacía para calcularlo.</param>
    /// <param name="cMov">Índice de la columna de tipo de movimiento; si contiene «finiquito» es finiquito.</param>
    /// <param name="cObs">Índice de la columna de observaciones.</param>
    /// <returns>Las cantidades de la fila, aún sin validar.</returns>
    private static DatosDeIncidencia Leer(
        string?[] fila, decimal diasPredeterminados,
        int cDias, int cVac, int cAus, int cInc, int cFes, int cDob, int cTri, int cDom, int cGra, int cRee, int cTel,
        int cFin, int cCaf, int cHde, int cOtr, int cPre, int cAgu, int cDfi, int cFon, int cDsa, int cAjs, int cIsr,
        int cMov, int cObs)
    {
        decimal N(int indice) => TablaLeida.Numero(fila, indice, out decimal valor) ? valor : 0m;

        decimal dias = TablaLeida.Numero(fila, cDias, out decimal d) ? d : diasPredeterminados;
        decimal? isrManual = TablaLeida.Numero(fila, cIsr, out decimal isr) ? isr : null;
        string? movimiento = TablaLeida.Texto(fila, cMov);

        TipoDeMovimiento tipo = movimiento is not null && TablaLeida.Normalizar(movimiento).Contains("FINIQUITO", StringComparison.Ordinal)
            ? TipoDeMovimiento.Finiquito
            : TipoDeMovimiento.Ordinaria;

        return new DatosDeIncidencia(
            dias, N(cVac), N(cAus), N(cInc), N(cFes), N(cDob), N(cTri), N(cDom), N(cGra), N(cRee), N(cTel), N(cFin),
            N(cCaf), N(cHde), N(cOtr), N(cPre), N(cAgu), N(cDfi), N(cFon), N(cDsa), N(cAjs), isrManual, tipo,
            TablaLeida.Texto(fila, cObs));
    }
}
