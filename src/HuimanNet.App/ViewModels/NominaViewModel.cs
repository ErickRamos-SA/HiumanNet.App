using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HuimanNet.App.Services;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Domain.Enums;

namespace HuimanNet.App.ViewModels;

/// <summary>
/// Corridas de nómina de un período y cálculo de una nueva.
/// </summary>
public sealed partial class NominaViewModel : ViewModelBase
{
    private readonly IServicioDeApi _api;
    private readonly SesionDeLaApp _sesion;
    private readonly IServicioDeNavegacion _navegacion;
    private readonly IServicioDeDialogos _dialogos;
    private bool _cambiandoPeriodos;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="NominaViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    /// <param name="navegacion">Navegación.</param>
    /// <param name="dialogos">Diálogos, para confirmar un reproceso.</param>
    public NominaViewModel(
        Traductor traductor, IServicioDeApi api, SesionDeLaApp sesion, IServicioDeNavegacion navegacion, IServicioDeDialogos dialogos)
        : base(traductor)
    {
        _api = api;
        _sesion = sesion;
        _navegacion = navegacion;
        _dialogos = dialogos;
        Titulo = traductor["nomina.titulo"];
    }

    /// <summary>Obtiene los períodos.</summary>
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
    /// <value>El primero no cerrado por defecto.</value>
    [ObservableProperty]
    public partial PeriodoDto? Periodo { get; set; }

    /// <summary>Obtiene o establece las corridas del período.</summary>
    /// <value>De la más reciente a la más antigua.</value>
    [ObservableProperty]
    public partial IReadOnlyList<CorridaDeNominaDto> Corridas { get; set; } = [];

    /// <summary>Obtiene o establece las observaciones de la corrida a calcular.</summary>
    /// <value>Opcional.</value>
    [ObservableProperty]
    public partial string? Observaciones { get; set; }

    /// <summary>Indica si el usuario puede calcular.</summary>
    /// <value><c>true</c> para nómina y administración; la empresa cliente consulta en solo lectura.</value>
    public bool PuedeCalcular => _sesion.Puede(AccionDelSistema.CalcularNomina);

    /// <summary>Obtiene el texto del botón de cálculo.</summary>
    /// <value>«Calcular nómina» la primera vez; «Reprocesar nómina» si el período ya tiene corridas.</value>
    public string TextoDeCalculo => Corridas.Count > 0 ? T["nomina.reprocesar"] : T["nomina.calcular"];

    /// <summary>Obtiene el aviso sobre el cálculo del período elegido.</summary>
    /// <value>Solo lectura, el motivo por el que no se puede calcular, o <c>null</c>.</value>
    public string? AvisoDeCalculo => !PuedeCalcular ? T["nomina.soloLectura"] : MotivoSinCalculo;

    /// <summary>
    /// Motivo por el que el período elegido no admite cálculo, o <c>null</c>.
    /// Usa la regla del dominio para explicarla antes de pulsar; el servidor la
    /// vuelve a aplicar al calcular.
    /// </summary>
    private string? MotivoSinCalculo => Periodo switch
    {
        null => null,
        { Estado: var estado } when !estado.AdmiteCalculo()
            => estado == EstadoPeriodo.Cerrado ? T["nomina.periodoCerrado"] : T["nomina.requiereArchivos"],
        _ => Corridas.FirstOrDefault(c => c.Estado == EstadoDeCorrida.Aprobada) is { } aprobada
            ? T.Formato("nomina.yaAprobada", aprobada.Numero)
            : null,
    };

    /// <summary>Indica si el botón de calcular está habilitado.</summary>
    /// <value><c>true</c> si el usuario puede calcular, hay período y nada lo impide.</value>
    private bool PuedeCalcularAhora =>PuedeCalcular && Periodo is not null && MotivoSinCalculo is null;

    /// <summary>
    /// Carga períodos y corridas.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        Titulo = T["nomina.titulo"];
        OnPropertyChanged(nameof(PuedeCalcular));
        ActualizarEstadoDelCalculo();

        if (_sesion.FaltaEmpresa)
        {
            Periodos.Clear();
            Corridas = [];
            MensajeDeError = T["comun.seleccioneEmpresaDetalle"];
            return;
        }

        IReadOnlyList<PeriodoDto> periodos = await _api.ListarPeriodosAsync(_sesion.EmpresaDeConsulta, incluirCerrados: true);

        _cambiandoPeriodos = true;
        Guid? anterior = Periodo?.Id;
        Reemplazar(Periodos, periodos);
        Reemplazar(NombresDePeriodos, periodos.Select(p => $"{p.Clave} · {p.Descripcion}"));
        Periodo = Periodos.FirstOrDefault(p => p.Id == anterior)
            ?? Periodos.FirstOrDefault(p => p.Estado != EstadoPeriodo.Cerrado)
            ?? Periodos.FirstOrDefault();
        _cambiandoPeriodos = false;
        OnPropertyChanged(nameof(IndiceDePeriodo));

        await CargarCorridasInternoAsync();
    });

    /// <summary>
    /// Recarga las corridas del período.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarCorridasAsync() => EjecutarAsync(CargarCorridasInternoAsync);

    /// <summary>
    /// Calcula la nómina del período (o la reprocesa, previa confirmación) y abre la corrida.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand(CanExecute = nameof(PuedeCalcularAhora))]
    public async Task CalcularAsync()
    {
        if (Periodo is not { } periodo)
        {
            return;
        }

        if (Corridas.Count > 0)
        {
            string boton = T["nomina.reprocesar"];
            string mensaje = T.Formato("nomina.confirmarReproceso", Corridas.Max(c => c.Numero) + 1);

            if (!await _dialogos.ConfirmarAsync(boton, mensaje, boton))
            {
                return;
            }
        }

        CorridaDeNominaDto? corrida = null;

        bool exito = await EjecutarAsync(async () =>
            corrida = await _api.CalcularNominaAsync(new CalcularNominaRequest(periodo.Id, EmpresaDe(periodo), Observaciones)));

        if (exito && corrida is not null)
        {
            Observaciones = null;
            await AbrirCorridaAsync(corrida);
        }
    }

    /// <summary>
    /// Abre el resumen de una corrida.
    /// </summary>
    /// <param name="corrida">Corrida elegida.</param>
    /// <returns>Una tarea que representa la navegación.</returns>
    [RelayCommand]
    public Task AbrirCorridaAsync(CorridaDeNominaDto? corrida)
        => corrida is null
            ? Task.CompletedTask
            : _navegacion.IrAAsync(AppShell.RutaCorrida, new Dictionary<string, object> { ["corridaId"] = corrida.Id, ["empresaId"] = corrida.EmpresaId });

    /// <summary>Carga las corridas del período elegido, sin el indicador de ocupado.</summary>
    /// <returns>Tarea que finaliza al cargarlas.</returns>
    private async Task CargarCorridasInternoAsync()
        => Corridas = Periodo is { } periodo ? await _api.ListarCorridasAsync(periodo.Id, EmpresaDe(periodo)) : [];

    /// <summary>Obtiene la empresa que se envía a la API para un período.</summary>
    /// <param name="periodo">Período.</param>
    /// <returns>La empresa del período.</returns>
    private Guid? EmpresaDe(PeriodoDto periodo) => periodo.EmpresaId;

    /// <summary>
    /// Refresca el texto y el aviso del cálculo, y habilita o no el botón,
    /// cuando cambian el período, sus corridas o el idioma.
    /// </summary>
    private void ActualizarEstadoDelCalculo()
    {
        OnPropertyChanged(nameof(TextoDeCalculo));
        OnPropertyChanged(nameof(AvisoDeCalculo));
        CalcularCommand.NotifyCanExecuteChanged();
    }

    /// <inheritdoc/>
    protected override void AlCambiarIdioma() => ActualizarEstadoDelCalculo();

    /// <summary>Refresca el estado del cálculo al cambiar las corridas.</summary>
    /// <param name="value">Corridas nuevas.</param>
    partial void OnCorridasChanged(IReadOnlyList<CorridaDeNominaDto> value) => ActualizarEstadoDelCalculo();

    /// <summary>Carga las corridas del período elegido por el usuario.</summary>
    /// <param name="value">Período elegido.</param>
    partial void OnPeriodoChanged(PeriodoDto? value)
    {
        OnPropertyChanged(nameof(IndiceDePeriodo));
        ActualizarEstadoDelCalculo();

        if (!_cambiandoPeriodos)
        {
            CargarCorridasCommand.Execute(null);
        }
    }
}
