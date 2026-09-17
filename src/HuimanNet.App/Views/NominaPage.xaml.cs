using HuimanNet.App.ViewModels;

namespace HuimanNet.App.Views;

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
