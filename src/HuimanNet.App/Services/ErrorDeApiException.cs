using System.Net;
using HuimanNet.Contracts.Catalogos;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Contracts.Empleados;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Contracts.Incidencias;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Enums;

namespace HuimanNet.App.Services;

/// <summary>
/// Rechazo de la API con el mensaje que redactó el servidor.
/// </summary>
public sealed class ErrorDeApiException : Exception
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ErrorDeApiException"/>.
    /// </summary>
    /// <param name="mensaje">Mensaje para el usuario.</param>
    /// <param name="estado">Código HTTP de la respuesta.</param>
    public ErrorDeApiException(string mensaje, HttpStatusCode estado)
        : base(mensaje)
        => Estado = estado;

    /// <summary>Inicializa una nueva instancia de <see cref="ErrorDeApiException"/>.</summary>
    public ErrorDeApiException()
    {
    }

    /// <summary>Inicializa una nueva instancia de <see cref="ErrorDeApiException"/>.</summary>
    /// <param name="message">Mensaje.</param>
    public ErrorDeApiException(string message)
        : base(message)
    {
    }

    /// <summary>Inicializa una nueva instancia de <see cref="ErrorDeApiException"/>.</summary>
    /// <param name="message">Mensaje.</param>
    /// <param name="innerException">Excepción de origen.</param>
    public ErrorDeApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Obtiene el código HTTP de la respuesta.</summary>
    /// <value>Por ejemplo <see cref="HttpStatusCode.BadRequest"/>.</value>
    public HttpStatusCode Estado { get; }
}
