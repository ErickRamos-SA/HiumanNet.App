using HuimanNet.App.ViewModels;

namespace HuimanNet.App.Views;

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
