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
    private const int TamanoPagina = 30;

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

    private async Task CargarPaginaAsync()
    {
        PaginaDto<EmpleadoResumenDto> pagina = await _api.ListarEmpleadosAsync(EmpresaFiltrada, Texto, _pagina, TamanoPagina);

        foreach (EmpleadoResumenDto empleado in pagina.Elementos)
        {
            Empleados.Add(empleado);
        }

        HayMas = pagina.Pagina < pagina.TotalPaginas;
        Total = T.Formato("empleados.total", pagina.TotalElementos.ToString("N0", T.Cultura));
    }
}

/// <summary>
/// Ficha de un empleado con sus contratos (esquemas y razones sociales).
/// </summary>
public sealed partial class EmpleadoViewModel : ViewModelBase, IQueryAttributable
{
    private readonly IServicioDeApi _api;
    private readonly SesionDeLaApp _sesion;
    private Guid _empleadoId;
    private Guid? _empresaId;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="EmpleadoViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    public EmpleadoViewModel(Traductor traductor, IServicioDeApi api, SesionDeLaApp sesion)
        : base(traductor)
    {
        _api = api;
        _sesion = sesion;
    }

    /// <summary>Obtiene o establece el empleado.</summary>
    /// <value><c>null</c> hasta cargar.</value>
    [ObservableProperty]
    public partial EmpleadoDto? Empleado { get; set; }

    /// <inheritdoc/>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.TryGetValue("empleadoId", out object? valor) && valor is Guid empleadoId)
        {
            _empleadoId = empleadoId;
        }

        // Empresa a la que pertenece el empleado: la lista puede mostrar varias.
        if (query.TryGetValue("empresaId", out object? empresa) && empresa is Guid empresaId)
        {
            _empresaId = empresaId;
        }
    }

    /// <summary>
    /// Carga la ficha.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        Empleado = await _api.ObtenerEmpleadoAsync(_empleadoId, _sesion.EligeEmpresa ? _empresaId ?? _sesion.EmpresaDeConsulta : null);
        Titulo = Empleado.NombreCompleto;
    });
}

/// <summary>
/// Explicación de los cálculos de cada esquema de pago, con los valores vigentes.
/// </summary>
public sealed partial class ExplicacionViewModel : ViewModelBase
{
    private readonly IServicioDeApi _api;
    private readonly SesionDeLaApp _sesion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ExplicacionViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    public ExplicacionViewModel(Traductor traductor, IServicioDeApi api, SesionDeLaApp sesion)
        : base(traductor)
    {
        _api = api;
        _sesion = sesion;
        Titulo = traductor["explicacion.titulo"];
    }

    /// <summary>Obtiene los esquemas de pago.</summary>
    /// <value>IMSS, sindicato y honorarios.</value>
    public IReadOnlyList<EsquemaDePago> Esquemas { get; } = [EsquemaDePago.Imss, EsquemaDePago.Sindicato, EsquemaDePago.Honorarios];

    /// <summary>Obtiene los nombres traducidos de los esquemas.</summary>
    /// <value>En el mismo orden que <see cref="Esquemas"/>.</value>
    public IReadOnlyList<string> NombresDeEsquemas => [.. Esquemas.Select(e => T.Enumerado(e))];

    /// <summary>Obtiene o establece la posición del esquema elegido.</summary>
    /// <value>0 IMSS, 1 sindicato, 2 honorarios.</value>
    public int IndiceDeEsquema
    {
        get => Esquemas.ToList().IndexOf(Esquema);
        set
        {
            if (value >= 0 && value < Esquemas.Count)
            {
                Esquema = Esquemas[value];
            }
        }
    }

    /// <summary>Obtiene o establece el esquema elegido.</summary>
    /// <value>IMSS por defecto.</value>
    [ObservableProperty]
    public partial EsquemaDePago Esquema { get; set; } = EsquemaDePago.Imss;

    /// <summary>Obtiene o establece las secciones narrativas.</summary>
    /// <value>En el idioma de la interfaz.</value>
    [ObservableProperty]
    public partial IReadOnlyList<ExplicacionDeCalculoDto> Secciones { get; set; } = [];

    /// <summary>Obtiene o establece los conceptos con su fórmula.</summary>
    /// <value>En orden de presentación.</value>
    [ObservableProperty]
    public partial IReadOnlyList<ConceptoDeNominaDto> Conceptos { get; set; } = [];

    /// <summary>Obtiene o establece el aviso de catálogo incoherente.</summary>
    /// <value><c>null</c> si el catálogo es válido.</value>
    [ObservableProperty]
    public partial string? AvisoDeCatalogo { get; set; }

    /// <summary>
    /// Carga la explicación del esquema elegido.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        Titulo = T["explicacion.titulo"];
        ExplicacionCompletaDto explicacion = await _api.ObtenerExplicacionAsync(Esquema, T.Idioma, _sesion.EmpresaDeConsulta);

        Secciones = [.. explicacion.Secciones.OrderBy(s => s.Orden)];
        Conceptos = [.. explicacion.Conceptos.OrderBy(c => c.Orden)];
        AvisoDeCatalogo = explicacion.ErrorDeCatalogo is null ? null : $"{T["explicacion.errorDeCatalogo"]} {explicacion.ErrorDeCatalogo}";
    });

    /// <inheritdoc/>
    protected override void AlCambiarIdioma()
    {
        OnPropertyChanged(nameof(NombresDeEsquemas));
        CargarCommand.Execute(null);
    }

    partial void OnEsquemaChanged(EsquemaDePago value)
    {
        OnPropertyChanged(nameof(IndiceDeEsquema));
        CargarCommand.Execute(null);
    }
}

/// <summary>
/// Administración desde el móvil: consulta de usuarios y prueba de fórmulas.
/// </summary>
/// <remarks>
/// La edición de catálogos y usuarios, que implica formularios extensos, se
/// hace desde el portal web; aquí se ofrece lo que tiene sentido en campo:
/// ver quién tiene acceso y comprobar una fórmula con el catálogo vigente.
/// </remarks>
public sealed partial class AdministracionViewModel : ViewModelBase
{
    private readonly IServicioDeApi _api;
    private readonly SesionDeLaApp _sesion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AdministracionViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    public AdministracionViewModel(Traductor traductor, IServicioDeApi api, SesionDeLaApp sesion)
        : base(traductor)
    {
        _api = api;
        _sesion = sesion;
        Titulo = traductor["movil.administracion"];
    }

    /// <summary>Indica si puede consultar usuarios.</summary>
    /// <value><c>true</c> con el permiso de administrar usuarios.</value>
    public bool PuedeUsuarios => _sesion.Puede(AccionDelSistema.AdministrarUsuarios);

    /// <summary>Indica si puede probar fórmulas.</summary>
    /// <value><c>true</c> con el permiso de administrar catálogos.</value>
    public bool PuedeCatalogos => _sesion.Puede(AccionDelSistema.AdministrarCatalogosDeCalculo);

    /// <summary>Obtiene o establece los usuarios.</summary>
    /// <value>Ordenados por nombre.</value>
    [ObservableProperty]
    public partial IReadOnlyList<UsuarioDto> Usuarios { get; set; } = [];

    /// <summary>Obtiene los esquemas para la prueba de fórmulas.</summary>
    /// <value>IMSS, sindicato y honorarios.</value>
    public IReadOnlyList<EsquemaDePago> Esquemas { get; } = [EsquemaDePago.Imss, EsquemaDePago.Sindicato, EsquemaDePago.Honorarios];

    /// <summary>Obtiene los nombres traducidos de los esquemas.</summary>
    /// <value>En el mismo orden que <see cref="Esquemas"/>.</value>
    public IReadOnlyList<string> NombresDeEsquemas => [.. Esquemas.Select(e => T.Enumerado(e))];

    /// <summary>Obtiene o establece la posición del esquema elegido.</summary>
    /// <value>0 IMSS, 1 sindicato, 2 honorarios.</value>
    public int IndiceDeEsquemaDePrueba
    {
        get => Esquemas.ToList().IndexOf(EsquemaDePrueba);
        set
        {
            if (value >= 0 && value < Esquemas.Count)
            {
                EsquemaDePrueba = Esquemas[value];
            }
        }
    }

    /// <summary>Obtiene o establece el esquema de la prueba.</summary>
    /// <value>IMSS por defecto.</value>
    [ObservableProperty]
    public partial EsquemaDePago EsquemaDePrueba { get; set; } = EsquemaDePago.Imss;

    /// <summary>Obtiene o establece la fórmula a probar.</summary>
    /// <value>Texto en el lenguaje de fórmulas.</value>
    [ObservableProperty]
    public partial string Formula { get; set; } = "REDONDEAR(SUELDO_PERIODO_REAL * 1.16; 2)";

    /// <summary>Obtiene o establece el resultado de la prueba.</summary>
    /// <value>Valor, o el error de la fórmula.</value>
    [ObservableProperty]
    public partial string? ResultadoDePrueba { get; set; }

    /// <summary>
    /// Carga los usuarios.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        Titulo = T["movil.administracion"];
        OnPropertyChanged(nameof(PuedeUsuarios));
        OnPropertyChanged(nameof(PuedeCatalogos));

        Usuarios = PuedeUsuarios ? await _api.ListarUsuariosAsync(_sesion.EmpresaDeConsulta, incluirInactivos: true) : [];
    });

    /// <summary>
    /// Evalúa la fórmula con el catálogo vigente.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task ProbarAsync() => EjecutarAsync(async () =>
    {
        ProbarFormulaResponse respuesta = await _api.ProbarFormulaAsync(
            new ProbarFormulaRequest(Formula, EsquemaDePrueba, _sesion.EmpresaDeConsulta, []));

        ResultadoDePrueba = respuesta.Valida
            ? $"{T["catalogos.resultado"]}: {respuesta.Resultado?.ToString("N6", T.Cultura) ?? "—"}"
              + (respuesta.Referencias.Count > 0 ? $"\n{T["catalogos.referencias"]}: {string.Join(", ", respuesta.Referencias)}" : string.Empty)
            : respuesta.Error;
    });
}
