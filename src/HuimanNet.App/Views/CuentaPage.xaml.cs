using HuimanNet.App.ViewModels;

namespace HuimanNet.App.Views;

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
