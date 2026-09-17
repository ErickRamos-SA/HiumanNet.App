using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HuimanNet.App.Services;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Contracts.Incidencias;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Services;

namespace HuimanNet.App.ViewModels;

/// <summary>
/// Valores editables de una incidencia.
/// </summary>
public sealed class ModeloIncidencia
{
    /// <summary>Días del período.</summary>
    public decimal DiasPeriodo { get; set; }

    /// <summary>Días de vacaciones.</summary>
    public decimal Vacaciones { get; set; }

    /// <summary>Faltas.</summary>
    public decimal Ausentismos { get; set; }

    /// <summary>Días de incapacidad.</summary>
    public decimal Incapacidades { get; set; }

    /// <summary>Festivos trabajados.</summary>
    public decimal Festivos { get; set; }

    /// <summary>Horas dobles.</summary>
    public decimal HorasDobles { get; set; }

    /// <summary>Horas triples.</summary>
    public decimal HorasTriples { get; set; }

    /// <summary>Domingos trabajados.</summary>
    public decimal DomingosTrabajados { get; set; }

    /// <summary>Gratificación o bonos.</summary>
    public decimal Gratificacion { get; set; }

    /// <summary>Reembolsos.</summary>
    public decimal Reembolsos { get; set; }

    /// <summary>Apoyo de teletrabajo.</summary>
    public decimal Teletrabajo { get; set; }

    /// <summary>Finiquito.</summary>
    public decimal Finiquito { get; set; }

    /// <summary>Cafetería.</summary>
    public decimal Cafeteria { get; set; }

    /// <summary>Horas descontadas.</summary>
    public decimal HorasDescontadas { get; set; }

    /// <summary>Otros descuentos.</summary>
    public decimal OtrosDescuentos { get; set; }

    /// <summary>Préstamo personal.</summary>
    public decimal PrestamoPersonal { get; set; }

    /// <summary>Aguinaldo.</summary>
    public decimal Aguinaldo { get; set; }

    /// <summary>Descuentos fiscales.</summary>
    public decimal DescuentosFiscales { get; set; }

    /// <summary>FONACOT capturado.</summary>
    public decimal FonacotCapturado { get; set; }

    /// <summary>Descuento sindical adicional.</summary>
    public decimal DescuentoSindicalAdicional { get; set; }

    /// <summary>Ajuste sindical.</summary>
    public decimal AjusteSindical { get; set; }

    /// <summary>ISR manual como texto; vacío para que lo calcule el sistema.</summary>
    public string? IsrManualTexto { get; set; }

    /// <summary>Tipo de movimiento.</summary>
    public TipoDeMovimiento TipoDeMovimiento { get; set; }

    /// <summary>Observaciones.</summary>
    public string? Observaciones { get; set; }

    /// <summary>
    /// Crea el modelo a partir de una fila.
    /// </summary>
    /// <param name="d">Fila.</param>
    /// <returns>El modelo.</returns>
    public static ModeloIncidencia Desde(IncidenciaDto d)
    {
        ArgumentNullException.ThrowIfNull(d);

        return new ModeloIncidencia
        {
            DiasPeriodo = d.DiasPeriodo,
            Vacaciones = d.Vacaciones,
            Ausentismos = d.Ausentismos,
            Incapacidades = d.Incapacidades,
            Festivos = d.Festivos,
            HorasDobles = d.HorasDobles,
            HorasTriples = d.HorasTriples,
            DomingosTrabajados = d.DomingosTrabajados,
            Gratificacion = d.Gratificacion,
            Reembolsos = d.Reembolsos,
            Teletrabajo = d.Teletrabajo,
            Finiquito = d.Finiquito,
            Cafeteria = d.Cafeteria,
            HorasDescontadas = d.HorasDescontadas,
            OtrosDescuentos = d.OtrosDescuentos,
            PrestamoPersonal = d.PrestamoPersonal,
            Aguinaldo = d.Aguinaldo,
            DescuentosFiscales = d.DescuentosFiscales,
            FonacotCapturado = d.FonacotCapturado,
            DescuentoSindicalAdicional = d.DescuentoSindicalAdicional,
            AjusteSindical = d.AjusteSindical,
            IsrManualTexto = d.IsrManual?.ToString(CultureInfo.InvariantCulture),
            TipoDeMovimiento = d.TipoDeMovimiento,
            Observaciones = d.Observaciones,
        };
    }

    /// <summary>
    /// Interpreta el ISR manual.
    /// </summary>
    /// <param name="isr">Valor leído, o <c>null</c> si está vacío.</param>
    /// <returns><c>false</c> si el texto no es un número.</returns>
    public bool IntentarLeerIsr(out decimal? isr)
    {
        isr = null;

        if (string.IsNullOrWhiteSpace(IsrManualTexto))
        {
            return true;
        }

        string texto = IsrManualTexto.Trim().Replace(',', '.');

        if (decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal valor))
        {
            isr = valor;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Arma la petición para la API.
    /// </summary>
    /// <param name="periodoId">Período.</param>
    /// <param name="contratoId">Contrato.</param>
    /// <param name="empresaId">Empresa de trabajo, o <c>null</c>.</param>
    /// <param name="isr">ISR manual ya interpretado.</param>
    /// <returns>La petición.</returns>
    public GuardarIncidenciaRequest ARequest(Guid periodoId, Guid contratoId, Guid? empresaId, decimal? isr) => new(
        periodoId, contratoId, empresaId, DiasPeriodo, Vacaciones, Ausentismos, Incapacidades, Festivos,
        HorasDobles, HorasTriples, DomingosTrabajados, Gratificacion, Reembolsos, Teletrabajo, Finiquito,
        Cafeteria, HorasDescontadas, OtrosDescuentos, PrestamoPersonal, Aguinaldo, DescuentosFiscales,
        FonacotCapturado, DescuentoSindicalAdicional, AjusteSindical, isr, TipoDeMovimiento,
        string.IsNullOrWhiteSpace(Observaciones) ? null : Observaciones.Trim());
}
