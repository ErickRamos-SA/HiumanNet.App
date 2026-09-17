namespace HuimanNet.Application.Interfaces;

/// <summary>
/// Motivo por el que se genera un aviso a los usuarios.
/// </summary>
public enum TipoDeAviso
{
    /// <summary>Valor no especificado. Nunca debe encolarse.</summary>
    NoEspecificado = 0,

    /// <summary>La empresa cliente publicó documentos: se avisa al operador de nómina.</summary>
    DocumentosRecibidos = 1,

    /// <summary>El operador publicó resultados o ajustes: se avisa a la empresa cliente.</summary>
    ResultadosDisponibles = 2,

    /// <summary>El escaneo detectó malware: se avisa al administrador y a quien cargó el archivo.</summary>
    ArchivoEnCuarentena = 3,
}
