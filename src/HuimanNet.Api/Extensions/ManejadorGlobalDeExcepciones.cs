using HuimanNet.Application.Common;
using HuimanNet.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HuimanNet.Api.Extensions;

/// <summary>
/// Traduce las excepciones de dominio a respuestas <c>ProblemDetails</c>
/// (RFC 7807).
/// </summary>
/// <remarks>
/// Punto único de traducción de errores. Dos decisiones deliberadas:
/// <list type="bullet">
///   <item><description>
///     <see cref="AccesoNoAutorizadoException"/> devuelve <c>404 Not Found</c>,
///     no <c>403 Forbidden</c>: confirmar que un recurso existe pero es ajeno ya
///     sería una filtración entre empresas.
///   </description></item>
///   <item><description>
///     El motivo técnico se registra en el log, nunca se devuelve al cliente.
///   </description></item>
/// </list>
/// </remarks>
public sealed class ManejadorGlobalDeExcepciones : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ManejadorGlobalDeExcepciones> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ManejadorGlobalDeExcepciones"/>.
    /// </summary>
    /// <param name="problemDetails">Servicio que escribe la respuesta de error.</param>
    /// <param name="logger">Registro de eventos.</param>
    public ManejadorGlobalDeExcepciones(
        IProblemDetailsService problemDetails, ILogger<ManejadorGlobalDeExcepciones> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        (int estado, string titulo, string detalle) = Traducir(exception);

        if (estado >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Error no controlado al procesar {Ruta}.", httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                "Petición rechazada en {Ruta}: {Motivo}", httpContext.Request.Path, exception.Message);
        }

        httpContext.Response.StatusCode = estado;

        var problema = new ProblemDetails
        {
            Status = estado,
            Title = titulo,
            Detail = detalle,
            Instance = httpContext.Request.Path,
        };

        // El código de error viaja en 'type' y no en 'extensions': el diccionario
        // de extensiones serializa valores de tipo object, para los que la
        // compilación AOT no puede generar metadatos.
        if (exception is DomainException dominio)
        {
            problema.Type = $"https://huimannet.app/errores/{dominio.Codigo}";
        }

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problema,
        });
    }

    /// <summary>Traduce una excepción a la respuesta de problema que recibe el cliente.</summary>
    /// <param name="exception">Excepción no controlada.</param>
    /// <returns>Estado HTTP, título y detalle público del problema.</returns>
    private static (int Estado, string Titulo, string Detalle) Traducir(Exception exception)
        => exception switch
        {
            EntradaInvalidaException invalida => (
                StatusCodes.Status400BadRequest,
                "Solicitud no válida",
                string.Join(
                    " ",
                    invalida.Errores.Select(static error => $"{error.Campo}: {error.Mensaje}"))),

            DocumentoInvalidoException documento => (
                StatusCodes.Status400BadRequest,
                "Documento no válido",
                documento.Message),

            PeriodoCerradoException periodo => (
                StatusCodes.Status409Conflict,
                "El período no admite la operación",
                periodo.Message),

            // Deliberadamente 404: un 403 confirmaría que el recurso existe.
            AccesoNoAutorizadoException => (
                StatusCodes.Status404NotFound,
                "Recurso no encontrado",
                AccesoNoAutorizadoException.MensajePublico),

            ErrorDeFormulaException formula => (
                StatusCodes.Status400BadRequest,
                "Fórmula no válida",
                formula.Message),

            CatalogoInvalidoException catalogo => (
                StatusCodes.Status400BadRequest,
                "Catálogo de cálculo no válido",
                catalogo.Message),

            NominaInvalidaException nomina => (
                StatusCodes.Status409Conflict,
                "La nómina no admite la operación",
                nomina.Message),

            // Cualquier otra regla de negocio violada es un error del cliente.
            DomainException dominio => (
                StatusCodes.Status400BadRequest,
                "Operación no válida",
                dominio.Message),

            OperationCanceledException => (
                StatusCodes.Status499ClientClosedRequest,
                "Solicitud cancelada",
                "El cliente canceló la solicitud."),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Error interno",
                "Ocurrió un error inesperado. Inténtelo de nuevo más tarde."),
        };
}
