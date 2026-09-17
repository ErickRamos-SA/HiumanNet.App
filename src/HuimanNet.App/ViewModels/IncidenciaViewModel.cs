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
/// Captura de la incidencia de un contrato.
/// </summary>
public sealed partial class IncidenciaViewModel : ViewModelBase, IQueryAttributable
{
    private readonly IServicioDeApi _api;
    private readonly IServicioDeDialogos _dialogos;
    private readonly IServicioDeNavegacion _navegacion;
    private readonly SesionDeLaApp _sesion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="IncidenciaViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="dialogos">Diálogos.</param>
    /// <param name="navegacion">Navegación.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    public IncidenciaViewModel(
        Traductor traductor, IServicioDeApi api, IServicioDeDialogos dialogos, IServicioDeNavegacion navegacion, SesionDeLaApp sesion)
        : base(traductor)
    {
        _api = api;
        _dialogos = dialogos;
        _navegacion = navegacion;
        _sesion = sesion;
        Titulo = traductor["incidencias.editar"];
    }

    /// <summary>Obtiene o establece la fila capturada.</summary>
    /// <value>Llega como parámetro de navegación.</value>
    [ObservableProperty]
    public partial IncidenciaDto? Incidencia { get; set; }

    /// <summary>Obtiene los valores editables.</summary>
    /// <value>Se enlaza a los campos del formulario.</value>
    [ObservableProperty]
    public partial ModeloIncidencia Modelo { get; set; } = new();

    /// <summary>Obtiene los tipos de movimiento.</summary>
    /// <value>Ordinaria y finiquito.</value>
    public IReadOnlyList<TipoDeMovimiento> TiposDeMovimiento { get; } = Enum.GetValues<TipoDeMovimiento>();

    /// <summary>Obtiene los nombres traducidos de los tipos de movimiento.</summary>
    /// <value>En el mismo orden que <see cref="TiposDeMovimiento"/>.</value>
    public IReadOnlyList<string> NombresDeMovimientos => [.. TiposDeMovimiento.Select(t => T.Enumerado(t))];

    /// <summary>Obtiene o establece la posición del tipo de movimiento.</summary>
    /// <value>0 ordinaria, 1 finiquito.</value>
    public int IndiceDeMovimiento
    {
        get => TiposDeMovimiento.ToList().IndexOf(Modelo.TipoDeMovimiento);
        set
        {
            if (value >= 0 && value < TiposDeMovimiento.Count)
            {
                Modelo.TipoDeMovimiento = TiposDeMovimiento[value];
            }
        }
    }

    /// <summary>Indica si hay una captura que se pueda restablecer.</summary>
    /// <value><c>true</c> si la fila ya tenía incidencia guardada.</value>
    public bool PuedeRestablecer => Incidencia?.Id is not null;

    /// <inheritdoc/>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.TryGetValue("incidencia", out object? valor) && valor is IncidenciaDto fila)
        {
            Incidencia = fila;
            Modelo = ModeloIncidencia.Desde(fila);
            OnPropertyChanged(nameof(IndiceDeMovimiento));
            Titulo = $"{fila.ClaveEmpleado} · {fila.NombreEmpleado}";
            OnPropertyChanged(nameof(PuedeRestablecer));
        }
    }

    /// <summary>
    /// Guarda la captura y vuelve a la lista.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public async Task GuardarAsync()
    {
        if (Incidencia is not { } fila)
        {
            return;
        }

        if (!Modelo.IntentarLeerIsr(out decimal? isr))
        {
            MensajeDeError = T["movil.valorInvalido"];
            return;
        }

        bool exito = await EjecutarAsync(() => _api.GuardarIncidenciaAsync(Modelo.ARequest(fila.PeriodoId, fila.ContratoId, _sesion.EmpresaDeConsulta, isr)));

        if (exito)
        {
            await _navegacion.VolverAsync();
        }
    }

    /// <summary>
    /// Elimina la captura para volver a período completo.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public async Task RestablecerAsync()
    {
        if (Incidencia is not { Id: { } incidenciaId } fila)
        {
            return;
        }

        string boton = T["incidencias.restablecer"];

        if (!await _dialogos.ConfirmarAsync(boton, T.Formato("incidencias.confirmarRestablecer", fila.NombreEmpleado), boton))
        {
            return;
        }

        if (await EjecutarAsync(() => _api.EliminarIncidenciaAsync(incidenciaId, _sesion.EmpresaDeConsulta)))
        {
            await _navegacion.VolverAsync();
        }
    }
}
