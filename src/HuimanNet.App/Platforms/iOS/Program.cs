using ObjCRuntime;
using UIKit;

namespace HuimanNet.App;

/// <summary>
/// Punto de entrada nativo de la aplicación en iOS.
/// </summary>
public static class Program
{
    /// <summary>
    /// Arranca la aplicación con el delegado de MAUI.
    /// </summary>
    /// <param name="args">Argumentos de línea de comandos del sistema.</param>
    public static void Main(string[] args)
        => UIApplication.Main(args, null, typeof(AppDelegate));
}
