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
/// Resumen de una corrida: cifras, resultados por trabajador, facturación,
/// aprobación y exportación.
/// </summary>
public sealed partial class CorridaViewModel : ViewModelBase, IQueryAttributable
{
    private readonly IServicioDeApi _api;
    private readonly IServicioDeDialogos _dialogos;
    private readonly IServicioDeNavegacion _navegacion;
    private readonly SesionDeLaApp _sesion;
    private IReadOnlyList<ResultadoDeNominaDto> _todos = [];
    private Guid _corridaId;
    private Guid _empresaId;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CorridaViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="dialogos">Diálogos.</param>
    /// <param name="navegacion">Navegación.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    public CorridaViewModel(
        Traductor traductor, IServicioDeApi api, IServicioDeDialogos dialogos, IServicioDeNavegacion navegacion, SesionDeLaApp sesion)
        : base(traductor)
    {
        _api = api;
        _dialogos = dialogos;
        _navegacion = navegacion;
        _sesion = sesion;
        Titulo = traductor["corrida.tituloPagina"];
    }

    /// <summary>Obtiene o establece la corrida.</summary>
    /// <value><c>null</c> hasta cargar.</value>
    [ObservableProperty]
    public partial CorridaDeNominaDto? Corrida { get; set; }

    /// <summary>Obtiene o establece las cifras principales.</summary>
    /// <value>Bruto, neto, complemento, costo…</value>
    [ObservableProperty]
    public partial IReadOnlyList<CifraDeCorrida> Cifras { get; set; } = [];

    /// <summary>Obtiene o establece los resultados visibles.</summary>
    /// <value>Filtrados por <see cref="Texto"/>.</value>
    [ObservableProperty]
    public partial IReadOnlyList<ResultadoDeNominaDto> Resultados { get; set; } = [];

    /// <summary>Obtiene o establece la facturación por razón social.</summary>
    /// <value>Una fila por razón social.</value>
    [ObservableProperty]
    public partial IReadOnlyList<FacturacionDeCorridaDto> Facturacion { get; set; } = [];

    /// <summary>Obtiene o establece el texto de búsqueda.</summary>
    /// <value>Filtra por clave o nombre.</value>
    [ObservableProperty]
    public partial string? Texto { get; set; }

    /// <summary>Indica si la corrida se puede aprobar o descartar.</summary>
    /// <value><c>true</c> si el usuario puede y la corrida sigue abierta.</value>
    public bool PuedeAprobar => _sesion.Puede(AccionDelSistema.AprobarNomina)
        && Corrida?.Estado is EstadoDeCorrida.Calculada or EstadoDeCorrida.Cotejada;

    /// <summary>Obtiene la empresa que se envía a la API.</summary>
    /// <value>La recibida al navegar; si no llegó, la de consulta de la sesión.</value>
    private Guid? EmpresaParaApi =>_empresaId == Guid.Empty ? _sesion.EmpresaDeConsulta : _empresaId;

    /// <inheritdoc/>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.TryGetValue("corridaId", out object? corrida) && corrida is Guid corridaId)
        {
            _corridaId = corridaId;
        }

        if (query.TryGetValue("empresaId", out object? empresa) && empresa is Guid empresaId)
        {
            _empresaId = empresaId;
        }
    }

    /// <summary>
    /// Carga el resumen.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(CargarInternoAsync);

    /// <summary>
    /// Aprueba la corrida tras confirmar.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task AprobarAsync() => CambiarEstadoAsync(EstadoDeCorrida.Aprobada, T["corrida.aprobar"], T["corrida.confirmarAprobar"]);

    /// <summary>
    /// Descarta la corrida tras confirmar.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task DescartarAsync() => CambiarEstadoAsync(EstadoDeCorrida.Descartada, T["corrida.descartar"], T["corrida.confirmarDescartar"]);

    /// <summary>
    /// Descarga la exportación y la ofrece para compartir.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task ExportarAsync() => EjecutarAsync(async () =>
    {
        ArchivoDescargado archivo = await _api.ExportarCorridaAsync(_corridaId, EmpresaParaApi);
        string ruta = Path.Combine(FileSystem.CacheDirectory, archivo.Nombre);

        await File.WriteAllBytesAsync(ruta, archivo.Contenido);
        await Share.Default.RequestAsync(new ShareFileRequest(archivo.Nombre, new ShareFile(ruta, archivo.TipoDeContenido)));
    });

    /// <summary>
    /// Abre el detalle de un trabajador.
    /// </summary>
    /// <param name="resultado">Resultado elegido.</param>
    /// <returns>Una tarea que representa la navegación.</returns>
    [RelayCommand]
    public Task AbrirResultadoAsync(ResultadoDeNominaDto? resultado)
        => resultado is null
            ? Task.CompletedTask
            : _navegacion.IrAAsync(AppShell.RutaResultado, new Dictionary<string, object> { ["resultadoId"] = resultado.Id, ["empresaId"] = _empresaId });

    /// <summary>Carga el resumen y arma las cifras, sin el indicador de ocupado.</summary>
    /// <returns>Tarea que finaliza al cargar el resumen.</returns>
    private async Task CargarInternoAsync()
    {
        ResumenDeCorridaDto resumen = await _api.ObtenerResumenDeCorridaAsync(_corridaId, EmpresaParaApi);
        CorridaDeNominaDto c = resumen.Corrida;

        Corrida = c;
        Titulo = T.Formato("corrida.titulo", c.Numero);
        Facturacion = resumen.Facturacion;
        _todos = resumen.Resultados;

        Cifras =
        [
            new CifraDeCorrida(T["nomina.bruto"], c.Bruto),
            new CifraDeCorrida(T["corrida.percepciones"], c.Percepciones),
            new CifraDeCorrida(T["corrida.deducciones"], c.Deducciones),
            new CifraDeCorrida(T["nomina.neto"], c.Neto),
            new CifraDeCorrida(T["corrida.complemento"], c.ComplementoSindical),
            new CifraDeCorrida(T["corrida.isr"], c.Isr),
            new CifraDeCorrida(T["corrida.imssTrabajador"], c.ImssTrabajador),
            new CifraDeCorrida(T["corrida.imssPatronal"], c.ImssPatronal),
            new CifraDeCorrida(T["corrida.infonavit"], c.Infonavit),
            new CifraDeCorrida(T["corrida.isn"], c.Isn),
            new CifraDeCorrida(T["corrida.comision"], c.Comision),
            new CifraDeCorrida(T["nomina.costoTotal"], c.CostoTotal),
        ];

        Filtrar();
        OnPropertyChanged(nameof(PuedeAprobar));
    }

    /// <summary>Cambia el estado de la corrida tras confirmarlo y la recarga.</summary>
    /// <param name="estado">Estado nuevo.</param>
    /// <param name="boton">Texto del botón de confirmación.</param>
    /// <param name="mensaje">Pregunta de confirmación.</param>
    /// <returns>Tarea que finaliza al cambiar el estado o al cancelar.</returns>
    private async Task CambiarEstadoAsync(EstadoDeCorrida estado, string boton, string mensaje)
    {
        if (!await _dialogos.ConfirmarAsync(boton, mensaje, boton))
        {
            return;
        }

        await EjecutarAsync(async () =>
        {
            await _api.CambiarEstadoCorridaAsync(_corridaId, new CambiarEstadoCorridaRequest(estado, EmpresaParaApi));
            MensajeDeExito = T.Formato("corrida.estadoCambiado", T.Enumerado(estado));
            await CargarInternoAsync();
        });
    }

    /// <summary>Filtra los resultados por clave o nombre del empleado.</summary>
    private void Filtrar()
    {
        string? texto = Texto?.Trim();

        Resultados =string.IsNullOrEmpty(texto)
            ? _todos
            : [.. _todos.Where(r => r.ClaveEmpleado.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || r.NombreEmpleado.Contains(texto, StringComparison.OrdinalIgnoreCase))];
    }

    /// <summary>Vuelve a filtrar al cambiar el texto de búsqueda.</summary>
    /// <param name="value">Texto nuevo.</param>
    partial void OnTextoChanged(string? value) => Filtrar();
}
