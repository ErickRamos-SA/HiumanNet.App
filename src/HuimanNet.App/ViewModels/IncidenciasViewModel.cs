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
/// Incidencias del período: una fila por contrato vigente.
/// </summary>
public sealed partial class IncidenciasViewModel : ViewModelBase
{
    private readonly IServicioDeApi _api;
    private readonly SesionDeLaApp _sesion;
    private readonly IServicioDeNavegacion _navegacion;
    private IReadOnlyList<IncidenciaDto> _todas = [];
    private bool _cambiandoPeriodos;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="IncidenciasViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    /// <param name="navegacion">Navegación.</param>
    public IncidenciasViewModel(Traductor traductor, IServicioDeApi api, SesionDeLaApp sesion, IServicioDeNavegacion navegacion)
        : base(traductor)
    {
        _api = api;
        _sesion = sesion;
        _navegacion = navegacion;
        Titulo = traductor["incidencias.titulo"];
    }

    /// <summary>Obtiene los períodos abiertos.</summary>
    /// <value>Opciones del selector.</value>
    public ObservableCollection<PeriodoDto> Periodos { get; } = [];

    /// <summary>Obtiene los nombres de los períodos para el selector.</summary>
    /// <value>En el mismo orden que <see cref="Periodos"/>.</value>
    public ObservableCollection<string> NombresDePeriodos { get; } = [];

    /// <summary>Obtiene o establece la posición del período elegido.</summary>
    /// <value>-1 si no hay períodos.</value>
    public int IndiceDePeriodo
    {
        get => Periodo is null ? -1 : Periodos.IndexOf(Periodo);
        set
        {
            if (value >= 0 && value < Periodos.Count && !Equals(Periodos[value], Periodo))
            {
                Periodo = Periodos[value];
            }
        }
    }

    /// <summary>Obtiene o establece el período elegido.</summary>
    /// <value>El primero abierto por defecto.</value>
    [ObservableProperty]
    public partial PeriodoDto? Periodo { get; set; }

    /// <summary>Obtiene o establece las filas visibles.</summary>
    /// <value>Se sustituye de una vez para no emitir miles de notificaciones.</value>
    [ObservableProperty]
    public partial IReadOnlyList<IncidenciaDto> Filas { get; set; } = [];

    /// <summary>Obtiene o establece el texto de búsqueda.</summary>
    /// <value>Filtra por clave, nombre o razón social.</value>
    [ObservableProperty]
    public partial string? Texto { get; set; }

    /// <summary>Obtiene o establece el resumen de captura.</summary>
    /// <value>Por ejemplo «3 de 40 contratos con captura».</value>
    [ObservableProperty]
    public partial string? Resumen { get; set; }

    /// <summary>
    /// Carga los períodos y las filas del elegido.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        Titulo = T["incidencias.titulo"];

        if (_sesion.FaltaEmpresa)
        {
            Periodos.Clear();
            _todas = [];
            Filtrar();
            MensajeDeError = T["comun.seleccioneEmpresaDetalle"];
            return;
        }

        IReadOnlyList<PeriodoDto> periodos = await _api.ListarPeriodosAsync(_sesion.EmpresaDeConsulta, incluirCerrados: false);

        _cambiandoPeriodos = true;
        Guid? anterior = Periodo?.Id;
        Reemplazar(Periodos, periodos);
        Reemplazar(NombresDePeriodos, periodos.Select(p => $"{p.Clave} · {p.Descripcion}"));
        Periodo = Periodos.FirstOrDefault(p => p.Id == anterior) ?? Periodos.FirstOrDefault();
        _cambiandoPeriodos = false;
        OnPropertyChanged(nameof(IndiceDePeriodo));

        await CargarFilasInternoAsync();
    });

    /// <summary>
    /// Recarga las filas del período elegido.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarFilasAsync() => EjecutarAsync(CargarFilasInternoAsync);

    /// <summary>
    /// Abre la captura de una fila.
    /// </summary>
    /// <param name="fila">Fila elegida.</param>
    /// <returns>Una tarea que representa la navegación.</returns>
    [RelayCommand]
    public Task EditarAsync(IncidenciaDto? fila)
        => fila is null
            ? Task.CompletedTask
            : _navegacion.IrAAsync(AppShell.RutaIncidencia, new Dictionary<string, object> { ["incidencia"] = fila });

    /// <summary>Carga las filas del período elegido, sin el indicador de ocupado.</summary>
    /// <returns>Tarea que finaliza al cargarlas.</returns>
    private async Task CargarFilasInternoAsync()
    {
        _todas = Periodo is { } periodo
            ? await _api.ListarIncidenciasAsync(periodo.Id, periodo.EmpresaId)
            : [];
        Filtrar();
    }

    /// <summary>
    /// Filtra las filas por clave, nombre o razón social y actualiza el resumen
    /// de capturadas.
    /// </summary>
    private void Filtrar()
    {
        string? texto = Texto?.Trim();

        Filas =string.IsNullOrEmpty(texto)
            ? _todas
            : [.. _todas.Where(f => f.ClaveEmpleado.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || f.NombreEmpleado.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || f.RazonSocialNombre.Contains(texto, StringComparison.OrdinalIgnoreCase))];

        Resumen = T.Formato("incidencias.resumen", _todas.Count(f => f.Id is not null), _todas.Count);
    }

    /// <summary>Carga las filas del período elegido por el usuario.</summary>
    /// <param name="value">Período elegido.</param>
    partial void OnPeriodoChanged(PeriodoDto? value)
    {
        OnPropertyChanged(nameof(IndiceDePeriodo));

        if (!_cambiandoPeriodos)
        {
            CargarFilasCommand.Execute(null);
        }
    }

    /// <summary>Vuelve a filtrar al cambiar el texto de búsqueda.</summary>
    /// <param name="value">Texto nuevo.</param>
    partial void OnTextoChanged(string? value) => Filtrar();
}
