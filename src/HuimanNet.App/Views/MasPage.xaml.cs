using HuimanNet.App.ViewModels;

namespace HuimanNet.App.Views;

/// <summary>Menú «Más».</summary>
public partial class MasPage : ContentPage
{
    private readonly MasViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="MasPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public MasPage(MasViewModel viewModel)
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
