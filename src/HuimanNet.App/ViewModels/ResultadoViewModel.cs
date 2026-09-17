using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HuimanNet.App.Services;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Domain.Enums;

namespace HuimanNet.App.ViewModels;

/// <summary>
/// Detalle por concepto del cálculo de un trabajador, con la fórmula aplicada.
/// </summary>
public sealed partial class ResultadoViewModel : ViewModelBase, IQueryAttributable
{
    private readonly IServicioDeApi _api;
    private readonly SesionDeLaApp _sesion;
    private Guid _resultadoId;
    private Guid _empresaId;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ResultadoViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    public ResultadoViewModel(Traductor traductor, IServicioDeApi api, SesionDeLaApp sesion)
        : base(traductor)
    {
        _api = api;
        _sesion = sesion;
    }

    /// <summary>Obtiene o establece el resumen del trabajador.</summary>
    /// <value><c>null</c> hasta cargar.</value>
    [ObservableProperty]
    public partial ResultadoDeNominaDto? Resultado { get; set; }

    /// <summary>Obtiene los conceptos agrupados por tipo.</summary>
    /// <value>Bases, percepciones, deducciones…</value>
    public ObservableCollection<GrupoDeConceptos> Grupos { get; } = [];

    /// <inheritdoc/>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.TryGetValue("resultadoId", out object? resultado) && resultado is Guid resultadoId)
        {
            _resultadoId = resultadoId;
        }

        if (query.TryGetValue("empresaId", out object? empresa) && empresa is Guid empresaId)
        {
            _empresaId = empresaId;
        }
    }

    /// <summary>
    /// Carga el detalle.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        DetalleDeResultadoDto detalle = await _api.ObtenerDetalleDeResultadoAsync(_resultadoId, _empresaId == Guid.Empty ? _sesion.EmpresaDeConsulta : _empresaId);

        Resultado = detalle.Resultado;
        Titulo = $"{detalle.Resultado.ClaveEmpleado} · {detalle.Resultado.NombreEmpleado}";

        Reemplazar(Grupos, detalle.Conceptos
            .GroupBy(c => c.Tipo)
            .OrderBy(g => (int)g.Key)
            .Select(g => new GrupoDeConceptos(T.Enumerado(g.Key), g)));
    });
}
