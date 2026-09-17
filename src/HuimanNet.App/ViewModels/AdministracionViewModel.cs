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
/// Administración desde el móvil: consulta de usuarios y prueba de fórmulas.
/// </summary>
/// <remarks>
/// La edición de catálogos y usuarios, que implica formularios extensos, se
/// hace desde el portal web; aquí se ofrece lo que tiene sentido en campo:
/// ver quién tiene acceso y comprobar una fórmula con el catálogo vigente.
/// </remarks>
public sealed partial class AdministracionViewModel : ViewModelBase
{
    private readonly IServicioDeApi _api;
    private readonly SesionDeLaApp _sesion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AdministracionViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    public AdministracionViewModel(Traductor traductor, IServicioDeApi api, SesionDeLaApp sesion)
        : base(traductor)
    {
        _api = api;
        _sesion = sesion;
        Titulo = traductor["movil.administracion"];
    }

    /// <summary>Indica si puede consultar usuarios.</summary>
    /// <value><c>true</c> con el permiso de administrar usuarios.</value>
    public bool PuedeUsuarios => _sesion.Puede(AccionDelSistema.AdministrarUsuarios);

    /// <summary>Indica si puede probar fórmulas.</summary>
    /// <value><c>true</c> con el permiso de administrar catálogos.</value>
    public bool PuedeCatalogos => _sesion.Puede(AccionDelSistema.AdministrarCatalogosDeCalculo);

    /// <summary>Obtiene o establece los usuarios.</summary>
    /// <value>Ordenados por nombre.</value>
    [ObservableProperty]
    public partial IReadOnlyList<UsuarioDto> Usuarios { get; set; } = [];

    /// <summary>Obtiene los esquemas para la prueba de fórmulas.</summary>
    /// <value>IMSS, sindicato y honorarios.</value>
    public IReadOnlyList<EsquemaDePago> Esquemas { get; } = [EsquemaDePago.Imss, EsquemaDePago.Sindicato, EsquemaDePago.Honorarios];

    /// <summary>Obtiene los nombres traducidos de los esquemas.</summary>
    /// <value>En el mismo orden que <see cref="Esquemas"/>.</value>
    public IReadOnlyList<string> NombresDeEsquemas => [.. Esquemas.Select(e => T.Enumerado(e))];

    /// <summary>Obtiene o establece la posición del esquema elegido.</summary>
    /// <value>0 IMSS, 1 sindicato, 2 honorarios.</value>
    public int IndiceDeEsquemaDePrueba
    {
        get => Esquemas.ToList().IndexOf(EsquemaDePrueba);
        set
        {
            if (value >= 0 && value < Esquemas.Count)
            {
                EsquemaDePrueba = Esquemas[value];
            }
        }
    }

    /// <summary>Obtiene o establece el esquema de la prueba.</summary>
    /// <value>IMSS por defecto.</value>
    [ObservableProperty]
    public partial EsquemaDePago EsquemaDePrueba { get; set; } = EsquemaDePago.Imss;

    /// <summary>Obtiene o establece la fórmula a probar.</summary>
    /// <value>Texto en el lenguaje de fórmulas.</value>
    [ObservableProperty]
    public partial string Formula { get; set; } = "REDONDEAR(SUELDO_PERIODO_REAL * 1.16; 2)";

    /// <summary>Obtiene o establece el resultado de la prueba.</summary>
    /// <value>Valor, o el error de la fórmula.</value>
    [ObservableProperty]
    public partial string? ResultadoDePrueba { get; set; }

    /// <summary>
    /// Carga los usuarios.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        Titulo = T["movil.administracion"];
        OnPropertyChanged(nameof(PuedeUsuarios));
        OnPropertyChanged(nameof(PuedeCatalogos));

        Usuarios = PuedeUsuarios ? await _api.ListarUsuariosAsync(_sesion.EmpresaDeConsulta, incluirInactivos: true) : [];
    });

    /// <summary>
    /// Evalúa la fórmula con el catálogo vigente.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task ProbarAsync() => EjecutarAsync(async () =>
    {
        ProbarFormulaResponse respuesta = await _api.ProbarFormulaAsync(
            new ProbarFormulaRequest(Formula, EsquemaDePrueba, _sesion.EmpresaDeConsulta, []));

        ResultadoDePrueba = respuesta.Valida
            ? $"{T["catalogos.resultado"]}: {respuesta.Resultado?.ToString("N6", T.Cultura) ?? "—"}"
              + (respuesta.Referencias.Count > 0 ? $"\n{T["catalogos.referencias"]}: {string.Join(", ", respuesta.Referencias)}" : string.Empty)
            : respuesta.Error;
    });
}
