using HuimanNet.Application.Interfaces;

namespace HuimanNet.Web.Services;

/// <summary>
/// Portador del usuario del circuito dentro de un ámbito de operación.
/// </summary>
/// <remarks>
/// Cada operación de la web se ejecuta en un ámbito de DI propio (ver
/// <see cref="EjecutorDeCasosDeUso"/>). Ese ámbito no conoce el circuito, así
/// que el ejecutor deposita aquí la identidad ya resuelta antes de construir
/// el manejador; el registro de <see cref="IUsuarioActual"/> la toma de aquí.
/// </remarks>
public sealed class PortadorDeUsuarioActual
{
    /// <summary>Obtiene o establece el usuario del circuito que origina la operación.</summary>
    /// <value><c>null</c> en el ámbito del propio circuito.</value>
    public IUsuarioActual? Usuario { get; set; }
}
