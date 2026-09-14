namespace HuimanNet.Domain.Enums;

/// <summary>
/// Esquema de servicio pactado con la razón social: determina la base
/// facturable y si existe un complemento pagado vía sindicato.
/// </summary>
public enum TipoDeServicio
{
    /// <summary>Valor no especificado. Nunca debe persistirse.</summary>
    NoEspecificado = 0,

    /// <summary>La nómina completa se paga y factura como nómina fiscal.</summary>
    Nomina = 1,

    /// <summary>La parte fiscal se paga con el salario registrado y la diferencia se entrega vía sindicato o cooperativa.</summary>
    Maquila = 2,
}

/// <summary>
/// Base sobre la que se calcula la comisión al cliente.
/// </summary>
public enum ModalidadDeComision
{
    /// <summary>Valor no especificado. Nunca debe persistirse.</summary>
    NoEspecificado = 0,

    /// <summary>Porcentaje sobre el subtotal de costos (base, ISN, cargas sociales).</summary>
    SobreCosto = 1,

    /// <summary>Porcentaje sobre el bruto de incidencias.</summary>
    SobreBrutos = 2,
}

/// <summary>
/// Zona geográfica de salario mínimo.
/// </summary>
public enum ZonaSalarioMinimo
{
    /// <summary>Valor no especificado. Nunca debe persistirse.</summary>
    NoEspecificado = 0,

    /// <summary>Zona A: resto del país.</summary>
    A = 1,

    /// <summary>Zona B: Zona Libre de la Frontera Norte.</summary>
    B = 2,
}

/// <summary>
/// Regla con la que se determina la tasa del Impuesto Sobre Nóminas de una razón social.
/// </summary>
public enum ZonaIsn
{
    /// <summary>La tasa se toma de la zona de salario mínimo de cada trabajador.</summary>
    SegunZonaDelTrabajador = 0,

    /// <summary>Se fuerza la tasa de la zona A para todos los trabajadores.</summary>
    ForzarZonaA = 1,

    /// <summary>Se fuerza la tasa de la zona B para todos los trabajadores.</summary>
    ForzarZonaB = 2,
}

/// <summary>
/// Modalidad del aviso de retención de un crédito INFONAVIT.
/// </summary>
public enum TipoDeCreditoInfonavit
{
    /// <summary>El trabajador no tiene crédito vigente.</summary>
    Ninguno = 0,

    /// <summary>Importe fijo mensual.</summary>
    CuotaFija = 1,

    /// <summary>Factor expresado en veces salario mínimo, convertido con la UMI.</summary>
    VecesSalarioMinimo = 2,

    /// <summary>Porcentaje sobre el salario diario.</summary>
    Porcentaje = 3,
}

/// <summary>
/// Clasificación del movimiento de un trabajador dentro de un período.
/// </summary>
public enum TipoDeMovimiento
{
    /// <summary>Nómina ordinaria del período.</summary>
    Ordinaria = 1,

    /// <summary>Última nómina del trabajador: incluye finiquito y se factura por separado.</summary>
    Finiquito = 2,
}

/// <summary>
/// Estado de una corrida de cálculo de nómina.
/// </summary>
/// <remarks>
/// En esta primera etapa el sistema calcula en paralelo con el equipo de nómina:
/// una corrida se coteja contra el resultado manual y sólo se aprueba cuando
/// las diferencias están dentro de la tolerancia acordada.
/// </remarks>
public enum EstadoDeCorrida
{
    /// <summary>Valor no especificado. Nunca debe persistirse.</summary>
    NoEspecificado = 0,

    /// <summary>Calculada por el sistema; pendiente de cotejo.</summary>
    Calculada = 1,

    /// <summary>Cotejada contra el resultado manual.</summary>
    Cotejada = 2,

    /// <summary>Aprobada como resultado definitivo del período.</summary>
    Aprobada = 3,

    /// <summary>Descartada; se conserva sólo como historial.</summary>
    Descartada = 4,
}

/// <summary>
/// Idioma de la interfaz de usuario.
/// </summary>
public enum Idioma
{
    /// <summary>Español (predeterminado).</summary>
    Espanol = 1,

    /// <summary>Inglés.</summary>
    Ingles = 2,
}

/// <summary>
/// Utilidades del enumerado <see cref="Idioma"/>.
/// </summary>
public static class IdiomaExtensiones
{
    /// <summary>
    /// Devuelve el código de cultura de dos letras del idioma.
    /// </summary>
    /// <param name="idioma">Idioma a convertir.</param>
    /// <returns><c>"es"</c> o <c>"en"</c>.</returns>
    public static string Codigo(this Idioma idioma) => idioma == Idioma.Ingles ? "en" : "es";

    /// <summary>
    /// Interpreta un código de cultura.
    /// </summary>
    /// <param name="codigo">Código como <c>"es"</c>, <c>"en"</c> o <c>"en-US"</c>.</param>
    /// <returns>El idioma correspondiente; español si el código no se reconoce.</returns>
    public static Idioma DesdeCodigo(string? codigo)
        => codigo is not null && codigo.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? Idioma.Ingles
            : Idioma.Espanol;
}
