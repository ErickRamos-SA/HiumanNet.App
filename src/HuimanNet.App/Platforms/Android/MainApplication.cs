using Android.App;
using Android.Runtime;

namespace HuimanNet.App;

/// <summary>
/// Aplicación Android de HuimanNet.
/// </summary>
[Application]
public class MainApplication : MauiApplication
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="MainApplication"/>.
    /// </summary>
    /// <param name="handle">Identificador nativo proporcionado por el entorno.</param>
    /// <param name="ownership">Modo de propiedad del identificador nativo.</param>
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    /// <inheritdoc/>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
