using System.Text.Json.Serialization;
using HuimanNet.Contracts.Auditoria;
using HuimanNet.Contracts.Catalogos;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Contracts.Empleados;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Contracts.Incidencias;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Contracts.Usuarios;

namespace HuimanNet.Contracts.Serialization;

/// <summary>
/// Contexto de serialización generado en compilación para todos los contratos
/// HTTP de HuimanNet.
/// </summary>
/// <remarks>
/// Es la pieza que hace posible publicar la API con <b>Native AOT</b>: al
/// generar los metadatos de serialización en tiempo de compilación se elimina
/// por completo la reflexión en tiempo de ejecución (ESPECIFICACION.md §6).
/// <para>
/// El mismo contexto lo consumen la API, la web Blazor y la app MAUI, de modo
/// que el contrato no puede divergir entre clientes.
/// </para>
/// <para>
/// <b>Al añadir un DTO nuevo hay que declararlo aquí</b>, o fallará en tiempo de
/// ejecución con <c>NotSupportedException</c> en la compilación AOT.
/// </para>
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(DocumentoDto))]
[JsonSerializable(typeof(IReadOnlyList<DocumentoDto>))]
[JsonSerializable(typeof(SolicitarCargaRequest))]
[JsonSerializable(typeof(SolicitarCargaResponse))]
[JsonSerializable(typeof(ConfirmarCargaRequest))]
[JsonSerializable(typeof(EnlaceDescargaResponse))]
[JsonSerializable(typeof(RegistrarResultadoEscaneoRequest))]
[JsonSerializable(typeof(PeriodoDto))]
[JsonSerializable(typeof(IReadOnlyList<PeriodoDto>))]
[JsonSerializable(typeof(AbrirPeriodoRequest))]
[JsonSerializable(typeof(CambiarEstadoPeriodoRequest))]
[JsonSerializable(typeof(EmpresaDto))]
[JsonSerializable(typeof(IReadOnlyList<EmpresaDto>))]
[JsonSerializable(typeof(EmpresaDetalleDto))]
[JsonSerializable(typeof(IReadOnlyList<EmpresaDetalleDto>))]
[JsonSerializable(typeof(GuardarEmpresaRequest))]
[JsonSerializable(typeof(RazonSocialDto))]
[JsonSerializable(typeof(IReadOnlyList<RazonSocialDto>))]
[JsonSerializable(typeof(GuardarRazonSocialRequest))]
[JsonSerializable(typeof(EmpleadoDto))]
[JsonSerializable(typeof(EmpleadoResumenDto))]
[JsonSerializable(typeof(IReadOnlyList<EmpleadoResumenDto>))]
[JsonSerializable(typeof(PaginaDto<EmpleadoResumenDto>))]
[JsonSerializable(typeof(GuardarEmpleadoRequest))]
[JsonSerializable(typeof(ContratoDto))]
[JsonSerializable(typeof(IReadOnlyList<ContratoDto>))]
[JsonSerializable(typeof(GuardarContratoRequest))]
[JsonSerializable(typeof(IncidenciaDto))]
[JsonSerializable(typeof(IReadOnlyList<IncidenciaDto>))]
[JsonSerializable(typeof(GuardarIncidenciaRequest))]
[JsonSerializable(typeof(ImportarIncidenciasRequest))]
[JsonSerializable(typeof(ResultadoDeImportacionDto))]
[JsonSerializable(typeof(CorridaDeNominaDto))]
[JsonSerializable(typeof(IReadOnlyList<CorridaDeNominaDto>))]
[JsonSerializable(typeof(ResultadoDeNominaDto))]
[JsonSerializable(typeof(IReadOnlyList<ResultadoDeNominaDto>))]
[JsonSerializable(typeof(DetalleDeResultadoDto))]
[JsonSerializable(typeof(ConceptoCalculadoDto))]
[JsonSerializable(typeof(ValorDto))]
[JsonSerializable(typeof(IReadOnlyList<ValorDto>))]
[JsonSerializable(typeof(FacturacionDeCorridaDto))]
[JsonSerializable(typeof(ResumenDeCorridaDto))]
[JsonSerializable(typeof(CalcularNominaRequest))]
[JsonSerializable(typeof(CambiarEstadoCorridaRequest))]
[JsonSerializable(typeof(CotejarNominaRequest))]
[JsonSerializable(typeof(CotejoDto))]
[JsonSerializable(typeof(IReadOnlyList<CotejoDto>))]
[JsonSerializable(typeof(DiferenciaDeCotejoDto))]
[JsonSerializable(typeof(ParametroDeCalculoDto))]
[JsonSerializable(typeof(IReadOnlyList<ParametroDeCalculoDto>))]
[JsonSerializable(typeof(GuardarParametroRequest))]
[JsonSerializable(typeof(TablaDeRangosDto))]
[JsonSerializable(typeof(IReadOnlyList<TablaDeRangosDto>))]
[JsonSerializable(typeof(RangoDeTablaDto))]
[JsonSerializable(typeof(GuardarTablaRequest))]
[JsonSerializable(typeof(ConceptoDeNominaDto))]
[JsonSerializable(typeof(IReadOnlyList<ConceptoDeNominaDto>))]
[JsonSerializable(typeof(GuardarConceptoRequest))]
[JsonSerializable(typeof(ProbarFormulaRequest))]
[JsonSerializable(typeof(ProbarFormulaResponse))]
[JsonSerializable(typeof(ExplicacionDeCalculoDto))]
[JsonSerializable(typeof(IReadOnlyList<ExplicacionDeCalculoDto>))]
[JsonSerializable(typeof(GuardarExplicacionRequest))]
[JsonSerializable(typeof(ExplicacionCompletaDto))]
[JsonSerializable(typeof(VariableDeCalculoDto))]
[JsonSerializable(typeof(FuncionDeFormulaDto))]
[JsonSerializable(typeof(UsuarioDto))]
[JsonSerializable(typeof(IReadOnlyList<UsuarioDto>))]
[JsonSerializable(typeof(GuardarUsuarioRequest))]
[JsonSerializable(typeof(RestablecerContrasenaRequest))]
[JsonSerializable(typeof(PermisoDto))]
[JsonSerializable(typeof(ResumenDeInicioDto))]
[JsonSerializable(typeof(PendienteDto))]
[JsonSerializable(typeof(RegistroAuditoriaDto))]
[JsonSerializable(typeof(PaginaDto<RegistroAuditoriaDto>))]
[JsonSerializable(typeof(UsuarioActualDto))]
[JsonSerializable(typeof(ConfiguracionPublicaDto))]
[JsonSerializable(typeof(IniciarSesionRequest))]
[JsonSerializable(typeof(IniciarSesionResponse))]
[JsonSerializable(typeof(CambiarContrasenaRequest))]
[JsonSerializable(typeof(ActualizarPreferenciasRequest))]
public sealed partial class HuimanNetJsonContext : JsonSerializerContext;
