namespace HuimanNet.App;

/// <summary>
/// Punto de entrada de la aplicación MAUI.
/// </summary>
public partial class App : Application
{
    private readonly AppShell _shell;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="App"/>.
    /// </summary>
    /// <param name="shell">Shell de navegación resuelto por el contenedor.</param>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="shell"/> es <c>null</c>.
    /// </exception>
    public App(AppShell shell)
    {
        ArgumentNullException.ThrowIfNull(shell);

        InitializeComponent();
        _shell = shell;
    }

    /// <inheritdoc/>
    protected override Window CreateWindow(IActivationState? activationState)
        => new(_shell) { Title = "HuimanNet" };
}
