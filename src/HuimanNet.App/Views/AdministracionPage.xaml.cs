using HuimanNet.App.ViewModels;

namespace HuimanNet.App.Views;

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
