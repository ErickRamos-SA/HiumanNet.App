using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HuimanNet.App.Localizacion;
using HuimanNet.App.Services;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Enums;

namespace HuimanNet.App.ViewModels;

/// <summary>
/// Menú «Más»: accesos a las pantallas que no caben en la barra inferior.
/// </summary>
public sealed partial class MasViewModel : ViewModelBase
{
    private readonly SesionDeLaApp _sesion;
    private readonly IServicioDeNavegacion _navegacion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="MasViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    /// <param name="navegacion">Navegación.</param>
    public MasViewModel(Traductor traductor, SesionDeLaApp sesion, IServicioDeNavegacion navegacion)
        : base(traductor)
    {
        _sesion = sesion;
        _navegacion = navegacion;
    }

    /// <summary>Obtiene las opciones permitidas al usuario.</summary>
    /// <value>Opciones del menú.</value>
    public ObservableCollection<OpcionDeMenu> Opciones { get; } = [];

    /// <summary>Arma las opciones según los permisos.</summary>
    [RelayCommand]
    public void Cargar()
    {
        Titulo = T["movil.mas"];
        var opciones = new List<OpcionDeMenu>();

        if (_sesion.Puede(AccionDelSistema.AdministrarEmpleados))
        {
            opciones.Add(new OpcionDeMenu(T["nav.empleados"], T["empleados.subtitulo"], AppShell.RutaEmpleados));
        }

        if (_sesion.Puede(AccionDelSistema.ConsultarExplicacionDeCalculos))
        {
            opciones.Add(new OpcionDeMenu(T["nav.explicacion"], T["explicacion.subtitulo"], AppShell.RutaExplicacion));
        }

        if (_sesion.Puede(AccionDelSistema.AdministrarUsuarios) || _sesion.Puede(AccionDelSistema.AdministrarCatalogosDeCalculo))
        {
            opciones.Add(new OpcionDeMenu(T["movil.administracion"], T["movil.administracionDetalle"], AppShell.RutaAdministracion));
        }

        opciones.Add(new OpcionDeMenu(T["nav.perfil"], T["perfil.subtitulo"], AppShell.RutaCuenta));
        Reemplazar(Opciones, opciones);
    }

    /// <summary>
    /// Abre una opción.
    /// </summary>
    /// <param name="opcion">Opción elegida.</param>
    /// <returns>Una tarea que representa la navegación.</returns>
    [RelayCommand]
    public Task AbrirAsync(OpcionDeMenu? opcion) => opcion is null ? Task.CompletedTask : _navegacion.IrAAsync(opcion.Ruta);

    /// <inheritdoc/>
    protected override void AlCambiarIdioma() => Cargar();
}
