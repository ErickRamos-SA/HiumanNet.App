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
/// Períodos de la empresa y acceso a sus documentos.
/// </summary>
public sealed partial class PeriodosViewModel : ViewModelBase
{
    private readonly IServicioDeApi _api;
    private readonly SesionDeLaApp _sesion;
    private readonly IServicioDeNavegacion _navegacion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="PeriodosViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    /// <param name="navegacion">Navegación.</param>
    public PeriodosViewModel(Traductor traductor, IServicioDeApi api, SesionDeLaApp sesion, IServicioDeNavegacion navegacion)
        : base(traductor)
    {
        _api = api;
        _sesion = sesion;
        _navegacion = navegacion;
        Titulo = traductor["periodos.titulo"];
    }

    /// <summary>Obtiene los períodos.</summary>
    /// <value>Del más reciente al más antiguo.</value>
    public ObservableCollection<PeriodoDto> Periodos { get; } = [];

    /// <summary>Obtiene o establece si se incluyen los cerrados.</summary>
    /// <value><c>false</c> por defecto.</value>
    [ObservableProperty]
    public partial bool IncluirCerrados { get; set; }

    /// <summary>
    /// Carga los períodos de la empresa de trabajo.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        Titulo = T["periodos.titulo"];

        if (_sesion.FaltaEmpresa)
        {
            Periodos.Clear();
            MensajeDeError = T["comun.seleccioneEmpresaDetalle"];
            return;
        }

        Reemplazar(Periodos, await _api.ListarPeriodosAsync(_sesion.EmpresaDeConsulta, IncluirCerrados));
    });

    /// <summary>
    /// Abre los documentos de un período.
    /// </summary>
    /// <param name="periodo">Período elegido.</param>
    /// <returns>Una tarea que representa la navegación.</returns>
    [RelayCommand]
    public Task AbrirPeriodoAsync(PeriodoDto? periodo)
        => periodo is null
            ? Task.CompletedTask
            : _navegacion.IrAAsync(AppShell.RutaDocumentos, new Dictionary<string, object> { ["periodo"] = periodo });

    /// <summary>Recarga los períodos al mostrar u ocultar los cerrados.</summary>
    /// <param name="value">Si se incluyen los cerrados.</param>
    partial void OnIncluirCerradosChanged(bool value) => CargarCommand.Execute(null);
}
