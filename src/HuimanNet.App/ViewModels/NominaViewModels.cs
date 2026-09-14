using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HuimanNet.App.Services;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Domain.Enums;

namespace HuimanNet.App.ViewModels;

/// <summary>Cifra del resumen de una corrida.</summary>
/// <param name="Etiqueta">Texto traducido.</param>
/// <param name="Valor">Importe.</param>
public sealed record CifraDeCorrida(string Etiqueta, decimal Valor);

/// <summary>Conceptos de un tipo, para una lista agrupada.</summary>
public sealed class GrupoDeConceptos : List<ConceptoCalculadoDto>
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="GrupoDeConceptos"/>.
    /// </summary>
    /// <param name="nombre">Nombre traducido del tipo.</param>
    /// <param name="conceptos">Conceptos del grupo.</param>
    public GrupoDeConceptos(string nombre, IEnumerable<ConceptoCalculadoDto> conceptos)
        : base(conceptos)
        => Nombre = nombre;

    /// <summary>Obtiene el nombre del grupo.</summary>
    /// <value>Por ejemplo «Percepciones».</value>
    public string Nombre { get; }
}

/// <summary>
/// Corridas de nómina de un período y cálculo de una nueva.
/// </summary>
public sealed partial class NominaViewModel : ViewModelBase
{
    private readonly IServicioDeApi _api;
    private readonly SesionDeLaApp _sesion;
    private readonly IServicioDeNavegacion _navegacion;
    private readonly IServicioDeDialogos _dialogos;
    private bool _cambiandoPeriodos;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="NominaViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    /// <param name="navegacion">Navegación.</param>
    /// <param name="dialogos">Diálogos, para confirmar un reproceso.</param>
    public NominaViewModel(
        Traductor traductor, IServicioDeApi api, SesionDeLaApp sesion, IServicioDeNavegacion navegacion, IServicioDeDialogos dialogos)
        : base(traductor)
    {
        _api = api;
        _sesion = sesion;
        _navegacion = navegacion;
        _dialogos = dialogos;
        Titulo = traductor["nomina.titulo"];
    }

    /// <summary>Obtiene los períodos.</summary>
    /// <value>Opciones del selector.</value>
    public ObservableCollection<PeriodoDto> Periodos { get; } = [];

    /// <summary>Obtiene los nombres de los períodos para el selector.</summary>
    /// <value>En el mismo orden que <see cref="Periodos"/>.</value>
    public ObservableCollection<string> NombresDePeriodos { get; } = [];

    /// <summary>Obtiene o establece la posición del período elegido.</summary>
    /// <value>-1 si no hay períodos.</value>
    public int IndiceDePeriodo
    {
        get => Periodo is null ? -1 : Periodos.IndexOf(Periodo);
        set
        {
            if (value >= 0 && value < Periodos.Count && !Equals(Periodos[value], Periodo))
            {
                Periodo = Periodos[value];
            }
        }
    }

    /// <summary>Obtiene o establece el período elegido.</summary>
    /// <value>El primero no cerrado por defecto.</value>
    [ObservableProperty]
    public partial PeriodoDto? Periodo { get; set; }

    /// <summary>Obtiene o establece las corridas del período.</summary>
    /// <value>De la más reciente a la más antigua.</value>
    [ObservableProperty]
    public partial IReadOnlyList<CorridaDeNominaDto> Corridas { get; set; } = [];

    /// <summary>Obtiene o establece las observaciones de la corrida a calcular.</summary>
    /// <value>Opcional.</value>
    [ObservableProperty]
    public partial string? Observaciones { get; set; }

    /// <summary>Indica si el usuario puede calcular.</summary>
    /// <value><c>true</c> para nómina y administración; la empresa cliente consulta en solo lectura.</value>
    public bool PuedeCalcular => _sesion.Puede(AccionDelSistema.CalcularNomina);

    /// <summary>Obtiene el texto del botón de cálculo.</summary>
    /// <value>«Calcular nómina» la primera vez; «Reprocesar nómina» si el período ya tiene corridas.</value>
    public string TextoDeCalculo => Corridas.Count > 0 ? T["nomina.reprocesar"] : T["nomina.calcular"];

    /// <summary>Obtiene el aviso sobre el cálculo del período elegido.</summary>
    /// <value>Solo lectura, el motivo por el que no se puede calcular, o <c>null</c>.</value>
    public string? AvisoDeCalculo => !PuedeCalcular ? T["nomina.soloLectura"] : MotivoSinCalculo;

    /// <summary>
    /// Motivo por el que el período elegido no admite cálculo, o <c>null</c>.
    /// Replica las reglas del servidor para explicarlas antes de pulsar.
    /// </summary>
    private string? MotivoSinCalculo => Periodo switch
    {
        null => null,
        { Estado: EstadoPeriodo.Abierto } => T["nomina.requiereArchivos"],
        { Estado: EstadoPeriodo.Cerrado } => T["nomina.periodoCerrado"],
        _ => Corridas.FirstOrDefault(c => c.Estado == EstadoDeCorrida.Aprobada) is { } aprobada
            ? T.Formato("nomina.yaAprobada", aprobada.Numero)
            : null,
    };

    private bool PuedeCalcularAhora => PuedeCalcular && Periodo is not null && MotivoSinCalculo is null;

    /// <summary>
    /// Carga períodos y corridas.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        Titulo = T["nomina.titulo"];
        OnPropertyChanged(nameof(PuedeCalcular));
        ActualizarEstadoDelCalculo();

        if (_sesion.FaltaEmpresa)
        {
            Periodos.Clear();
            Corridas = [];
            MensajeDeError = T["comun.seleccioneEmpresaDetalle"];
            return;
        }

        IReadOnlyList<PeriodoDto> periodos = await _api.ListarPeriodosAsync(_sesion.EmpresaDeConsulta, incluirCerrados: true);

        _cambiandoPeriodos = true;
        Guid? anterior = Periodo?.Id;
        Reemplazar(Periodos, periodos);
        Reemplazar(NombresDePeriodos, periodos.Select(p => $"{p.Clave} · {p.Descripcion}"));
        Periodo = Periodos.FirstOrDefault(p => p.Id == anterior)
            ?? Periodos.FirstOrDefault(p => p.Estado != EstadoPeriodo.Cerrado)
            ?? Periodos.FirstOrDefault();
        _cambiandoPeriodos = false;
        OnPropertyChanged(nameof(IndiceDePeriodo));

        await CargarCorridasInternoAsync();
    });

    /// <summary>
    /// Recarga las corridas del período.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarCorridasAsync() => EjecutarAsync(CargarCorridasInternoAsync);

    /// <summary>
    /// Calcula la nómina del período (o la reprocesa, previa confirmación) y abre la corrida.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand(CanExecute = nameof(PuedeCalcularAhora))]
    public async Task CalcularAsync()
    {
        if (Periodo is not { } periodo)
        {
            return;
        }

        if (Corridas.Count > 0)
        {
            string boton = T["nomina.reprocesar"];
            string mensaje = T.Formato("nomina.confirmarReproceso", Corridas.Max(c => c.Numero) + 1);

            if (!await _dialogos.ConfirmarAsync(boton, mensaje, boton))
            {
                return;
            }
        }

        CorridaDeNominaDto? corrida = null;

        bool exito = await EjecutarAsync(async () =>
            corrida = await _api.CalcularNominaAsync(new CalcularNominaRequest(periodo.Id, EmpresaDe(periodo), Observaciones)));

        if (exito && corrida is not null)
        {
            Observaciones = null;
            await AbrirCorridaAsync(corrida);
        }
    }

    /// <summary>
    /// Abre el resumen de una corrida.
    /// </summary>
    /// <param name="corrida">Corrida elegida.</param>
    /// <returns>Una tarea que representa la navegación.</returns>
    [RelayCommand]
    public Task AbrirCorridaAsync(CorridaDeNominaDto? corrida)
        => corrida is null
            ? Task.CompletedTask
            : _navegacion.IrAAsync(AppShell.RutaCorrida, new Dictionary<string, object> { ["corridaId"] = corrida.Id, ["empresaId"] = corrida.EmpresaId });

    private async Task CargarCorridasInternoAsync()
        => Corridas = Periodo is { } periodo ? await _api.ListarCorridasAsync(periodo.Id, EmpresaDe(periodo)) : [];

    private Guid? EmpresaDe(PeriodoDto periodo) => periodo.EmpresaId;

    /// <summary>
    /// Refresca el texto y el aviso del cálculo, y habilita o no el botón,
    /// cuando cambian el período, sus corridas o el idioma.
    /// </summary>
    private void ActualizarEstadoDelCalculo()
    {
        OnPropertyChanged(nameof(TextoDeCalculo));
        OnPropertyChanged(nameof(AvisoDeCalculo));
        CalcularCommand.NotifyCanExecuteChanged();
    }

    /// <inheritdoc/>
    protected override void AlCambiarIdioma() => ActualizarEstadoDelCalculo();

    partial void OnCorridasChanged(IReadOnlyList<CorridaDeNominaDto> value) => ActualizarEstadoDelCalculo();

    partial void OnPeriodoChanged(PeriodoDto? value)
    {
        OnPropertyChanged(nameof(IndiceDePeriodo));
        ActualizarEstadoDelCalculo();

        if (!_cambiandoPeriodos)
        {
            CargarCorridasCommand.Execute(null);
        }
    }
}

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

    private Guid? EmpresaParaApi => _empresaId == Guid.Empty ? _sesion.EmpresaDeConsulta : _empresaId;

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

    private void Filtrar()
    {
        string? texto = Texto?.Trim();

        Resultados = string.IsNullOrEmpty(texto)
            ? _todos
            : [.. _todos.Where(r => r.ClaveEmpleado.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || r.NombreEmpleado.Contains(texto, StringComparison.OrdinalIgnoreCase))];
    }

    partial void OnTextoChanged(string? value) => Filtrar();
}

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
