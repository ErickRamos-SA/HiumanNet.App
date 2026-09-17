using HuimanNet.App.ViewModels;

namespace HuimanNet.App.Views;

/// <summary>Pantalla de captura de una incidencia.</summary>
public partial class IncidenciaPage : ContentPage
{
    /// <summary>Inicializa una nueva instancia de <see cref="IncidenciaPage"/>.</summary>
    /// <param name="viewModel">ViewModel de la pantalla.</param>
    public IncidenciaPage(IncidenciaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
