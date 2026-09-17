using HuimanNet.App.ViewModels;

namespace HuimanNet.App.Views;

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
