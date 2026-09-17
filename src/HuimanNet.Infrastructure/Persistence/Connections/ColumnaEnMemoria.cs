namespace HuimanNet.Infrastructure.Persistence.Connections;

/// <summary>
/// Columna de un <see cref="LectorEnMemoria{T}"/>.
/// </summary>
/// <typeparam name="T">Tipo de las filas.</typeparam>
/// <param name="Nombre">Nombre de la columna de destino.</param>
/// <param name="Tipo">Tipo CLR de los valores.</param>
/// <param name="Valor">Función que extrae el valor de una fila; <c>null</c> se envía como <see cref="DBNull"/>.</param>
public sealed record ColumnaEnMemoria<T>(
    string Nombre,
    [param: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
        System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields
        | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
    [property: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
        System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields
        | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
    Type Tipo,
    Func<T, object?> Valor);
