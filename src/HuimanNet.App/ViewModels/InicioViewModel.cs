using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HuimanNet.App.Localizacion;
using HuimanNet.App.Services;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Enums;

namespace HuimanNet.App.ViewModels;

/// <summary>
/// Inicio: saludo, indicadores, pendientes y empresa de trabajo.
/// </summary>
public sealed partial class InicioViewModel : ViewModelBase
{
    private readonly IServicioDeApi _api;
    private readonly SesionDeLaApp _sesion;
    private readonly IServicioDeNavegacion _navegacion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="InicioViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    /// <param name="navegacion">Navegación.</param>
    public InicioViewModel(Traductor traductor, IServicioDeApi api, SesionDeLaApp sesion, IServicioDeNavegacion navegacion)
        : base(traductor)
    {
        _api = api;
        _sesion = sesion;
        _navegacion = navegacion;
        Titulo = traductor["inicio.titulo"];
    }

    /// <summary>Obtiene los indicadores.</summary>
    /// <value>Empleados, períodos, documentos y corridas por cotejar.</value>
    public ObservableCollection<Indicador> Indicadores { get; } = [];

    /// <summary>Obtiene los pendientes del usuario.</summary>
    /// <value>Tareas sugeridas por el servidor.</value>
    public ObservableCollection<PendienteDto> Pendientes { get; } = [];

    /// <summary>Obtiene las empresas elegibles.</summary>
    /// <value>Todas las activas para nómina y administración; las suyas para la empresa cliente.</value>
    public IReadOnlyList<EmpresaDto> Empresas => _sesion.Empresas;

    /// <summary>Indica si el usuario elige la empresa de trabajo.</summary>
    /// <value><c>true</c> para mostrar el selector de empresa.</value>
    public bool EligeEmpresa => _sesion.EligeEmpresa;

    /// <summary>Obtiene los nombres de las empresas para el selector.</summary>
    /// <value>Razones sociales en el mismo orden que <see cref="Empresas"/>.</value>
    public IReadOnlyList<string> NombresDeEmpresas => [.. _sesion.Empresas.Select(e => e.RazonSocial)];

    /// <summary>Obtiene o establece la posición de la empresa elegida.</summary>
    /// <value>-1 si no hay empresa.</value>
    public int IndiceDeEmpresa
    {
        get => EmpresaSeleccionada is null ? -1 : _sesion.Empresas.ToList().FindIndex(e => e.Id == EmpresaSeleccionada.Id);
        set
        {
            if (value >= 0 && value < _sesion.Empresas.Count)
            {
                EmpresaSeleccionada = _sesion.Empresas[value];
            }
        }
    }

    /// <summary>Obtiene o establece la empresa de trabajo.</summary>
    /// <value>Empresa elegida.</value>
    [ObservableProperty]
    public partial EmpresaDto? EmpresaSeleccionada { get; set; }

    /// <summary>Obtiene o establece el saludo.</summary>
    /// <value>Texto traducido con el nombre.</value>
    [ObservableProperty]
    public partial string Saludo { get; set; } = string.Empty;

    /// <summary>Obtiene o establece si no hay pendientes.</summary>
    /// <value><c>true</c> para mostrar el estado vacío.</value>
    [ObservableProperty]
    public partial bool SinPendientes { get; set; }

    /// <summary>
    /// Carga indicadores y pendientes.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        if (_sesion.Usuario is not { } usuario)
        {
            await _navegacion.IrAAsync(AppShell.RutaSesion);
            return;
        }

        Titulo = T["inicio.titulo"];
        Saludo = T.Formato("inicio.saludo", usuario.NombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty);
        OnPropertyChanged(nameof(Empresas));
        OnPropertyChanged(nameof(NombresDeEmpresas));
        OnPropertyChanged(nameof(EligeEmpresa));
        EmpresaSeleccionada = _sesion.Empresa;

        // Nómina y administración ven el resumen de todas las empresas; la
        // empresa cliente, el de la empresa elegida (o su principal).
        ResumenDeInicioDto resumen = await _api.ObtenerResumenDeInicioAsync(_sesion.EsTransversal ? null : _sesion.EmpresaDeConsulta);

        Reemplazar(Indicadores,
        [
            new Indicador(T["inicio.empleadosActivos"], resumen.EmpleadosActivos),
            new Indicador(T["inicio.periodosAbiertos"], resumen.PeriodosAbiertos),
            new Indicador(T["inicio.documentosDisponibles"], resumen.DocumentosDisponibles),
            new Indicador(T["inicio.corridasPorCotejar"], resumen.CorridasPorCotejar),
        ]);

        Reemplazar(Pendientes, resumen.Pendientes);
        SinPendientes = Pendientes.Count == 0;
    });

    /// <summary>
    /// Abre la pantalla que resuelve un pendiente: la corrida para un cotejo;
    /// la lista de períodos en los demás casos.
    /// </summary>
    /// <param name="pendiente">Pendiente elegido.</param>
    /// <returns>Una tarea que representa la navegación.</returns>
    [RelayCommand]
    public Task AbrirPendienteAsync(PendienteDto? pendiente) => pendiente switch
    {
        null => Task.CompletedTask,
        { Tipo: TipoDePendiente.CotejarCorrida, CorridaId: { } corridaId } => _navegacion.IrAAsync(
            AppShell.RutaCorrida, new Dictionary<string, object> { ["corridaId"] = corridaId, ["empresaId"] = pendiente.EmpresaId }),
        _ => _navegacion.IrAAsync(AppShell.RutaPeriodos),
    };

    /// <inheritdoc/>
    protected override void AlCambiarIdioma() => CargarCommand.Execute(null);

    /// <summary>Aplica la empresa elegida a la sesión y actualiza lo que depende de ella.</summary>
    /// <param name="value">Empresa elegida.</param>
    partial void OnEmpresaSeleccionadaChanged(EmpresaDto? value)
    {
        _sesion.SeleccionarEmpresa(value);
        OnPropertyChanged(nameof(IndiceDeEmpresa));

        // El resumen de la empresa cliente depende de la empresa elegida.
        if (!_sesion.EsTransversal)
        {
            CargarCommand.Execute(null);
        }
    }
}
