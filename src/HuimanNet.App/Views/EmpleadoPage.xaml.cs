using HuimanNet.App.ViewModels;

namespace HuimanNet.App.Views;

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
