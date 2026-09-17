using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HuimanNet.App.Services;
using HuimanNet.Contracts.Catalogos;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empleados;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Enums;

namespace HuimanNet.App.ViewModels;

/// <summary>
/// Empleados de la empresa, con búsqueda y paginación incremental.
/// </summary>
public sealed partial class EmpleadosViewModel : ViewModelBase
{
    private readonly IServicioDeApi _api;
    private readonly SesionDeLaApp _sesion;
    private readonly IServicioDeNavegacion _navegacion;
    private int _pagina = 1;
    private int _indiceDeEmpresa;
    private bool _preparandoFiltro;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="EmpleadosViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    /// <param name="navegacion">Navegación.</param>
    public EmpleadosViewModel(Traductor traductor, IServicioDeApi api, SesionDeLaApp sesion, IServicioDeNavegacion navegacion)
        : base(traductor)
    {
        _api = api;
        _sesion = sesion;
        _navegacion = navegacion;
        Titulo = traductor["empleados.titulo"];
    }

    /// <summary>Obtiene los empleados cargados.</summary>
    /// <value>Crece al pedir más páginas.</value>
    public ObservableCollection<EmpleadoResumenDto> Empleados { get; } = [];

    /// <summary>Indica si el usuario trabaja sobre varias empresas y puede filtrar por empresa.</summary>
    /// <value><c>true</c> para operador de nómina y administrador.</value>
    public bool EligeEmpresa => _sesion.EligeEmpresa;

    /// <summary>Obtiene las opciones del filtro de empresa.</summary>
    /// <value>«Todas las empresas» seguida de cada empresa.</value>
    public ObservableCollection<string> NombresDeEmpresas { get; } = [];

    /// <summary>Obtiene o establece la posición elegida en el filtro de empresa.</summary>
    /// <value>0 para todas las empresas.</value>
    public int IndiceDeEmpresa
    {
        get => _indiceDeEmpresa;
        set
        {
            // El selector informa -1 mientras se reemplazan sus opciones.
            if (value < 0 || value == _indiceDeEmpresa)
            {
                return;
            }

            _indiceDeEmpresa = value;
            OnPropertyChanged();

            if (!_preparandoFiltro)
            {
                BuscarCommand.Execute(null);
            }
        }
    }

    /// <summary>Obtiene o establece el texto de búsqueda.</summary>
    /// <value>Clave o nombre.</value>
    [ObservableProperty]
    public partial string? Texto { get; set; }

    /// <summary>Obtiene o establece si hay más páginas.</summary>
    /// <value><c>true</c> para ofrecer «cargar más».</value>
    [ObservableProperty]
    public partial bool HayMas { get; set; }

    /// <summary>Obtiene o establece el total encontrado.</summary>
    /// <value>Texto traducido.</value>
    [ObservableProperty]
    public partial string? Total { get; set; }

    /// <summary>
    /// Busca desde la primera página.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task BuscarAsync() => EjecutarAsync(async () =>
    {
        PrepararFiltroDeEmpresa();
        _pagina = 1;
        Empleados.Clear();
        await CargarPaginaAsync();
    });

    /// <summary>
    /// Agrega la siguiente página.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarMasAsync() => !HayMas ? Task.CompletedTask : EjecutarAsync(async () =>
    {
        _pagina++;
        await CargarPaginaAsync();
    });

    /// <summary>
    /// Abre la ficha de un empleado.
    /// </summary>
    /// <param name="empleado">Empleado elegido.</param>
    /// <returns>Una tarea que representa la navegación.</returns>
    [RelayCommand]
    public Task AbrirAsync(EmpleadoResumenDto? empleado)
        => empleado is null
            ? Task.CompletedTask
            : _navegacion.IrAAsync(
                AppShell.RutaEmpleado,
                new Dictionary<string, object> { ["empleadoId"] = empleado.Id, ["empresaId"] = empleado.EmpresaId });

    /// <inheritdoc/>
    protected override void AlCambiarIdioma()
    {
        if (NombresDeEmpresas.Count > 0)
        {
            _preparandoFiltro = true;
            NombresDeEmpresas[0] = TextoDeTodas;
            OnPropertyChanged(nameof(IndiceDeEmpresa));
            _preparandoFiltro = false;
        }
    }

    /// <summary>
    /// Empresa del filtro; <c>null</c> para todas: todas las empresas para
    /// nómina y administración, todas las suyas para la empresa cliente.
    /// </summary>
    private Guid? EmpresaFiltrada => _sesion.EligeEmpresa && _indiceDeEmpresa > 0 && _indiceDeEmpresa <= _sesion.Empresas.Count
        ? _sesion.Empresas[_indiceDeEmpresa - 1].Id
        : null;

    /// <summary>Texto de la primera opción del filtro de empresa.</summary>
    private string TextoDeTodas => _sesion.EsTransversal ? T["empleados.todasLasEmpresas"] : T["empleados.todasMisEmpresas"];

    /// <summary>
    /// Llena el filtro de empresa la primera vez y lo sitúa en la empresa de trabajo.
    /// </summary>
    private void PrepararFiltroDeEmpresa()
    {
        if (!_sesion.EligeEmpresa || NombresDeEmpresas.Count == _sesion.Empresas.Count + 1)
        {
            return;
        }

        _preparandoFiltro = true;

        var nombres = new List<string> { TextoDeTodas };
        nombres.AddRange(_sesion.Empresas.Select(e => e.RazonSocial));
        Reemplazar(NombresDeEmpresas, nombres);

        int posicion = _sesion.Empresa is { } empresa
            ? _sesion.Empresas.Select(e => e.Id).ToList().IndexOf(empresa.Id) + 1
            : 0;

        _indiceDeEmpresa = Math.Max(0, posicion);
        OnPropertyChanged(nameof(IndiceDeEmpresa));
        OnPropertyChanged(nameof(EligeEmpresa));
        _preparandoFiltro = false;
    }

    /// <summary>Pide la página actual y la añade a la lista.</summary>
    /// <returns>Tarea que finaliza al añadir la página.</returns>
    private async Task CargarPaginaAsync()
    {
        PaginaDto<EmpleadoResumenDto> pagina = await _api.ListarEmpleadosAsync(EmpresaFiltrada, Texto, _pagina, Paginacion.TamanoDeEmpleados);

        foreach (EmpleadoResumenDto empleado in pagina.Elementos)
        {
            Empleados.Add(empleado);
        }

        HayMas = pagina.Pagina < pagina.TotalPaginas;
        Total = T.Formato("empleados.total", pagina.TotalElementos.ToString("N0", T.Cultura));
    }
}
