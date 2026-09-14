using HuimanNet.App.ViewModels;

namespace HuimanNet.App.Views;

/// <summary>Pantalla de inicio de sesión.</summary>
public partial class SesionPage : ContentPage
{
    private readonly SesionViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="SesionPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public SesionPage(SesionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.ComprobarSesionCommand.Execute(null);
    }
}

/// <summary>Pantalla de inicio.</summary>
public partial class InicioPage : ContentPage
{
    private readonly InicioViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="InicioPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public InicioPage(InicioViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.CargarCommand.Execute(null);
    }
}

/// <summary>Pantalla de períodos.</summary>
public partial class PeriodosPage : ContentPage
{
    private readonly PeriodosViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="PeriodosPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public PeriodosPage(PeriodosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.CargarCommand.Execute(null);
    }
}

/// <summary>Pantalla de documentos de un período.</summary>
public partial class DocumentosPage : ContentPage
{
    private readonly DocumentosViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="DocumentosPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public DocumentosPage(DocumentosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.CargarCommand.Execute(null);
    }
}

/// <summary>Pantalla de incidencias.</summary>
public partial class IncidenciasPage : ContentPage
{
    private readonly IncidenciasViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="IncidenciasPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public IncidenciasPage(IncidenciasViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.CargarCommand.Execute(null);
    }
}

/// <summary>Pantalla de captura de una incidencia.</summary>
public partial class IncidenciaPage : ContentPage
{
    /// <summary>Inicializa una nueva instancia de <see cref="IncidenciaPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public IncidenciaPage(IncidenciaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

/// <summary>Pantalla de nómina.</summary>
public partial class NominaPage : ContentPage
{
    private readonly NominaViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="NominaPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public NominaPage(NominaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.CargarCommand.Execute(null);
    }
}

/// <summary>Pantalla de una corrida.</summary>
public partial class CorridaPage : ContentPage
{
    private readonly CorridaViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="CorridaPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public CorridaPage(CorridaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.CargarCommand.Execute(null);
    }
}

/// <summary>Pantalla del detalle de un trabajador.</summary>
public partial class ResultadoPage : ContentPage
{
    private readonly ResultadoViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="ResultadoPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public ResultadoPage(ResultadoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.CargarCommand.Execute(null);
    }
}

/// <summary>Pantalla de empleados.</summary>
public partial class EmpleadosPage : ContentPage
{
    private readonly EmpleadosViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="EmpleadosPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public EmpleadosPage(EmpleadosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_vm.Empleados.Count == 0)
        {
            _vm.BuscarCommand.Execute(null);
        }
    }
}

/// <summary>Pantalla de la ficha de un empleado.</summary>
public partial class EmpleadoPage : ContentPage
{
    private readonly EmpleadoViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="EmpleadoPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public EmpleadoPage(EmpleadoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.CargarCommand.Execute(null);
    }
}

/// <summary>Pantalla de explicación de cálculos.</summary>
public partial class ExplicacionPage : ContentPage
{
    private readonly ExplicacionViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="ExplicacionPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public ExplicacionPage(ExplicacionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.CargarCommand.Execute(null);
    }
}

/// <summary>Pantalla de administración.</summary>
public partial class AdministracionPage : ContentPage
{
    private readonly AdministracionViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="AdministracionPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public AdministracionPage(AdministracionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.CargarCommand.Execute(null);
    }
}

/// <summary>Menú «Más».</summary>
public partial class MasPage : ContentPage
{
    private readonly MasViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="MasPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public MasPage(MasViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.CargarCommand.Execute(null);
    }
}

/// <summary>Pantalla de la cuenta.</summary>
public partial class CuentaPage : ContentPage
{
    private readonly CuentaViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="CuentaPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public CuentaPage(CuentaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.CargarCommand.Execute(null);
    }
}
