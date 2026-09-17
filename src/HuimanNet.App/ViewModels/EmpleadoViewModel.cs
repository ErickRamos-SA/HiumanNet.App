using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HuimanNet.App.Services;
using HuimanNet.Contracts.Catalogos;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Empleados;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Enums;

namespace HuimanNet.App.ViewModels;

/// <summary>
/// Ficha de un empleado con sus contratos (esquemas y razones sociales).
/// </summary>
public sealed partial class EmpleadoViewModel : ViewModelBase, IQueryAttributable
{
    private readonly IServicioDeApi _api;
    private readonly SesionDeLaApp _sesion;
    private Guid _empleadoId;
    private Guid? _empresaId;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="EmpleadoViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    public EmpleadoViewModel(Traductor traductor, IServicioDeApi api, SesionDeLaApp sesion)
        : base(traductor)
    {
        _api = api;
        _sesion = sesion;
    }

    /// <summary>Obtiene o establece el empleado.</summary>
    /// <value><c>null</c> hasta cargar.</value>
    [ObservableProperty]
    public partial EmpleadoDto? Empleado { get; set; }

    /// <inheritdoc/>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.TryGetValue("empleadoId", out object? valor) && valor is Guid empleadoId)
        {
            _empleadoId = empleadoId;
        }

        // Empresa a la que pertenece el empleado: la lista puede mostrar varias.
        if (query.TryGetValue("empresaId", out object? empresa) && empresa is Guid empresaId)
        {
            _empresaId = empresaId;
        }
    }

    /// <summary>
    /// Carga la ficha.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        Empleado = await _api.ObtenerEmpleadoAsync(_empleadoId, _sesion.EligeEmpresa ? _empresaId ?? _sesion.EmpresaDeConsulta : null);
        Titulo = Empleado.NombreCompleto;
    });
}
