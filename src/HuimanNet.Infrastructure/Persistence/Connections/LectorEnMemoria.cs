using System.Collections;
using System.Data.Common;

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

/// <summary>
/// Expone una lista en memoria como <see cref="DbDataReader"/> de sólo avance,
/// para alimentar <c>SqlBulkCopy</c>.
/// </summary>
/// <typeparam name="T">Tipo de las filas.</typeparam>
/// <remarks>
/// Alternativa a <c>DataTable</c>, que arrastra reflexión y advertencias de
/// recorte en Native AOT. La inserción masiva es la única forma razonable de
/// persistir los resultados de una corrida con miles de trabajadores en una
/// fracción de segundo.
/// </remarks>
public sealed class LectorEnMemoria<T> : DbDataReader
{
    private readonly IReadOnlyList<T> _filas;
    private readonly IReadOnlyList<ColumnaEnMemoria<T>> _columnas;
    private int _indice = -1;
    private bool _cerrado;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="LectorEnMemoria{T}"/>.
    /// </summary>
    /// <param name="filas">Filas a exponer.</param>
    /// <param name="columnas">Columnas, en el orden de la tabla de destino.</param>
    public LectorEnMemoria(IReadOnlyList<T> filas, IReadOnlyList<ColumnaEnMemoria<T>> columnas)
    {
        ArgumentNullException.ThrowIfNull(filas);
        ArgumentNullException.ThrowIfNull(columnas);

        _filas = filas;
        _columnas = columnas;
    }

    /// <inheritdoc/>
    public override int FieldCount => _columnas.Count;

    /// <inheritdoc/>
    public override bool HasRows => _filas.Count > 0;

    /// <inheritdoc/>
    public override bool IsClosed => _cerrado;

    /// <inheritdoc/>
    public override int RecordsAffected => -1;

    /// <inheritdoc/>
    public override int Depth => 0;

    /// <inheritdoc/>
    public override object this[int ordinal] => GetValue(ordinal);

    /// <inheritdoc/>
    public override object this[string name] => GetValue(GetOrdinal(name));

    /// <inheritdoc/>
    public override bool Read() => ++_indice < _filas.Count;

    /// <inheritdoc/>
    public override bool NextResult() => false;

    /// <inheritdoc/>
    public override void Close() => _cerrado = true;

    /// <inheritdoc/>
    public override string GetName(int ordinal) => _columnas[ordinal].Nombre;

    /// <inheritdoc/>
    public override int GetOrdinal(string name)
    {
        for (int i = 0; i < _columnas.Count; i++)
        {
            if (string.Equals(_columnas[i].Nombre, name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new IndexOutOfRangeException($"La columna '{name}' no existe.");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// La anotación replica la del miembro base para que el análisis de recorte
    /// (Native AOT) no emita IL2093. Los tipos de columna son primitivos del
    /// sistema (<see cref="Guid"/>, <see cref="decimal"/>, <see cref="string"/>…),
    /// que el recorte conserva siempre.
    /// </remarks>
    [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
        System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields
        | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type GetFieldType(int ordinal) => _columnas[ordinal].Tipo;

    /// <inheritdoc/>
    public override string GetDataTypeName(int ordinal) => _columnas[ordinal].Tipo.Name;

    /// <inheritdoc/>
    public override object GetValue(int ordinal) => _columnas[ordinal].Valor(_filas[_indice]) ?? DBNull.Value;

    /// <inheritdoc/>
    public override int GetValues(object[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        int n = Math.Min(values.Length, _columnas.Count);

        for (int i = 0; i < n; i++)
        {
            values[i] = GetValue(i);
        }

        return n;
    }

    /// <inheritdoc/>
    public override bool IsDBNull(int ordinal) => _columnas[ordinal].Valor(_filas[_indice]) is null;

    /// <inheritdoc/>
    public override bool GetBoolean(int ordinal) => (bool)GetValue(ordinal);

    /// <inheritdoc/>
    public override byte GetByte(int ordinal) => (byte)GetValue(ordinal);

    /// <inheritdoc/>
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
        => throw new NotSupportedException();

    /// <inheritdoc/>
    public override char GetChar(int ordinal) => (char)GetValue(ordinal);

    /// <inheritdoc/>
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
        => throw new NotSupportedException();

    /// <inheritdoc/>
    public override DateTime GetDateTime(int ordinal) => (DateTime)GetValue(ordinal);

    /// <inheritdoc/>
    public override decimal GetDecimal(int ordinal) => (decimal)GetValue(ordinal);

    /// <inheritdoc/>
    public override double GetDouble(int ordinal) => (double)GetValue(ordinal);

    /// <inheritdoc/>
    public override float GetFloat(int ordinal) => (float)GetValue(ordinal);

    /// <inheritdoc/>
    public override Guid GetGuid(int ordinal) => (Guid)GetValue(ordinal);

    /// <inheritdoc/>
    public override short GetInt16(int ordinal) => (short)GetValue(ordinal);

    /// <inheritdoc/>
    public override int GetInt32(int ordinal) => (int)GetValue(ordinal);

    /// <inheritdoc/>
    public override long GetInt64(int ordinal) => (long)GetValue(ordinal);

    /// <inheritdoc/>
    public override string GetString(int ordinal) => (string)GetValue(ordinal);

    /// <inheritdoc/>
    public override IEnumerator GetEnumerator() => throw new NotSupportedException();
}
