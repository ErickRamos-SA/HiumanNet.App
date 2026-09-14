namespace HuimanNet.Domain.Enums;

/// <summary>
/// Clasifica los documentos que circulan por el portal de intercambio.
/// </summary>
/// <remarks>
/// La dirección del flujo la determina el tipo: <see cref="Incidencia"/> y
/// <see cref="DatosEmpleado"/> los aporta la empresa cliente; <see cref="Resultado"/>
/// y <see cref="Ajuste"/> los aporta el operador de nómina.
/// </remarks>
public enum TipoDocumento
{
    /// <summary>Valor no especificado. Nunca debe persistirse.</summary>
    NoEspecificado = 0,

    /// <summary>Incidencias del período (faltas, horas extra, permisos). Lo sube el cliente.</summary>
    Incidencia = 1,

    /// <summary>Altas, bajas y modificaciones del catálogo de empleados. Lo sube el cliente.</summary>
    DatosEmpleado = 2,

    /// <summary>Archivo de resultado de la nómina procesada. Lo sube el operador.</summary>
    Resultado = 3,

    /// <summary>Archivo de ajustes posteriores al resultado. Lo sube el operador.</summary>
    Ajuste = 4,
}
