using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HuimanNet.App.Services;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Contracts.Incidencias;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Domain.Enums;

namespace HuimanNet.App.ViewModels;

/// <summary>
/// Períodos de la empresa y acceso a sus documentos.
/// </summary>
public sealed partial class PeriodosViewModel : ViewModelBase
{
    private readonly IServicioDeApi _api;
    private readonly SesionDeLaApp _sesion;
    private readonly IServicioDeNavegacion _navegacion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="PeriodosViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    /// <param name="navegacion">Navegación.</param>
    public PeriodosViewModel(Traductor traductor, IServicioDeApi api, SesionDeLaApp sesion, IServicioDeNavegacion navegacion)
        : base(traductor)
    {
        _api = api;
        _sesion = sesion;
        _navegacion = navegacion;
        Titulo = traductor["periodos.titulo"];
    }

    /// <summary>Obtiene los períodos.</summary>
    /// <value>Del más reciente al más antiguo.</value>
    public ObservableCollection<PeriodoDto> Periodos { get; } = [];

    /// <summary>Obtiene o establece si se incluyen los cerrados.</summary>
    /// <value><c>false</c> por defecto.</value>
    [ObservableProperty]
    public partial bool IncluirCerrados { get; set; }

    /// <summary>
    /// Carga los períodos de la empresa de trabajo.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        Titulo = T["periodos.titulo"];

        if (_sesion.FaltaEmpresa)
        {
            Periodos.Clear();
            MensajeDeError = T["comun.seleccioneEmpresaDetalle"];
            return;
        }

        Reemplazar(Periodos, await _api.ListarPeriodosAsync(_sesion.EmpresaDeConsulta, IncluirCerrados));
    });

    /// <summary>
    /// Abre los documentos de un período.
    /// </summary>
    /// <param name="periodo">Período elegido.</param>
    /// <returns>Una tarea que representa la navegación.</returns>
    [RelayCommand]
    public Task AbrirPeriodoAsync(PeriodoDto? periodo)
        => periodo is null
            ? Task.CompletedTask
            : _navegacion.IrAAsync(AppShell.RutaDocumentos, new Dictionary<string, object> { ["periodo"] = periodo });

    partial void OnIncluirCerradosChanged(bool value) => CargarCommand.Execute(null);
}

/// <summary>
/// Documentos de un período: carga directa al almacenamiento y descarga con enlace temporal.
/// </summary>
public sealed partial class DocumentosViewModel : ViewModelBase, IQueryAttributable
{
    private readonly IServicioDeApi _api;
    private readonly IServicioDeDialogos _dialogos;
    private readonly SesionDeLaApp _sesion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="DocumentosViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="dialogos">Diálogos.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    public DocumentosViewModel(Traductor traductor, IServicioDeApi api, IServicioDeDialogos dialogos, SesionDeLaApp sesion)
        : base(traductor)
    {
        _api = api;
        _dialogos = dialogos;
        _sesion = sesion;
        Titulo = traductor["documentos.titulo"];
    }

    /// <summary>Obtiene los documentos del período.</summary>
    /// <value>Lista de documentos.</value>
    public ObservableCollection<DocumentoDto> Documentos { get; } = [];

    /// <summary>Obtiene o establece el período.</summary>
    /// <value>Llega como parámetro de navegación.</value>
    [ObservableProperty]
    public partial PeriodoDto? Periodo { get; set; }

    /// <summary>Indica si el usuario puede subir algún tipo de documento.</summary>
    /// <value><c>true</c> para mostrar el botón de carga.</value>
    public bool PuedeSubir => _sesion.Puede(AccionDelSistema.CargarDocumentos) || _sesion.Puede(AccionDelSistema.PublicarResultados);

    /// <inheritdoc/>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.TryGetValue("periodo", out object? valor) && valor is PeriodoDto periodo)
        {
            Periodo = periodo;
            Titulo = periodo.Clave;
        }
    }

    /// <summary>
    /// Carga los documentos.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        if (Periodo is { } periodo)
        {
            Reemplazar(Documentos, await _api.ListarDocumentosAsync(periodo.Id, EmpresaDe(periodo)));
            OnPropertyChanged(nameof(PuedeSubir));
        }
    });

    /// <summary>
    /// Elige tipo y archivo y lo sube.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public async Task SubirDocumentoAsync()
    {
        if (Periodo is not { } periodo)
        {
            return;
        }

        var tipos = new List<TipoDocumento>();

        if (_sesion.Puede(AccionDelSistema.CargarDocumentos))
        {
            tipos.AddRange([TipoDocumento.Incidencia, TipoDocumento.DatosEmpleado]);
        }

        if (_sesion.Puede(AccionDelSistema.PublicarResultados))
        {
            tipos.AddRange([TipoDocumento.Resultado, TipoDocumento.Ajuste]);
        }

        string[] nombres = [.. tipos.Select(t => T.Enumerado(t))];
        string? eleccion = await _dialogos.ElegirAsync(T["documentos.tipo"], nombres);

        if (eleccion is null)
        {
            return;
        }

        TipoDocumento tipo = tipos[Array.IndexOf(nombres, eleccion)];
        FileResult? archivo = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = T["documentos.seleccioneArchivo"] });

        if (archivo is null)
        {
            return;
        }

        bool exito = await EjecutarAsync(async () =>
        {
            await using Stream contenido = await archivo.OpenReadAsync();
            await _api.SubirDocumentoAsync(periodo.Id, tipo, archivo.FileName, contenido, EmpresaDe(periodo));
        });

        if (exito)
        {
            await _dialogos.MostrarAvisoAsync(T["documentos.subir"], T["documentos.recibido"]);
            await CargarAsync();
        }
    }

    /// <summary>
    /// Abre un documento: lo descarga o, si es un archivo de incidencias y el
    /// usuario puede capturarlas, ofrece importarlo al período.
    /// </summary>
    /// <param name="documento">Documento elegido.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public async Task SeleccionarDocumentoAsync(DocumentoDto? documento)
    {
        if (documento is null)
        {
            return;
        }

        if (PuedeImportar(documento))
        {
            string descargar = T["comun.descargar"];
            string importar = T["documentos.importarIncidencias"];
            string? eleccion = await _dialogos.ElegirAsync(documento.NombreArchivo, descargar, importar);

            if (eleccion == importar)
            {
                await ImportarAsync(documento);
                return;
            }

            if (eleccion != descargar)
            {
                return;
            }
        }

        await EjecutarAsync(() => DescargarAsync(documento));
    }

    /// <summary>
    /// Un archivo de incidencias disponible se importa al período sin volver a
    /// subirlo: el servidor lo lee del almacenamiento.
    /// </summary>
    private bool PuedeImportar(DocumentoDto documento)
        => documento.Tipo == TipoDocumento.Incidencia
           && documento.EsDescargable
           && _sesion.Puede(AccionDelSistema.CapturarIncidencias)
           && Periodo?.Estado != EstadoPeriodo.Cerrado;

    private async Task ImportarAsync(DocumentoDto documento)
    {
        ResultadoDeImportacionDto? resultado = null;

        bool exito = await EjecutarAsync(async () =>
            resultado = await _api.ImportarIncidenciasAsync(
                new ImportarIncidenciasRequest(documento.Id, Periodo is { } periodo ? EmpresaDe(periodo) : null)));

        if (!exito || resultado is null)
        {
            return;
        }

        string mensaje = T.Formato("incidencias.importadas", resultado.FilasLeidas, resultado.Creadas, resultado.Actualizadas);

        if (resultado.Errores.Count > 0)
        {
            mensaje += "\n\n" + T.Formato("incidencias.erroresDeImportacion", resultado.Errores.Count)
                + "\n" + string.Join("\n", resultado.Errores.Take(10));
        }

        await _dialogos.MostrarAvisoAsync(T["documentos.importarIncidencias"], mensaje);
    }

    private async Task DescargarAsync(DocumentoDto documento)
    {
        if (!documento.EsDescargable)
        {
            await _dialogos.MostrarAvisoAsync(T["documentos.titulo"], T["movil.documentoNoDisponible"]);
            return;
        }

        EnlaceDescargaResponse enlace = await _api.ObtenerEnlaceDeDescargaAsync(documento.Id);
        await Browser.Default.OpenAsync(enlace.UrlDescarga, BrowserLaunchMode.SystemPreferred);
    }

    private Guid? EmpresaDe(PeriodoDto periodo) => periodo.EmpresaId;
}

/// <summary>
/// Incidencias del período: una fila por contrato vigente.
/// </summary>
public sealed partial class IncidenciasViewModel : ViewModelBase
{
    private readonly IServicioDeApi _api;
    private readonly SesionDeLaApp _sesion;
    private readonly IServicioDeNavegacion _navegacion;
    private IReadOnlyList<IncidenciaDto> _todas = [];
    private bool _cambiandoPeriodos;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="IncidenciasViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    /// <param name="navegacion">Navegación.</param>
    public IncidenciasViewModel(Traductor traductor, IServicioDeApi api, SesionDeLaApp sesion, IServicioDeNavegacion navegacion)
        : base(traductor)
    {
        _api = api;
        _sesion = sesion;
        _navegacion = navegacion;
        Titulo = traductor["incidencias.titulo"];
    }

    /// <summary>Obtiene los períodos abiertos.</summary>
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
    /// <value>El primero abierto por defecto.</value>
    [ObservableProperty]
    public partial PeriodoDto? Periodo { get; set; }

    /// <summary>Obtiene o establece las filas visibles.</summary>
    /// <value>Se sustituye de una vez para no emitir miles de notificaciones.</value>
    [ObservableProperty]
    public partial IReadOnlyList<IncidenciaDto> Filas { get; set; } = [];

    /// <summary>Obtiene o establece el texto de búsqueda.</summary>
    /// <value>Filtra por clave, nombre o razón social.</value>
    [ObservableProperty]
    public partial string? Texto { get; set; }

    /// <summary>Obtiene o establece el resumen de captura.</summary>
    /// <value>Por ejemplo «3 de 40 contratos con captura».</value>
    [ObservableProperty]
    public partial string? Resumen { get; set; }

    /// <summary>
    /// Carga los períodos y las filas del elegido.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarAsync() => EjecutarAsync(async () =>
    {
        Titulo = T["incidencias.titulo"];

        if (_sesion.FaltaEmpresa)
        {
            Periodos.Clear();
            _todas = [];
            Filtrar();
            MensajeDeError = T["comun.seleccioneEmpresaDetalle"];
            return;
        }

        IReadOnlyList<PeriodoDto> periodos = await _api.ListarPeriodosAsync(_sesion.EmpresaDeConsulta, incluirCerrados: false);

        _cambiandoPeriodos = true;
        Guid? anterior = Periodo?.Id;
        Reemplazar(Periodos, periodos);
        Reemplazar(NombresDePeriodos, periodos.Select(p => $"{p.Clave} · {p.Descripcion}"));
        Periodo = Periodos.FirstOrDefault(p => p.Id == anterior) ?? Periodos.FirstOrDefault();
        _cambiandoPeriodos = false;
        OnPropertyChanged(nameof(IndiceDePeriodo));

        await CargarFilasInternoAsync();
    });

    /// <summary>
    /// Recarga las filas del período elegido.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public Task CargarFilasAsync() => EjecutarAsync(CargarFilasInternoAsync);

    /// <summary>
    /// Abre la captura de una fila.
    /// </summary>
    /// <param name="fila">Fila elegida.</param>
    /// <returns>Una tarea que representa la navegación.</returns>
    [RelayCommand]
    public Task EditarAsync(IncidenciaDto? fila)
        => fila is null
            ? Task.CompletedTask
            : _navegacion.IrAAsync(AppShell.RutaIncidencia, new Dictionary<string, object> { ["incidencia"] = fila });

    private async Task CargarFilasInternoAsync()
    {
        _todas = Periodo is { } periodo
            ? await _api.ListarIncidenciasAsync(periodo.Id, periodo.EmpresaId)
            : [];
        Filtrar();
    }

    private void Filtrar()
    {
        string? texto = Texto?.Trim();

        Filas = string.IsNullOrEmpty(texto)
            ? _todas
            : [.. _todas.Where(f => f.ClaveEmpleado.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || f.NombreEmpleado.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || f.RazonSocialNombre.Contains(texto, StringComparison.OrdinalIgnoreCase))];

        Resumen = T.Formato("incidencias.resumen", _todas.Count(f => f.Id is not null), _todas.Count);
    }

    partial void OnPeriodoChanged(PeriodoDto? value)
    {
        OnPropertyChanged(nameof(IndiceDePeriodo));

        if (!_cambiandoPeriodos)
        {
            CargarFilasCommand.Execute(null);
        }
    }

    partial void OnTextoChanged(string? value) => Filtrar();
}

/// <summary>
/// Captura de la incidencia de un contrato.
/// </summary>
public sealed partial class IncidenciaViewModel : ViewModelBase, IQueryAttributable
{
    private readonly IServicioDeApi _api;
    private readonly IServicioDeDialogos _dialogos;
    private readonly IServicioDeNavegacion _navegacion;
    private readonly SesionDeLaApp _sesion;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="IncidenciaViewModel"/>.
    /// </summary>
    /// <param name="traductor">Traductor.</param>
    /// <param name="api">Cliente de la API.</param>
    /// <param name="dialogos">Diálogos.</param>
    /// <param name="navegacion">Navegación.</param>
    /// <param name="sesion">Estado de la sesión.</param>
    public IncidenciaViewModel(
        Traductor traductor, IServicioDeApi api, IServicioDeDialogos dialogos, IServicioDeNavegacion navegacion, SesionDeLaApp sesion)
        : base(traductor)
    {
        _api = api;
        _dialogos = dialogos;
        _navegacion = navegacion;
        _sesion = sesion;
        Titulo = traductor["incidencias.editar"];
    }

    /// <summary>Obtiene o establece la fila capturada.</summary>
    /// <value>Llega como parámetro de navegación.</value>
    [ObservableProperty]
    public partial IncidenciaDto? Incidencia { get; set; }

    /// <summary>Obtiene los valores editables.</summary>
    /// <value>Se enlaza a los campos del formulario.</value>
    [ObservableProperty]
    public partial ModeloIncidencia Modelo { get; set; } = new();

    /// <summary>Obtiene los tipos de movimiento.</summary>
    /// <value>Ordinaria y finiquito.</value>
    public IReadOnlyList<TipoDeMovimiento> TiposDeMovimiento { get; } = Enum.GetValues<TipoDeMovimiento>();

    /// <summary>Obtiene los nombres traducidos de los tipos de movimiento.</summary>
    /// <value>En el mismo orden que <see cref="TiposDeMovimiento"/>.</value>
    public IReadOnlyList<string> NombresDeMovimientos => [.. TiposDeMovimiento.Select(t => T.Enumerado(t))];

    /// <summary>Obtiene o establece la posición del tipo de movimiento.</summary>
    /// <value>0 ordinaria, 1 finiquito.</value>
    public int IndiceDeMovimiento
    {
        get => TiposDeMovimiento.ToList().IndexOf(Modelo.TipoDeMovimiento);
        set
        {
            if (value >= 0 && value < TiposDeMovimiento.Count)
            {
                Modelo.TipoDeMovimiento = TiposDeMovimiento[value];
            }
        }
    }

    /// <summary>Indica si hay una captura que se pueda restablecer.</summary>
    /// <value><c>true</c> si la fila ya tenía incidencia guardada.</value>
    public bool PuedeRestablecer => Incidencia?.Id is not null;

    /// <inheritdoc/>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.TryGetValue("incidencia", out object? valor) && valor is IncidenciaDto fila)
        {
            Incidencia = fila;
            Modelo = ModeloIncidencia.Desde(fila);
            OnPropertyChanged(nameof(IndiceDeMovimiento));
            Titulo = $"{fila.ClaveEmpleado} · {fila.NombreEmpleado}";
            OnPropertyChanged(nameof(PuedeRestablecer));
        }
    }

    /// <summary>
    /// Guarda la captura y vuelve a la lista.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public async Task GuardarAsync()
    {
        if (Incidencia is not { } fila)
        {
            return;
        }

        if (!Modelo.IntentarLeerIsr(out decimal? isr))
        {
            MensajeDeError = T["movil.valorInvalido"];
            return;
        }

        bool exito = await EjecutarAsync(() => _api.GuardarIncidenciaAsync(Modelo.ARequest(fila.PeriodoId, fila.ContratoId, _sesion.EmpresaDeConsulta, isr)));

        if (exito)
        {
            await _navegacion.VolverAsync();
        }
    }

    /// <summary>
    /// Elimina la captura para volver a período completo.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    [RelayCommand]
    public async Task RestablecerAsync()
    {
        if (Incidencia is not { Id: { } incidenciaId } fila)
        {
            return;
        }

        string boton = T["incidencias.restablecer"];

        if (!await _dialogos.ConfirmarAsync(boton, T.Formato("incidencias.confirmarRestablecer", fila.NombreEmpleado), boton))
        {
            return;
        }

        if (await EjecutarAsync(() => _api.EliminarIncidenciaAsync(incidenciaId, _sesion.EmpresaDeConsulta)))
        {
            await _navegacion.VolverAsync();
        }
    }
}

/// <summary>
/// Valores editables de una incidencia.
/// </summary>
public sealed class ModeloIncidencia
{
    /// <summary>Días del período.</summary>
    public decimal DiasPeriodo { get; set; }

    /// <summary>Días de vacaciones.</summary>
    public decimal Vacaciones { get; set; }

    /// <summary>Faltas.</summary>
    public decimal Ausentismos { get; set; }

    /// <summary>Días de incapacidad.</summary>
    public decimal Incapacidades { get; set; }

    /// <summary>Festivos trabajados.</summary>
    public decimal Festivos { get; set; }

    /// <summary>Horas dobles.</summary>
    public decimal HorasDobles { get; set; }

    /// <summary>Horas triples.</summary>
    public decimal HorasTriples { get; set; }

    /// <summary>Domingos trabajados.</summary>
    public decimal DomingosTrabajados { get; set; }

    /// <summary>Gratificación o bonos.</summary>
    public decimal Gratificacion { get; set; }

    /// <summary>Reembolsos.</summary>
    public decimal Reembolsos { get; set; }

    /// <summary>Apoyo de teletrabajo.</summary>
    public decimal Teletrabajo { get; set; }

    /// <summary>Finiquito.</summary>
    public decimal Finiquito { get; set; }

    /// <summary>Cafetería.</summary>
    public decimal Cafeteria { get; set; }

    /// <summary>Horas descontadas.</summary>
    public decimal HorasDescontadas { get; set; }

    /// <summary>Otros descuentos.</summary>
    public decimal OtrosDescuentos { get; set; }

    /// <summary>Préstamo personal.</summary>
    public decimal PrestamoPersonal { get; set; }

    /// <summary>Aguinaldo.</summary>
    public decimal Aguinaldo { get; set; }

    /// <summary>Descuentos fiscales.</summary>
    public decimal DescuentosFiscales { get; set; }

    /// <summary>FONACOT capturado.</summary>
    public decimal FonacotCapturado { get; set; }

    /// <summary>Descuento sindical adicional.</summary>
    public decimal DescuentoSindicalAdicional { get; set; }

    /// <summary>Ajuste sindical.</summary>
    public decimal AjusteSindical { get; set; }

    /// <summary>ISR manual como texto; vacío para que lo calcule el sistema.</summary>
    public string? IsrManualTexto { get; set; }

    /// <summary>Tipo de movimiento.</summary>
    public TipoDeMovimiento TipoDeMovimiento { get; set; }

    /// <summary>Observaciones.</summary>
    public string? Observaciones { get; set; }

    /// <summary>
    /// Crea el modelo a partir de una fila.
    /// </summary>
    /// <param name="d">Fila.</param>
    /// <returns>El modelo.</returns>
    public static ModeloIncidencia Desde(IncidenciaDto d)
    {
        ArgumentNullException.ThrowIfNull(d);

        return new ModeloIncidencia
        {
            DiasPeriodo = d.DiasPeriodo,
            Vacaciones = d.Vacaciones,
            Ausentismos = d.Ausentismos,
            Incapacidades = d.Incapacidades,
            Festivos = d.Festivos,
            HorasDobles = d.HorasDobles,
            HorasTriples = d.HorasTriples,
            DomingosTrabajados = d.DomingosTrabajados,
            Gratificacion = d.Gratificacion,
            Reembolsos = d.Reembolsos,
            Teletrabajo = d.Teletrabajo,
            Finiquito = d.Finiquito,
            Cafeteria = d.Cafeteria,
            HorasDescontadas = d.HorasDescontadas,
            OtrosDescuentos = d.OtrosDescuentos,
            PrestamoPersonal = d.PrestamoPersonal,
            Aguinaldo = d.Aguinaldo,
            DescuentosFiscales = d.DescuentosFiscales,
            FonacotCapturado = d.FonacotCapturado,
            DescuentoSindicalAdicional = d.DescuentoSindicalAdicional,
            AjusteSindical = d.AjusteSindical,
            IsrManualTexto = d.IsrManual?.ToString(CultureInfo.InvariantCulture),
            TipoDeMovimiento = d.TipoDeMovimiento,
            Observaciones = d.Observaciones,
        };
    }

    /// <summary>
    /// Interpreta el ISR manual.
    /// </summary>
    /// <param name="isr">Valor leído, o <c>null</c> si está vacío.</param>
    /// <returns><c>false</c> si el texto no es un número.</returns>
    public bool IntentarLeerIsr(out decimal? isr)
    {
        isr = null;

        if (string.IsNullOrWhiteSpace(IsrManualTexto))
        {
            return true;
        }

        string texto = IsrManualTexto.Trim().Replace(',', '.');

        if (decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal valor))
        {
            isr = valor;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Arma la petición para la API.
    /// </summary>
    /// <param name="periodoId">Período.</param>
    /// <param name="contratoId">Contrato.</param>
    /// <param name="empresaId">Empresa de trabajo, o <c>null</c>.</param>
    /// <param name="isr">ISR manual ya interpretado.</param>
    /// <returns>La petición.</returns>
    public GuardarIncidenciaRequest ARequest(Guid periodoId, Guid contratoId, Guid? empresaId, decimal? isr) => new(
        periodoId, contratoId, empresaId, DiasPeriodo, Vacaciones, Ausentismos, Incapacidades, Festivos,
        HorasDobles, HorasTriples, DomingosTrabajados, Gratificacion, Reembolsos, Teletrabajo, Finiquito,
        Cafeteria, HorasDescontadas, OtrosDescuentos, PrestamoPersonal, Aguinaldo, DescuentosFiscales,
        FonacotCapturado, DescuentoSindicalAdicional, AjusteSindical, isr, TipoDeMovimiento,
        string.IsNullOrWhiteSpace(Observaciones) ? null : Observaciones.Trim());
}
