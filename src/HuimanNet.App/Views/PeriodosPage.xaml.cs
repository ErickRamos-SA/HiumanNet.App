using HuimanNet.App.ViewModels;

namespace HuimanNet.App.Views;

/// <summary>Pantalla de períodos.</summary>
public partial class PeriodosPage : ContentPage
{
    private readonly PeriodosViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="PeriodosPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public PeriodosPage(PeriodosViewModel viewModel)
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
