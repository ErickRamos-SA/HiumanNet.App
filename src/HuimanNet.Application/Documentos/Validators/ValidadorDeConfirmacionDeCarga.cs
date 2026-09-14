using HuimanNet.Application.Common;
using HuimanNet.Application.Documentos.Commands;
using HuimanNet.Domain.ValueObjects;

namespace HuimanNet.Application.Documentos.Validators;

/// <summary>
/// Valida la forma de <see cref="ConfirmarCargaCommand"/>.
/// </summary>
public sealed class ValidadorDeConfirmacionDeCarga : IValidadorDeEntrada<ConfirmarCargaCommand>
{
    /// <inheritdoc/>
    public ResultadoDeValidacion Validar(ConfirmarCargaCommand entrada)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var errores = new List<ErrorDeValidacion>();

        if (entrada.DocumentoId == Guid.Empty)
        {
            errores.Add(new ErrorDeValidacion(
                nameof(entrada.DocumentoId), "Debe indicarse el documento a confirmar."));
        }

        if (string.IsNullOrWhiteSpace(entrada.HuellaSha256))
        {
            errores.Add(new ErrorDeValidacion(
                nameof(entrada.HuellaSha256), "Debe indicarse la huella SHA-256 del archivo."));
        }
        else if (entrada.HuellaSha256.Trim().Length != HuellaArchivo.LongitudSha256Hex)
        {
            errores.Add(new ErrorDeValidacion(
                nameof(entrada.HuellaSha256),
                $"La huella debe tener {HuellaArchivo.LongitudSha256Hex} caracteres hexadecimales."));
        }

        return ResultadoDeValidacion.Con(errores);
    }
}
