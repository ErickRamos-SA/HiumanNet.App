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
/// Explicación de los cálculos de cada esquema de pago, con los valores vigentes.
/// </summary>
public sealed partial class ExplicacionViewModel : ViewModelBase
{
    private readonly IServicioDeApi _api;
    private readonly SesionDeLaApp _sesion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ExplicacionViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    public ExplicacionViewModel(Traductor traductor, IServicioDeApi api, SesionDeLaApp sesion)
        : base(traductor)
    {
        _api = api;
        _sesion = sesion;
        Titulo = traductor["explicacion.titulo"];
    }

    /// <summary>Obtiene los esquemas de pago.</summary>
    /// <value>IMSS, sindicato y honorarios.</value>
    public IReadOnlyList<EsquemaDePago> Esquemas { get; } = [EsquemaDePago.Imss, EsquemaDePago.Sindicato, EsquemaDePago.Honorarios];

    /// <summary>Obtiene los nombres traducidos de los esquemas.</summary>
    /// <value>En el mismo orden que <see cref="Esquemas"/>.</value>
    public IReadOnlyList<string> NombresDeEsquemas => [.. Esquemas.Select(e => T.Enumerado(e))];

    /// <summary>Obtiene o establece la posición del esquema elegido.</summary>
    /// <value>0 IMSS, 1 sindicato, 2 honorarios.</value>
    public int IndiceDeEsquema
    {
        get => Esquemas.ToList().IndexOf(Esquema);
        set
        {
            if (value >= 0 && value < Esquemas.Count)
            {
                Esquema = Esquemas[value];
            }
        }
    }

    /// <summary>Obtiene o establece el esquema elegido.</summary>
    /// <value>IMSS por defecto.</value>
    [ObservableProperty]
    public partial EsquemaDePago Esquema { get; set; } = EsquemaDePago.Imss;

    /// <summary>Obtiene o establece las secciones narrativas.</summary>
    /// <value>En el idioma de la interfaz.</value>
    [ObservableProperty]
    public partial IReadOnlyList<ExplicacionDeCalculoDto> Secciones { get; set; } = [];

    /// <summary>Obtiene o establece los conceptos con su fórmula.</summary>
    /// <value>En orden de presentación.</value>
    [ObservableProperty]
    public partial IReadOnlyList<ConceptoDeNominaDto> Conceptos { get; set; } = [];

    /// <summary>Obtiene o establece el aviso de catálogo incoherente.</summary>
    /// <value><c>null</c> si el catálogo es válido.</value>
    [ObservableProperty]
    public partial string? AvisoDeCatalogo { get; set; }

    /// <summary>
    /// Carga la explicación del esquema elegido.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        Titulo = T["explicacion.titulo"];
        ExplicacionCompletaDto explicacion = await _api.ObtenerExplicacionAsync(Esquema, T.Idioma, _sesion.EmpresaDeConsulta);

        Secciones = [.. explicacion.Secciones.OrderBy(s => s.Orden)];
        Conceptos = [.. explicacion.Conceptos.OrderBy(c => c.Orden)];
        AvisoDeCatalogo = explicacion.ErrorDeCatalogo is null ? null : $"{T["explicacion.errorDeCatalogo"]} {explicacion.ErrorDeCatalogo}";
    });

    /// <inheritdoc/>
    protected override void AlCambiarIdioma()
    {
        OnPropertyChanged(nameof(NombresDeEsquemas));
        CargarCommand.Execute(null);
    }

    /// <summary>Recarga la explicación al elegir otro esquema.</summary>
    /// <param name="value">Esquema elegido.</param>
    partial void OnEsquemaChanged(EsquemaDePago value)
    {
        OnPropertyChanged(nameof(IndiceDeEsquema));
        CargarCommand.Execute(null);
    }
}
