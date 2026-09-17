using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using HuimanNet.Contracts;
using HuimanNet.Contracts.Catalogos;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Contracts.Empleados;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Contracts.Incidencias;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Contracts.Serialization;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace HuimanNet.App.Services;

/// <summary>
/// Implementación HTTP de <see cref="IServicioDeApi"/>.
/// </summary>
/// <remarks>
/// El <see cref="HttpClient"/> llega configurado desde <c>MauiProgram</c> con
/// la dirección base, el manejador que añade el token y la política de
/// reintentos. Los enlaces firmados del almacenamiento local llegan relativos
/// a la API (en Azure son absolutos): aquí se resuelven contra la dirección
/// base para que el flujo sea idéntico en los dos entornos.
/// </remarks>
public sealed class ServicioDeApi : IServicioDeApi
{
    private static readonly HuimanNetJsonContext Json = HuimanNetJsonContext.Default;

    private readonly HttpClient _http;
    private readonly ILogger<ServicioDeApi> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ServicioDeApi"/>.
    /// </summary>
    /// <param name="http">Cliente HTTP configurado.</param>
    /// <param name="logger">Registro de eventos.</param>
    public ServicioDeApi(HttpClient http, ILogger<ServicioDeApi> logger)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<ConfiguracionPublicaDto> ObtenerConfiguracionAsync(CancellationToken cancellationToken = default)
        => ObtenerAsync(RutasApi.Configuracion, Json.ConfiguracionPublicaDto, cancellationToken);

    /// <inheritdoc/>
    public Task<IniciarSesionResponse> IniciarSesionLocalAsync(IniciarSesionRequest peticion, CancellationToken cancellationToken = default)
        => EnviarAsync(HttpMethod.Post, RutasApi.IniciarSesion, peticion, Json.IniciarSesionRequest, Json.IniciarSesionResponse, cancellationToken);

    /// <inheritdoc/>
    public Task<UsuarioActualDto> ObtenerUsuarioActualAsync(CancellationToken cancellationToken = default)
        => ObtenerAsync(RutasApi.UsuarioActual, Json.UsuarioActualDto, cancellationToken);

    /// <inheritdoc/>
    public Task CambiarContrasenaAsync(CambiarContrasenaRequest peticion, CancellationToken cancellationToken = default)
        => EnviarSinRespuestaAsync(HttpMethod.Put, RutasApi.CambiarContrasena, peticion, Json.CambiarContrasenaRequest, cancellationToken);

    /// <inheritdoc/>
    public Task ActualizarPreferenciasAsync(ActualizarPreferenciasRequest peticion, CancellationToken cancellationToken = default)
        => EnviarSinRespuestaAsync(HttpMethod.Put, RutasApi.Preferencias, peticion, Json.ActualizarPreferenciasRequest, cancellationToken);

    /// <inheritdoc/>
    public Task<ResumenDeInicioDto> ObtenerResumenDeInicioAsync(Guid? empresaId, CancellationToken cancellationToken = default)
        => ObtenerAsync(Ruta(RutasApi.Inicio, ("empresaId", Id(empresaId))), Json.ResumenDeInicioDto, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<EmpresaDto>> ListarEmpresasAsync(CancellationToken cancellationToken = default)
        => ObtenerAsync($"{RutasApi.Empresas}?soloActivas=true", Json.IReadOnlyListEmpresaDto, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<PeriodoDto>> ListarPeriodosAsync(Guid? empresaId, bool incluirCerrados, CancellationToken cancellationToken = default)
        => ObtenerAsync(
            Ruta(RutasApi.Periodos, ("incluirCerrados", incluirCerrados ? "true" : "false"), ("empresaId", Id(empresaId))),
            Json.IReadOnlyListPeriodoDto,
            cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<DocumentoDto>> ListarDocumentosAsync(Guid periodoId, Guid? empresaId, CancellationToken cancellationToken = default)
        => ObtenerAsync(Ruta(RutasApi.DocumentosDePeriodo(periodoId), ("empresaId", Id(empresaId))), Json.IReadOnlyListDocumentoDto, cancellationToken);

    /// <inheritdoc/>
    public async Task<DocumentoDto> SubirDocumentoAsync(
        Guid periodoId, TipoDocumento tipo, string nombreArchivo, Stream contenido, Guid? empresaId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contenido);

        // Se materializa para calcular la huella y subir sin releer el flujo,
        // que en móvil puede no admitir búsqueda.
        using var memoria = new MemoryStream();
        await contenido.CopyToAsync(memoria, cancellationToken);
        byte[] bytes = memoria.ToArray();

        // 1. El servidor autoriza y firma; todavía no existe ningún byte en el almacén.
        SolicitarCargaResponse autorizacion = await EnviarAsync(
            HttpMethod.Post,
            $"{RutasApi.Documentos}/solicitar-carga",
            new SolicitarCargaRequest(periodoId, tipo, nombreArchivo, bytes.LongLength, empresaId),
            Json.SolicitarCargaRequest,
            Json.SolicitarCargaResponse,
            cancellationToken);

        // 2. El dispositivo sube el archivo directo al almacenamiento.
        await SubirAlAlmacenAsync(autorizacion, bytes, cancellationToken);

        // 3. Se confirma con la huella para que el servidor verifique la integridad.
        HuellaArchivo huella = HuellaArchivo.DesdeBytes(SHA256.HashData(bytes));

        DocumentoDto documento = await EnviarAsync(
            HttpMethod.Post,
            RutasApi.ConfirmarCarga(autorizacion.DocumentoId),
            new ConfirmarCargaRequest(huella.ValorHex),
            Json.ConfirmarCargaRequest,
            Json.DocumentoDto,
            cancellationToken);

        _logger.LogInformation("Documento {DocumentoId} subido y confirmado.", autorizacion.DocumentoId);
        return documento;
    }

    /// <inheritdoc/>
    public async Task<EnlaceDescargaResponse> ObtenerEnlaceDeDescargaAsync(Guid documentoId, CancellationToken cancellationToken = default)
    {
        EnlaceDescargaResponse enlace = await ObtenerAsync(RutasApi.EnlaceDescarga(documentoId), Json.EnlaceDescargaResponse, cancellationToken);
        return enlace with { UrlDescarga = Absoluta(enlace.UrlDescarga) };
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<IncidenciaDto>> ListarIncidenciasAsync(Guid periodoId, Guid? empresaId, CancellationToken cancellationToken = default)
        => ObtenerAsync(Ruta(RutasApi.IncidenciasDePeriodo(periodoId), ("empresaId", Id(empresaId))), Json.IReadOnlyListIncidenciaDto, cancellationToken);

    /// <inheritdoc/>
    public Task<IncidenciaDto> GuardarIncidenciaAsync(GuardarIncidenciaRequest peticion, CancellationToken cancellationToken = default)
        => EnviarAsync(HttpMethod.Put, RutasApi.Incidencias, peticion, Json.GuardarIncidenciaRequest, Json.IncidenciaDto, cancellationToken);

    /// <inheritdoc/>
    public async Task EliminarIncidenciaAsync(Guid incidenciaId, Guid? empresaId, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage respuesta = await _http.DeleteAsync(
            Ruta(RutasApi.Recurso(RutasApi.Incidencias, incidenciaId), ("empresaId", Id(empresaId))), cancellationToken);
        await GarantizarExitoAsync(respuesta, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ResultadoDeImportacionDto> ImportarIncidenciasAsync(ImportarIncidenciasRequest peticion, CancellationToken cancellationToken = default)
        => EnviarAsync(HttpMethod.Post, RutasApi.ImportarIncidencias, peticion, Json.ImportarIncidenciasRequest, Json.ResultadoDeImportacionDto, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<CorridaDeNominaDto>> ListarCorridasAsync(Guid periodoId, Guid? empresaId, CancellationToken cancellationToken = default)
        => ObtenerAsync(Ruta(RutasApi.CorridasDePeriodo(periodoId), ("empresaId", Id(empresaId))), Json.IReadOnlyListCorridaDeNominaDto, cancellationToken);

    /// <inheritdoc/>
    public Task<CorridaDeNominaDto> CalcularNominaAsync(CalcularNominaRequest peticion, CancellationToken cancellationToken = default)
        => EnviarAsync(HttpMethod.Post, RutasApi.CalcularNomina, peticion, Json.CalcularNominaRequest, Json.CorridaDeNominaDto, cancellationToken);

    /// <inheritdoc/>
    public Task<ResumenDeCorridaDto> ObtenerResumenDeCorridaAsync(Guid corridaId, Guid? empresaId, CancellationToken cancellationToken = default)
        => ObtenerAsync(Ruta(RutasApi.ResumenDeCorrida(corridaId), ("empresaId", Id(empresaId))), Json.ResumenDeCorridaDto, cancellationToken);

    /// <inheritdoc/>
    public Task<DetalleDeResultadoDto> ObtenerDetalleDeResultadoAsync(Guid resultadoId, Guid? empresaId, CancellationToken cancellationToken = default)
        => ObtenerAsync(Ruta(RutasApi.DetalleDeResultado(resultadoId), ("empresaId", Id(empresaId))), Json.DetalleDeResultadoDto, cancellationToken);

    /// <inheritdoc/>
    public Task CambiarEstadoCorridaAsync(Guid corridaId, CambiarEstadoCorridaRequest peticion, CancellationToken cancellationToken = default)
        => EnviarSinRespuestaAsync(HttpMethod.Put, RutasApi.EstadoDeCorrida(corridaId), peticion, Json.CambiarEstadoCorridaRequest, cancellationToken);

    /// <inheritdoc/>
    public async Task<ArchivoDescargado> ExportarCorridaAsync(Guid corridaId, Guid? empresaId, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage respuesta = await _http.GetAsync(
            Ruta(RutasApi.ExportarCorrida(corridaId), ("empresaId", Id(empresaId))), cancellationToken);
        await GarantizarExitoAsync(respuesta, cancellationToken);

        byte[] contenido = await respuesta.Content.ReadAsByteArrayAsync(cancellationToken);
        string nombre = respuesta.Content.Headers.ContentDisposition?.FileNameStar
            ?? respuesta.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? string.Create(CultureInfo.InvariantCulture, $"nomina-{corridaId:N}.csv");
        string tipo = respuesta.Content.Headers.ContentType?.MediaType ?? "text/csv";

        return new ArchivoDescargado(nombre, tipo, contenido);
    }

    /// <inheritdoc/>
    public Task<PaginaDto<EmpleadoResumenDto>> ListarEmpleadosAsync(
        Guid? empresaId, string? texto, int pagina, int tamanoPagina, CancellationToken cancellationToken = default)
        => ObtenerAsync(
            Ruta(
                RutasApi.Empleados,
                ("empresaId", Id(empresaId)),
                ("soloActivos", "true"),
                ("texto", string.IsNullOrWhiteSpace(texto) ? null : texto.Trim()),
                ("pagina", pagina.ToString(CultureInfo.InvariantCulture)),
                ("tamanoPagina", tamanoPagina.ToString(CultureInfo.InvariantCulture))),
            Json.PaginaDtoEmpleadoResumenDto,
            cancellationToken);

    /// <inheritdoc/>
    public Task<EmpleadoDto> ObtenerEmpleadoAsync(Guid empleadoId, Guid? empresaId, CancellationToken cancellationToken = default)
        => ObtenerAsync(Ruta(RutasApi.Recurso(RutasApi.Empleados, empleadoId), ("empresaId", Id(empresaId))), Json.EmpleadoDto, cancellationToken);

    /// <inheritdoc/>
    public Task<ExplicacionCompletaDto> ObtenerExplicacionAsync(
        EsquemaDePago esquema, Idioma idioma, Guid? empresaId, CancellationToken cancellationToken = default)
        => ObtenerAsync(
            Ruta(RutasApi.ExplicacionCompleta, ("esquema", esquema.ToString()), ("idioma", idioma.ToString()), ("empresaId", Id(empresaId))),
            Json.ExplicacionCompletaDto,
            cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<UsuarioDto>> ListarUsuariosAsync(Guid? empresaId, bool incluirInactivos, CancellationToken cancellationToken = default)
        => ObtenerAsync(
            Ruta(RutasApi.Usuarios, ("empresaId", Id(empresaId)), ("incluirInactivos", incluirInactivos ? "true" : "false")),
            Json.IReadOnlyListUsuarioDto,
            cancellationToken);

    /// <inheritdoc/>
    public Task<ProbarFormulaResponse> ProbarFormulaAsync(ProbarFormulaRequest peticion, CancellationToken cancellationToken = default)
        => EnviarAsync(HttpMethod.Post, RutasApi.ProbarFormula, peticion, Json.ProbarFormulaRequest, Json.ProbarFormulaResponse, cancellationToken);

    // ---- Infraestructura ----------------------------------------------------

    /// <summary>Hace un GET y lee la respuesta JSON.</summary>
    /// <typeparam name="TRespuesta">Tipo de la respuesta.</typeparam>
    /// <param name="ruta">Ruta relativa, con la cadena de consulta.</param>
    /// <param name="tipo">Metadatos de serialización de la respuesta.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La respuesta deserializada.</returns>
    /// <exception cref="ErrorDeApiException">Se lanza si la API responde con error o sin cuerpo.</exception>
    private async Task<TRespuesta> ObtenerAsync<TRespuesta>(
        string ruta, JsonTypeInfo<TRespuesta> tipo, CancellationToken cancellationToken)
    {
        using HttpResponseMessage respuesta = await _http.GetAsync(ruta, cancellationToken);
        await GarantizarExitoAsync(respuesta, cancellationToken);

        return await respuesta.Content.ReadFromJsonAsync(tipo, cancellationToken)
            ?? throw new ErrorDeApiException("La API devolvió una respuesta vacía.", respuesta.StatusCode);
    }

    /// <summary>Envía una petición JSON y lee la respuesta JSON.</summary>
    /// <typeparam name="TPeticion">Tipo del cuerpo.</typeparam>
    /// <typeparam name="TRespuesta">Tipo de la respuesta.</typeparam>
    /// <param name="metodo">Método HTTP.</param>
    /// <param name="ruta">Ruta relativa.</param>
    /// <param name="peticion">Cuerpo de la petición.</param>
    /// <param name="tipoPeticion">Metadatos de serialización del cuerpo.</param>
    /// <param name="tipoRespuesta">Metadatos de serialización de la respuesta.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La respuesta deserializada.</returns>
    /// <exception cref="ErrorDeApiException">Se lanza si la API responde con error o sin cuerpo.</exception>
    private async Task<TRespuesta> EnviarAsync<TPeticion, TRespuesta>(
        HttpMethod metodo, string ruta, TPeticion peticion, JsonTypeInfo<TPeticion> tipoPeticion,
        JsonTypeInfo<TRespuesta> tipoRespuesta, CancellationToken cancellationToken)
    {
        using var mensaje = new HttpRequestMessage(metodo, ruta) { Content = JsonContent.Create(peticion, tipoPeticion) };
        using HttpResponseMessage respuesta = await _http.SendAsync(mensaje, cancellationToken);
        await GarantizarExitoAsync(respuesta, cancellationToken);

        return await respuesta.Content.ReadFromJsonAsync(tipoRespuesta, cancellationToken)
            ?? throw new ErrorDeApiException("La API devolvió una respuesta vacía.", respuesta.StatusCode);
    }

    /// <summary>Envía una petición JSON que no devuelve cuerpo.</summary>
    /// <typeparam name="TPeticion">Tipo del cuerpo.</typeparam>
    /// <param name="metodo">Método HTTP.</param>
    /// <param name="ruta">Ruta relativa.</param>
    /// <param name="peticion">Cuerpo de la petición.</param>
    /// <param name="tipoPeticion">Metadatos de serialización del cuerpo.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Tarea que finaliza al recibir la respuesta.</returns>
    /// <exception cref="ErrorDeApiException">Se lanza si la API responde con error.</exception>
    private async Task EnviarSinRespuestaAsync<TPeticion>(
        HttpMethod metodo, string ruta, TPeticion peticion, JsonTypeInfo<TPeticion> tipoPeticion, CancellationToken cancellationToken)
    {
        using var mensaje = new HttpRequestMessage(metodo, ruta) { Content = JsonContent.Create(peticion, tipoPeticion) };
        using HttpResponseMessage respuesta = await _http.SendAsync(mensaje, cancellationToken);
        await GarantizarExitoAsync(respuesta, cancellationToken);
    }

    /// <summary>
    /// Sube el archivo directo al almacenamiento con la URL firmada, sin el
    /// token de la API.
    /// </summary>
    /// <param name="autorizacion">URL firmada y tipo de blob que devolvió la API.</param>
    /// <param name="bytes">Contenido del archivo.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Tarea que finaliza al subir el archivo.</returns>
    /// <exception cref="ErrorDeApiException">Se lanza si el almacenamiento rechaza la carga.</exception>
    private async Task SubirAlAlmacenAsync(SolicitarCargaResponse autorizacion, byte[] bytes, CancellationToken cancellationToken)
    {
        // Cliente aparte, sin el token de la API: la URL firmada ya es la credencial.
        using var cliente = new HttpClient();
        using var contenido = new ByteArrayContent(bytes);
        contenido.Headers.Add("x-ms-blob-type", autorizacion.EncabezadoTipoBlob);

        using HttpResponseMessage respuesta = await cliente.PutAsync(Absoluta(autorizacion.UrlCarga), contenido, cancellationToken);

        if (!respuesta.IsSuccessStatusCode)
        {
            throw new ErrorDeApiException(
                string.Create(CultureInfo.InvariantCulture, $"El almacenamiento rechazó la carga (HTTP {(int)respuesta.StatusCode})."),
                respuesta.StatusCode);
        }
    }

    /// <summary>
    /// Convierte una URL relativa (la del almacén local) en absoluta respecto
    /// de la dirección de la API.
    /// </summary>
    /// <param name="url">URL devuelta por la API.</param>
    /// <returns>La URL absoluta.</returns>
    /// <exception cref="InvalidOperationException">Se lanza si el cliente HTTP no tiene dirección base.</exception>
    private Uri Absoluta(Uri url)
        => url.IsAbsoluteUri ? url : new Uri(_http.BaseAddress ?? throw new InvalidOperationException("El cliente HTTP no tiene dirección base."), url);

    /// <summary>Da formato a un identificador opcional para la cadena de consulta.</summary>
    /// <param name="id">Identificador.</param>
    /// <returns>El texto, o <c>null</c> para omitir el parámetro.</returns>
    private static string? Id(Guid? id) => id?.ToString();

    /// <summary>Añade a una ruta los parámetros de consulta que tienen valor.</summary>
    /// <param name="ruta">Ruta relativa.</param>
    /// <param name="parametros">Nombres y valores; los <c>null</c> se omiten.</param>
    /// <returns>La ruta con los valores escapados.</returns>
    private static string Ruta(string ruta, params (string Nombre, string? Valor)[] parametros)
    {
        var constructor = new StringBuilder(ruta);
        char separador = ruta.Contains('?', StringComparison.Ordinal) ? '&' : '?';

        foreach ((string nombre, string? valor) in parametros)
        {
            if (valor is null)
            {
                continue;
            }

            constructor.Append(separador).Append(nombre).Append('=').Append(Uri.EscapeDataString(valor));
            separador = '&';
        }

        return constructor.ToString();
    }

    /// <summary>
    /// Convierte una respuesta de error en <see cref="ErrorDeApiException"/> con
    /// el mensaje de <c>ProblemDetails</c> que redactó el servidor.
    /// </summary>
    /// <param name="respuesta">Respuesta de la API.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Tarea que finaliza sin error si la respuesta fue correcta.</returns>
    /// <exception cref="ErrorDeApiException">Se lanza si la respuesta tiene un código de error.</exception>
    private static async Task GarantizarExitoAsync(HttpResponseMessage respuesta, CancellationToken cancellationToken)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return;
        }

        string cuerpo = await respuesta.Content.ReadAsStringAsync(cancellationToken);
        string? mensaje = null;

        if (!string.IsNullOrWhiteSpace(cuerpo))
        {
            try
            {
                using JsonDocument documento = JsonDocument.Parse(cuerpo);
                JsonElement raiz = documento.RootElement;

                if (raiz.ValueKind == JsonValueKind.Object)
                {
                    mensaje = raiz.TryGetProperty("detail", out JsonElement detalle) && detalle.ValueKind == JsonValueKind.String
                        ? detalle.GetString()
                        : raiz.TryGetProperty("title", out JsonElement titulo) && titulo.ValueKind == JsonValueKind.String
                            ? titulo.GetString()
                            : null;
                }
            }
            catch (JsonException)
            {
                mensaje = cuerpo.Length > 300 ? cuerpo[..300] : cuerpo;
            }
        }

        throw new ErrorDeApiException(
            mensaje ?? string.Create(CultureInfo.InvariantCulture, $"La API devolvió HTTP {(int)respuesta.StatusCode}."),
            respuesta.StatusCode == 0 ? HttpStatusCode.InternalServerError : respuesta.StatusCode);
    }
}
