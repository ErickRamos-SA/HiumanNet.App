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
using HuimanNet.Domain.Services;

namespace HuimanNet.App.ViewModels;

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
    public bool PuedeSubir => TiposQuePuedeSubir.Count > 0;

    /// <summary>
    /// Obtiene los tipos que el usuario puede subir: los que admite su rol y
    /// para los que tiene habilitada la acción de carga.
    /// </summary>
    /// <value>La misma regla que aplica el servidor y la web; vacía sin sesión.</value>
    private IReadOnlyList<TipoDocumento> TiposQuePuedeSubir
        => _sesion.Usuario is { } usuario ? PoliticaDeAcceso.TiposQuePuedeCargar(usuario.Rol, usuario.Acciones) : [];

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

        IReadOnlyList<TipoDocumento> tipos = TiposQuePuedeSubir;

        if (tipos.Count == 0)
        {
            return;
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
    /// <param name="documento">Documento elegido.</param>
    /// <returns>
    /// <c>true</c> si es un archivo de incidencias disponible, el usuario captura
    /// incidencias y el período no está cerrado.
    /// </returns>
    private bool PuedeImportar(DocumentoDto documento)
        => documento.Tipo == TipoDocumento.Incidencia
           && documento.EsDescargable
           && _sesion.Puede(AccionDelSistema.CapturarIncidencias)
           && Periodo?.Estado != EstadoPeriodo.Cerrado;

    /// <summary>Importa las incidencias del documento y muestra el resumen con los primeros errores.</summary>
    /// <param name="documento">Archivo de incidencias disponible.</param>
    /// <returns>Tarea que finaliza al mostrar el resumen.</returns>
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

    /// <summary>
    /// Pide un enlace temporal y lo abre en el navegador del sistema; avisa si
    /// el documento todavía no está disponible.
    /// </summary>
    /// <param name="documento">Documento elegido.</param>
    /// <returns>Tarea que finaliza al abrir el enlace.</returns>
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

    /// <summary>Obtiene la empresa que se envía a la API para un período.</summary>
    /// <param name="periodo">Período.</param>
    /// <returns>La empresa del período.</returns>
    private Guid? EmpresaDe(PeriodoDto periodo) => periodo.EmpresaId;
}
