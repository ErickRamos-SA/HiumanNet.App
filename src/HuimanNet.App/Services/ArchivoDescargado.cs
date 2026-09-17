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
/// Archivo generado por el servidor (por ejemplo, la exportación de una corrida).
/// </summary>
/// <param name="Nombre">Nombre sugerido.</param>
/// <param name="TipoDeContenido">Tipo MIME.</param>
/// <param name="Contenido">Bytes del archivo.</param>
public sealed record ArchivoDescargado(string Nombre, string TipoDeContenido, byte[] Contenido);
