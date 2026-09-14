using HuimanNet.Application.Common;
using HuimanNet.Application.Periodos.Commands;
using HuimanNet.Domain.ValueObjects;

namespace HuimanNet.Application.Periodos.Validators;

/// <summary>
/// Valida la forma de <see cref="AbrirPeriodoCommand"/>.
/// </summary>
public sealed class ValidadorDeAperturaDePeriodo : IValidadorDeEntrada<AbrirPeriodoCommand>
{
    /// <summary>Longitud máxima de la descripción del período.</summary>
    public const int LongitudMaximaDescripcion = 200;

    /// <inheritdoc/>
    public ResultadoDeValidacion Validar(AbrirPeriodoCommand entrada)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var errores = new List<ErrorDeValidacion>();

        if (entrada.EmpresaId == Guid.Empty)
        {
            errores.Add(new ErrorDeValidacion(
                nameof(entrada.EmpresaId), "Debe indicarse la empresa del período."));
        }

        if (entrada.Anio is < PeriodoCalendario.AnioMinimo or > PeriodoCalendario.AnioMaximo)
        {
            errores.Add(new ErrorDeValidacion(
                nameof(entrada.Anio),
                $"El año debe estar entre {PeriodoCalendario.AnioMinimo} y {PeriodoCalendario.AnioMaximo}."));
        }

        if (entrada.Mes is < 1 or > 12)
        {
            errores.Add(new ErrorDeValidacion(nameof(entrada.Mes), "El mes debe estar entre 1 y 12."));
        }

        if (entrada.Consecutivo is < 1 or > PeriodoCalendario.ConsecutivoMaximo)
        {
            errores.Add(new ErrorDeValidacion(
                nameof(entrada.Consecutivo),
                $"El consecutivo debe estar entre 1 y {PeriodoCalendario.ConsecutivoMaximo}."));
        }

        if (string.IsNullOrWhiteSpace(entrada.Descripcion))
        {
            errores.Add(new ErrorDeValidacion(
                nameof(entrada.Descripcion), "Debe indicarse la descripción del período."));
        }
        else if (entrada.Descripcion.Length > LongitudMaximaDescripcion)
        {
            errores.Add(new ErrorDeValidacion(
                nameof(entrada.Descripcion),
                $"La descripción no puede exceder {LongitudMaximaDescripcion} caracteres."));
        }

        return ResultadoDeValidacion.Con(errores);
    }
}
