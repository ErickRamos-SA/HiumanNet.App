namespace HuimanNet.Web.Services;

/// <summary>
/// Estado de navegación compartido por las páginas de un circuito.
/// </summary>
/// <remarks>
/// Guarda la empresa sobre la que trabaja un usuario transversal (operador o
/// administrador), elegida una sola vez en la cabecera, como el selector de
/// espacio de trabajo de otras aplicaciones. Las páginas se suscriben a
/// <see cref="EmpresaCambiada"/> para recargar sus datos. Un usuario de empresa
/// cliente no lo usa: su empresa la impone el servidor.
/// </remarks>
public sealed class EstadoDelPortal
{
    /// <summary>
    /// Se produce cuando cambia la empresa seleccionada.
    /// </summary>
    public event Func<Task>? EmpresaCambiada;

    /// <summary>
    /// Se produce cuando se crea o modifica una empresa y las listas deben recargarse.
    /// </summary>
    public event Func<Task>? EmpresasModificadas;

    /// <summary>Obtiene la empresa seleccionada.</summary>
    /// <value>Identificador, o <c>null</c> si no se ha elegido ninguna.</value>
    public Guid? EmpresaId { get; private set; }

    /// <summary>Obtiene la razón social de la empresa seleccionada.</summary>
    /// <value>Nombre para mostrar, o <c>null</c>.</value>
    public string? EmpresaNombre { get; private set; }

    /// <summary>
    /// Selecciona una empresa y notifica a los suscriptores.
    /// </summary>
    /// <param name="empresaId">Empresa elegida, o <c>null</c>.</param>
    /// <param name="nombre">Razón social para mostrar.</param>
    /// <returns>Una tarea que termina cuando todos los suscriptores recargaron.</returns>
    public async Task SeleccionarEmpresaAsync(Guid? empresaId, string? nombre)
    {
        if (empresaId == EmpresaId)
        {
            return;
        }

        EmpresaId = empresaId;
        EmpresaNombre = nombre;
        await NotificarAsync(EmpresaCambiada);
    }

    /// <summary>
    /// Avisa que el catálogo de empresas cambió.
    /// </summary>
    /// <returns>Una tarea que termina cuando todos los suscriptores recargaron.</returns>
    public Task NotificarEmpresasModificadasAsync() => NotificarAsync(EmpresasModificadas);

    /// <summary>
    /// Invoca a los suscriptores de un evento de uno en uno, porque las páginas
    /// comparten la sesión SQL del circuito.
    /// </summary>
    /// <param name="evento">Evento a notificar, o <c>null</c> si no tiene suscriptores.</param>
    /// <returns>Tarea que finaliza cuando todos terminaron.</returns>
    private static async Task NotificarAsync(Func<Task>? evento)
    {
        if (evento is null)
        {
            return;
        }

        // Secuencial a propósito: las páginas comparten la sesión SQL del circuito.
        foreach (Func<Task> suscriptor in evento.GetInvocationList().Cast<Func<Task>>())
        {
            await suscriptor();
        }
    }
}
