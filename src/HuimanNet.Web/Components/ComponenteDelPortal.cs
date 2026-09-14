using HuimanNet.Application.Common;
using HuimanNet.Application.Interfaces;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Web.Seguridad;
using HuimanNet.Web.Services;
using Microsoft.AspNetCore.Components;

namespace HuimanNet.Web.Components;

/// <summary>
/// Base de las páginas del portal: resuelve la identidad del circuito, expone
/// los textos traducidos, la empresa de trabajo y un envoltorio uniforme de errores.
/// </summary>
/// <remarks>
/// Ninguna página debe acceder a datos antes de que la identidad esté resuelta:
/// <see cref="CargarDatosAsync"/> sólo se invoca después. Las páginas que
/// dependen de la empresa elegida en la cabecera sobrescriben
/// <see cref="DependeDeEmpresa"/> y se recargan solas cuando cambia.
/// </remarks>
public abstract class ComponenteDelPortal : ComponentBase, IDisposable
{
    private bool _liberado;

    /// <summary>Obtiene o establece la identidad efectiva del circuito.</summary>
    /// <value>Inyectada por el contenedor.</value>
    [Inject]
    protected ContextoDeUsuarioDelCircuito ContextoDeUsuario { get; set; } = default!;

    /// <summary>Obtiene o establece el traductor del circuito.</summary>
    /// <value>Inyectado por el contenedor.</value>
    [Inject]
    protected Traductor T { get; set; } = default!;

    /// <summary>Obtiene o establece el estado compartido del portal.</summary>
    /// <value>Inyectado por el contenedor.</value>
    [Inject]
    protected EstadoDelPortal Estado { get; set; } = default!;

    /// <summary>Obtiene o establece el ejecutor de casos de uso.</summary>
    /// <value>Inyectado por el contenedor.</value>
    [Inject]
    protected EjecutorDeCasosDeUso Casos { get; set; } = default!;

    /// <summary>Obtiene o establece el servicio de navegación.</summary>
    /// <value>Inyectado por el contenedor.</value>
    [Inject]
    protected NavigationManager Navegacion { get; set; } = default!;

    [Inject]
    private ILogger<ComponenteDelPortal> Logger { get; set; } = default!;

    /// <summary>Obtiene el último mensaje de error para mostrar.</summary>
    /// <value><c>null</c> si la última operación terminó bien.</value>
    protected string? MensajeDeError { get; private set; }

    /// <summary>Obtiene o establece el último mensaje de éxito para mostrar.</summary>
    /// <value><c>null</c> si no hay nada que confirmar.</value>
    protected string? MensajeDeExito { get; set; }

    /// <summary>Obtiene un valor que indica si hay una operación en curso.</summary>
    /// <value><c>true</c> mientras se ejecuta una operación.</value>
    protected bool EstaCargando { get; private set; }

    /// <summary>Obtiene un valor que indica si la carga inicial terminó.</summary>
    /// <value><c>true</c> tras la primera carga, haya fallado o no.</value>
    protected bool Inicializado { get; private set; }

    /// <summary>Obtiene un valor que indica si la página depende de la empresa elegida en la cabecera.</summary>
    /// <value><c>false</c> por omisión.</value>
    protected virtual bool DependeDeEmpresa => false;

    /// <summary>
    /// Obtiene la empresa que la página debe enviar a los casos de uso.
    /// </summary>
    /// <value>
    /// La empresa elegida en la cabecera cuando el usuario elige empresa (roles
    /// transversales o empresa cliente con varias empresas); <c>null</c> para la
    /// empresa cliente con una sola, cuya empresa impone el servidor.
    /// </value>
    protected Guid? EmpresaDeConsulta => ContextoDeUsuario.EligeEmpresa ? Estado.EmpresaId : null;

    /// <summary>
    /// Obtiene un valor que indica si falta elegir una empresa para continuar.
    /// </summary>
    /// <value><c>true</c> si el usuario elige empresa y todavía no lo ha hecho.</value>
    protected bool FaltaEmpresa => ContextoDeUsuario.EligeEmpresa && Estado.EmpresaId is null;

    /// <summary>
    /// Obtiene o establece la empresa indicada en la dirección (<c>?empresaId=</c>).
    /// </summary>
    /// <value>
    /// La usan los enlaces de las tareas del inicio y de la bandeja: al abrir la
    /// página, esa empresa pasa a ser la de trabajo si el usuario puede elegirla.
    /// </value>
    [SupplyParameterFromQuery(Name = "empresaId")]
    public Guid? EmpresaIdDeRuta { get; set; }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await EjecutarAsync(async () =>
        {
            await ContextoDeUsuario.GarantizarResueltoAsync();
            await AplicarEmpresaDeTrabajoAsync();
            await CargarDatosAsync();
        });

        // Se suscribe después de la carga inicial para no cargar dos veces.
        Estado.EmpresaCambiada += AlCambiarEmpresa;
        Inicializado = true;
    }

    /// <summary>
    /// Carga los datos de la página. Se invoca con la identidad ya resuelta.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    protected virtual Task CargarDatosAsync() => Task.CompletedTask;

    /// <summary>
    /// Reacciona al cambio de empresa en la cabecera. Por omisión recarga los datos.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    protected virtual Task AlCambiarEmpresaAsync() => CargarDatosAsync();

    /// <summary>
    /// Ejecuta una operación mostrando el indicador de carga y traduciendo las
    /// excepciones a un mensaje para el usuario.
    /// </summary>
    /// <param name="operacion">Operación a ejecutar.</param>
    /// <returns><c>true</c> si la operación terminó sin errores.</returns>
    protected async Task<bool> EjecutarAsync(Func<Task> operacion)
    {
        ArgumentNullException.ThrowIfNull(operacion);

        MensajeDeError = null;
        EstaCargando = true;

        try
        {
            await operacion();
            return true;
        }
        catch (AccesoNoAutorizadoException)
        {
            // Mensaje genérico a propósito: confirmar la existencia del recurso
            // sería una filtración entre empresas.
            MensajeDeError = T["error.noEncontrado"];
        }
        catch (EntradaInvalidaException invalida)
        {
            MensajeDeError = string.Join(" ", invalida.Errores.Select(static e => e.Mensaje));
        }
        catch (DomainException excepcion)
        {
            MensajeDeError = excepcion.Message;
        }
        catch (Exception excepcion) when (excepcion is not OperationCanceledException)
        {
            Logger.LogError(excepcion, "Error no controlado en {Pagina}.", GetType().Name);
            MensajeDeError = T["error.inesperado"];
        }
        finally
        {
            EstaCargando = false;
        }

        return false;
    }

    /// <summary>Muestra un error de validación propio de la pantalla.</summary>
    /// <param name="mensaje">Mensaje ya traducido.</param>
    protected void MostrarError(string mensaje)
    {
        MensajeDeError = mensaje;
        MensajeDeExito = null;
    }

    /// <summary>Limpia los mensajes de error y de éxito.</summary>
    protected void LimpiarMensajes()
    {
        MensajeDeError = null;
        MensajeDeExito = null;
    }

    /// <summary>Formatea un importe con dos decimales en la cultura activa.</summary>
    /// <param name="valor">Importe.</param>
    /// <returns>El texto formateado.</returns>
    protected string Moneda(decimal valor) => valor.ToString("N2", T.Cultura);

    /// <summary>Formatea una cantidad con los decimales indicados.</summary>
    /// <param name="valor">Cantidad.</param>
    /// <param name="decimales">Decimales a mostrar.</param>
    /// <returns>El texto formateado.</returns>
    protected string Numero(decimal valor, int decimales = 2) => valor.ToString("N" + decimales, T.Cultura);

    /// <summary>Formatea un instante en hora local.</summary>
    /// <param name="momento">Instante.</param>
    /// <returns>Fecha y hora cortas.</returns>
    protected string Fecha(DateTimeOffset momento) => momento.ToLocalTime().ToString("g", T.Cultura);

    /// <summary>Formatea una fecha.</summary>
    /// <param name="fecha">Fecha.</param>
    /// <returns>Fecha corta.</returns>
    protected string Fecha(DateOnly fecha) => fecha.ToString("d", T.Cultura);

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Libera los recursos de la página.
    /// </summary>
    /// <param name="disposing"><c>true</c> si se invoca desde <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_liberado)
        {
            return;
        }

        if (disposing)
        {
            Estado.EmpresaCambiada -= AlCambiarEmpresa;
        }

        _liberado = true;
    }

    /// <summary>
    /// Fija la empresa de trabajo antes de la primera carga: la de la dirección
    /// si el usuario puede elegirla y, para la empresa cliente con varias
    /// empresas, su empresa principal si todavía no eligió ninguna.
    /// </summary>
    private async Task AplicarEmpresaDeTrabajoAsync()
    {
        if (!ContextoDeUsuario.EligeEmpresa)
        {
            return;
        }

        if (EmpresaIdDeRuta is { } empresaId && empresaId != Estado.EmpresaId)
        {
            EmpresaDto? empresa = ContextoDeUsuario.EsTransversal
                ? await Casos.UsarAsync<IConsultasEmpresas, EmpresaDto?>(c => c.ObtenerAsync(empresaId))
                : ContextoDeUsuario.EmpresasDelUsuario.FirstOrDefault(e => e.Id == empresaId);

            if (empresa is not null)
            {
                await Estado.SeleccionarEmpresaAsync(empresa.Id, empresa.RazonSocial);
                return;
            }
        }

        if (Estado.EmpresaId is null && !ContextoDeUsuario.EsTransversal && ContextoDeUsuario.EmpresasDelUsuario.Count > 0)
        {
            EmpresaDto principal = ContextoDeUsuario.EmpresasDelUsuario[0];
            await Estado.SeleccionarEmpresaAsync(principal.Id, principal.RazonSocial);
        }
    }

    private Task AlCambiarEmpresa()
        => !DependeDeEmpresa
            ? Task.CompletedTask
            : InvokeAsync(async () =>
            {
                MensajeDeExito = null;
                await EjecutarAsync(AlCambiarEmpresaAsync);
                StateHasChanged();
            });
}
