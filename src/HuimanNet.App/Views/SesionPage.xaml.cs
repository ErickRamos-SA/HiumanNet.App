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
