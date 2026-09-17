using HuimanNet.App.ViewModels;

namespace HuimanNet.App.Views;

/// <summary>Pantalla de documentos de un período.</summary>
public partial class DocumentosPage : ContentPage
{
    private readonly DocumentosViewModel _vm;

    /// <summary>Inicializa una nueva instancia de <see cref="DocumentosPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public DocumentosPage(DocumentosViewModel viewModel)
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
