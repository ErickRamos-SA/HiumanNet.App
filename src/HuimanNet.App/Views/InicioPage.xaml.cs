using HuimanNet.App.ViewModels;

namespace HuimanNet.App.Views;

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
