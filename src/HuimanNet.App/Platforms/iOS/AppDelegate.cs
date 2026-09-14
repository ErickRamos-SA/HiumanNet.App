using Foundation;

namespace HuimanNet.App;

/// <summary>
/// Delegado de la aplicación en iOS.
/// </summary>
[Register(nameof(AppDelegate))]
public class AppDelegate : MauiUIApplicationDelegate
{
    /// <inheritdoc/>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
