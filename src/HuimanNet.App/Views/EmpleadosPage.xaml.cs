using HuimanNet.App.ViewModels;

namespace HuimanNet.App.Views;

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
