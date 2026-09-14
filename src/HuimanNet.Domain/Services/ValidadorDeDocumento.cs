using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.ValueObjects;

namespace HuimanNet.Domain.Services;

/// <summary>
/// Valida que un documento cumpla las reglas de negocio de carga:
/// tipo de archivo permitido, tamaño máximo y período abierto.
/// </summary>
/// <remarks>
/// Este servicio de dominio es puro: no accede a infraestructura y es
/// completamente determinista, lo que facilita su prueba unitaria.
/// La validación ocurre <b>antes</b> de emitir cualquier URL SAS de escritura.
/// </remarks>
public sealed class ValidadorDeDocumento
{
    private readonly PoliticaDeCarga _politica;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ValidadorDeDocumento"/>.
    /// </summary>
    /// <param name="politica">
    /// Política vigente con las extensiones permitidas y el tamaño máximo.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="politica"/> es <c>null</c>.
    /// </exception>
    public ValidadorDeDocumento(PoliticaDeCarga politica)
    {
        _politica = politica ?? throw new ArgumentNullException(nameof(politica));
    }

    /// <summary>
    /// Valida una solicitud de carga de documento contra la política vigente.
    /// </summary>
    /// <param name="nombreArchivo">Nombre original del archivo aportado por el usuario.</param>
    /// <param name="tamano">Tamaño del archivo a cargar.</param>
    /// <param name="tipo">Tipo funcional del documento.</param>
    /// <param name="periodo">Período al que se asociará el documento.</param>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="periodo"/> es <c>null</c>.
    /// </exception>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza cuando la extensión no está permitida o se excede el tamaño máximo.
    /// </exception>
    /// <exception cref="PeriodoCerradoException">
    /// Se lanza cuando el período ya no admite cargas de ese tipo de documento.
    /// </exception>
    public void Validar(
        NombreArchivo nombreArchivo, TamanoArchivo tamano, TipoDocumento tipo, PeriodoCarga periodo)
    {
        ArgumentNullException.ThrowIfNull(periodo);

        if (!_politica.PermiteExtension(nombreArchivo.Extension))
        {
            throw new DocumentoInvalidoException(
                $"La extensión '{nombreArchivo.Extension}' no está permitida. " +
                $"Extensiones aceptadas: {string.Join(", ", _politica.ExtensionesPermitidas)}.");
        }

        if (tamano.Excede(_politica.TamanoMaximo))
        {
            throw new DocumentoInvalidoException(
                $"El archivo pesa {tamano} y el máximo permitido es {_politica.TamanoMaximo}.");
        }

        periodo.GarantizarQueAdmiteCarga(tipo);
    }
}
