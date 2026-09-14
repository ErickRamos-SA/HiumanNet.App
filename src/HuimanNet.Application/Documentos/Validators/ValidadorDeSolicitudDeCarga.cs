using HuimanNet.Application.Common;
using HuimanNet.Application.Documentos.Commands;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.ValueObjects;

namespace HuimanNet.Application.Documentos.Validators;

/// <summary>
/// Valida la forma de <see cref="SolicitarCargaDocumentoCommand"/> antes de
/// tocar la base de datos.
/// </summary>
/// <remarks>
/// Se ocupa sólo de la <b>forma</b> de la petición. Las reglas de negocio
/// —extensión permitida, tamaño máximo y estado del período— son competencia de
/// <see cref="Domain.Services.ValidadorDeDocumento"/>, que se aplica más
/// adelante en el mismo caso de uso.
/// </remarks>
public sealed class ValidadorDeSolicitudDeCarga : IValidadorDeEntrada<SolicitarCargaDocumentoCommand>
{
    /// <inheritdoc/>
    public ResultadoDeValidacion Validar(SolicitarCargaDocumentoCommand entrada)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var errores = new List<ErrorDeValidacion>();

        if (entrada.PeriodoId == Guid.Empty)
        {
            errores.Add(new ErrorDeValidacion(
                nameof(entrada.PeriodoId), "Debe indicarse el período de carga."));
        }

        if (entrada.Tipo == TipoDocumento.NoEspecificado || !Enum.IsDefined(entrada.Tipo))
        {
            errores.Add(new ErrorDeValidacion(
                nameof(entrada.Tipo), "Debe indicarse un tipo de documento válido."));
        }

        if (string.IsNullOrWhiteSpace(entrada.NombreArchivo))
        {
            errores.Add(new ErrorDeValidacion(
                nameof(entrada.NombreArchivo), "Debe indicarse el nombre del archivo."));
        }
        else if (entrada.NombreArchivo.Length > NombreArchivo.LongitudMaxima)
        {
            errores.Add(new ErrorDeValidacion(
                nameof(entrada.NombreArchivo),
                $"El nombre del archivo no puede exceder {NombreArchivo.LongitudMaxima} caracteres."));
        }

        if (entrada.TamanoBytes <= 0)
        {
            errores.Add(new ErrorDeValidacion(
                nameof(entrada.TamanoBytes), "El tamaño del archivo debe ser mayor que cero."));
        }

        return ResultadoDeValidacion.Con(errores);
    }
}
