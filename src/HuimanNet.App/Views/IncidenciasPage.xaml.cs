using HuimanNet.App.ViewModels;

namespace HuimanNet.App.Views;

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
