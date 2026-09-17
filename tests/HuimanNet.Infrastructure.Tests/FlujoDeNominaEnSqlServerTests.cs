using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using HuimanNet.Application.Common;
using HuimanNet.Application.Documentos.Commands;
using HuimanNet.Application.Empleados;
using HuimanNet.Application.Empresas;
using HuimanNet.Application.Incidencias;
using HuimanNet.Application.Interfaces;
using HuimanNet.Application.Nomina;
using HuimanNet.Application.Periodos.Commands;
using HuimanNet.Application.Periodos.Queries;
using HuimanNet.Application.RazonesSociales;
using HuimanNet.Application.Usuarios;
using HuimanNet.Contracts.Common;
using HuimanNet.Contracts.Documentos;
using HuimanNet.Contracts.Empleados;
using HuimanNet.Contracts.Empresas;
using HuimanNet.Contracts.Incidencias;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Contracts.Usuarios;
using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.Repositories;
using HuimanNet.Domain.ValueObjects;
using HuimanNet.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static HuimanNet.Infrastructure.Tests.EntornoSqlDePrueba;

namespace HuimanNet.Infrastructure.Tests;

/// <summary>
/// Recorre el flujo completo de nómina contra un SQL Server real: alta de
/// empresa, razón social, empleados y contratos; período; archivos del período
/// subidos por la empresa; importación y captura de incidencias; cálculo,
/// reproceso, consultas, exportación, cotejo contra el archivo manual del
/// período y aprobación, con la separación de roles entre la empresa cliente
/// y nómina.
/// </summary>
/// <remarks>
/// Usa una base de datos exclusiva, <c>HuimanNet_Pruebas</c>, que se borra y se
/// recrea en cada ejecución con los mismos scripts y el mismo sembrado que el
/// arranque de la aplicación. La cadena de conexión se puede cambiar con la
/// variable de entorno <c>HUIMANNET_SQL_PRUEBAS</c>. Si no hay servidor
/// accesible, la prueba se omite en lugar de fallar.
/// </remarks>
public sealed class FlujoDeNominaEnSqlServerTests
{
    private const string CorreoAdministrador = "admin.pruebas@huimannet.local";
    private const string CorreoCliente = "cliente.pruebas@huimannet.local";

    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    public async Task FlujoCompleto_ReproduceLaHojaDeReferencia()
    {
        string cadena = Cadena();
        string? motivo = await RecrearBaseDeDatosAsync(cadena);
        Assert.SkipWhen(motivo is not null, $"SQL Server de pruebas no disponible: {motivo}");

        await using ServiceProvider servicios = ConstruirServicios(cadena, CorreoAdministrador);
        (await PreparacionDelEntorno.EjecutarAsync(servicios, Ct)).Should().BeTrue("la base de datos debe crearse, migrarse y sembrarse");

        UsuarioDePrueba identidad = servicios.GetRequiredService<UsuarioDePrueba>();
        Usuario administrador = await ObtenerUsuarioAsync(servicios, CorreoAdministrador);
        identidad.Establecer(administrador);
        var casos = new Casos(servicios);

        // ---- Catálogos de la empresa --------------------------------------
        EmpresaDetalleDto empresa = await casos.Ejecutar<CrearEmpresaCommand, EmpresaDetalleDto>(
            new CrearEmpresaCommand("Creatfor Pruebas", "CPR210104AB1"));

        RazonSocialDto razon = await casos.Ejecutar<GuardarRazonSocialCommand, RazonSocialDto>(new GuardarRazonSocialCommand(
            null,
            new GuardarRazonSocialRequest(
                empresa.Id, "Creatfor Imagen y Ventas", "CIV210104AB1", "Y5239554107", ZonaSalarioMinimo.B, TipoDeServicio.Nomina,
                SubsidioAbsorbido: false, AplicaFaltasProporcionales: false, ModalidadDeComision.SobreCosto, 0.08m,
                ZonaIsn.SegunZonaDelTrabajador, 0.16m, 0m, null, "BBVA")));

        ContratoDto stephany = await AltaAsync(casos, empresa.Id, razon.Id, "1", "Stephany", "Sarmiento", "Chong", "917", 22000m);
        ContratoDto luis = await AltaAsync(casos, empresa.Id, razon.Id, "9", "Luis Alberto", "Melchor", "Garcia", "1511", 10000m);

        PeriodoDto periodo = await casos.Ejecutar<AbrirPeriodoCommand, PeriodoDto>(
            new AbrirPeriodoCommand(empresa.Id, 2026, 2, 1, "Nómina semanal 05", null));

        // ---- Sin archivos del período no hay cálculo ------------------------
        Func<Task> calcularSinArchivos = () => casos.Ejecutar<CalcularNominaCommand, CorridaDeNominaDto>(
            new CalcularNominaCommand(periodo.Id, empresa.Id, null));
        await calcularSinArchivos.Should().ThrowAsync<NominaInvalidaException>();

        // ---- Usuario de la empresa: nunca recibe acciones de nómina ---------
        Func<Task> concederCalculoAlCliente = () => casos.Ejecutar<GuardarUsuarioCommand, UsuarioDto>(new GuardarUsuarioCommand(
            null,
            new GuardarUsuarioRequest(
                "Cliente de pruebas", CorreoCliente, RolUsuario.ClienteEmpresa, empresa.Id, true, Idioma.Espanol, null,
                [new PermisoDto(AccionDelSistema.CalcularNomina, true)])));
        await concederCalculoAlCliente.Should().ThrowAsync<EntradaInvalidaException>();

        await casos.Ejecutar<GuardarUsuarioCommand, UsuarioDto>(new GuardarUsuarioCommand(
            null,
            new GuardarUsuarioRequest("Cliente de pruebas", CorreoCliente, RolUsuario.ClienteEmpresa, empresa.Id, true, Idioma.Espanol, null, [])));
        Usuario cliente = await ObtenerUsuarioAsync(servicios, CorreoCliente);

        // ---- La empresa sube sus incidencias al período y las importa -------
        identidad.Establecer(cliente);

        byte[] archivoDeIncidencias = Encoding.UTF8.GetBytes(
            "Clave,Dias de Periodo,Gratificacion,Descuento Prestamo Personal,Descuento sindical adicional,Observaciones\n" +
            "1,7,900,961.5,3358.39,Descuento Prestamo 19/26\n");

        Guid documentoDeIncidencias = await SubirAsync(casos, periodo.Id, null, TipoDocumento.Incidencia, "incidencias.csv", archivoDeIncidencias);

        ResultadoDeImportacionDto importacion = await casos.Ejecutar<ImportarIncidenciasCommand, ResultadoDeImportacionDto>(
            new ImportarIncidenciasCommand(documentoDeIncidencias, null));

        importacion.Errores.Should().BeEmpty();
        importacion.Creadas.Should().Be(1);

        await casos.Ejecutar<GuardarIncidenciaCommand, IncidenciaDto>(new GuardarIncidenciaCommand(new GuardarIncidenciaRequest(
            periodo.Id, luis.Id, null, 7m,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            null, TipoDeMovimiento.Ordinaria, "Semana completa")));

        Func<Task> calcularComoCliente = () => casos.Ejecutar<CalcularNominaCommand, CorridaDeNominaDto>(
            new CalcularNominaCommand(periodo.Id, null, null));
        await calcularComoCliente.Should().ThrowAsync<AccesoNoAutorizadoException>();

        // ---- Nómina calcula con las incidencias del período -----------------
        identidad.Establecer(administrador);

        IReadOnlyList<IncidenciaDto> incidencias = await casos.Consultar<ListarIncidenciasQuery, IReadOnlyList<IncidenciaDto>>(
            new ListarIncidenciasQuery(periodo.Id, empresa.Id));

        incidencias.Should().HaveCount(2).And.OnlyContain(i => i.Id != null);
        incidencias.Single(i => i.ContratoId == stephany.Id).PrestamoPersonal.Should().Be(961.5m);

        CorridaDeNominaDto primera = await casos.Ejecutar<CalcularNominaCommand, CorridaDeNominaDto>(
            new CalcularNominaCommand(periodo.Id, empresa.Id, "Prueba de integración"));
        primera.Numero.Should().Be(1);

        // ---- Reproceso: corrida nueva; la anterior queda reemplazada --------
        CorridaDeNominaDto corrida = await casos.Ejecutar<CalcularNominaCommand, CorridaDeNominaDto>(
            new CalcularNominaCommand(periodo.Id, empresa.Id, "Reproceso"));

        corrida.Numero.Should().Be(2);
        corrida.Trabajadores.Should().Be(2);
        corrida.Estado.Should().Be(EstadoDeCorrida.Calculada);
        corrida.Bruto.Should().Be(31938.5m);
        corrida.ComplementoSindical.Should().Be(22407.93m);
        corrida.CostoTotal.Should().BeApproximately(36840.894m, 0.01m);

        IReadOnlyList<CorridaDeNominaDto> historial = await casos.Consultar<ListarCorridasQuery, IReadOnlyList<CorridaDeNominaDto>>(
            new ListarCorridasQuery(periodo.Id, empresa.Id));

        historial.Should().HaveCount(2);
        historial.Single(c => c.Numero == 1).Should().Match<CorridaDeNominaDto>(
            c => c.Estado == EstadoDeCorrida.Descartada && c.Observaciones != null && c.Observaciones.Contains("#2"));

        ResumenDeCorridaDto resumen = await casos.Consultar<ObtenerResumenDeCorridaQuery, ResumenDeCorridaDto>(
            new ObtenerResumenDeCorridaQuery(corrida.Id, empresa.Id));

        ResultadoDeNominaDto deStephany = resumen.Resultados.Single(r => r.ClaveEmpleado == "1");
        deStephany.Bruto.Should().Be(21938.5m);
        deStephany.TotalPercepciones.Should().Be(3086.09m);
        deStephany.Isr.Should().Be(0m);
        deStephany.ImssTrabajador.Should().Be(0m);
        deStephany.ComplementoSindical.Should().Be(15494.02m);
        deStephany.Isn.Should().BeApproximately(131.158825m, 0.0001m);
        deStephany.Comision.Should().BeApproximately(1842.0175589m, 0.0001m);
        deStephany.CostoTotal.Should().BeApproximately(24867.2370454m, 0.0001m);

        resumen.Resultados.Single(r => r.ClaveEmpleado == "9").ComplementoSindical.Should().Be(6913.91m);
        resumen.Facturacion.Should().ContainSingle().Which.Trabajadores.Should().Be(2);

        DetalleDeResultadoDto detalle = await casos.Consultar<ObtenerDetalleDeResultadoQuery, DetalleDeResultadoDto>(
            new ObtenerDetalleDeResultadoQuery(deStephany.Id, empresa.Id));

        detalle.Conceptos.Should().Contain(c => c.Clave == "COMPLEMENTO_SINDICAL" && c.Importe == 15494.02m);
        detalle.Variables.Should().Contain(v => v.Clave == "SDI" && v.Valor == 465.03m);

        ArchivoExportado exportado = await casos.Consultar<ExportarCorridaQuery, ArchivoExportado>(new ExportarCorridaQuery(corrida.Id, empresa.Id));
        exportado.Contenido.Should().NotBeEmpty();

        // ---- Lado de lectura ----------------------------------------------
        PaginaDto<EmpleadoResumenDto> empleados = await casos.Usar<IConsultasEmpleados, PaginaDto<EmpleadoResumenDto>>(
            c => c.ListarAsync([empresa.Id], true, "steph", 1, 50, Ct));
        empleados.Elementos.Should().ContainSingle().Which.Esquemas.Should().Be("Imss");

        PaginaDto<EmpleadoResumenDto> deTodasLasEmpresas = await casos.Consultar<ListarEmpleadosQuery, PaginaDto<EmpleadoResumenDto>>(
            new ListarEmpleadosQuery(null, true, null, 1, 50));
        deTodasLasEmpresas.Elementos.Should().HaveCount(2)
            .And.OnlyContain(e => e.EmpresaId == empresa.Id && e.EmpresaRazonSocial == "Creatfor Pruebas");

        HuimanNet.Application.Inicio.DatosDeInicio inicio = await casos.Usar<IConsultasInicio, HuimanNet.Application.Inicio.DatosDeInicio>(
            c => c.ObtenerDatosAsync(null, Ct));
        inicio.EmpleadosActivos.Should().Be(2);
        inicio.CorridasPorCotejar.Should().Be(1);
        inicio.Corridas.Should().ContainSingle(c => c.CorridaId == corrida.Id);

        IReadOnlyList<UsuarioDto> usuarios = await casos.Usar<IConsultasUsuarios, IReadOnlyList<UsuarioDto>>(c => c.ListarAsync(null, true, Ct));
        usuarios.Should().Contain(u => u.Rol == RolUsuario.Administrador);

        IReadOnlyList<EmpresaDetalleDto> empresas = await casos.Usar<IConsultasEmpresas, IReadOnlyList<EmpresaDetalleDto>>(c => c.ListarDetalleAsync(true, Ct));
        empresas.Single(e => e.Id == empresa.Id).TotalEmpleados.Should().Be(2);

        // ---- Empresa cliente con varias empresas: sólo ve las suyas --------
        EmpresaDetalleDto otraEmpresa = await casos.Ejecutar<CrearEmpresaCommand, EmpresaDetalleDto>(
            new CrearEmpresaCommand("Creatfor Servicios", "CSE210104AB1"));
        EmpresaDetalleDto ajena = await casos.Ejecutar<CrearEmpresaCommand, EmpresaDetalleDto>(
            new CrearEmpresaCommand("Empresa Ajena", "EAJ210104AB1"));
        await casos.Ejecutar<GuardarEmpleadoCommand, EmpleadoDto>(new GuardarEmpleadoCommand(
            null, new GuardarEmpleadoRequest(ajena.Id, "1", "Ajeno", "Perez", null, null, null, null, null, null, null)));

        UsuarioDto conVariasEmpresas = await casos.Ejecutar<GuardarUsuarioCommand, UsuarioDto>(new GuardarUsuarioCommand(
            cliente.Id,
            new GuardarUsuarioRequest(
                "Cliente de pruebas", CorreoCliente, RolUsuario.ClienteEmpresa, empresa.Id, true, Idioma.Espanol, null, [],
                [otraEmpresa.Id])));

        conVariasEmpresas.EmpresasAdicionales.Should().Equal(otraEmpresa.Id);
        cliente = await ObtenerUsuarioAsync(servicios, CorreoCliente);
        cliente.Empresas.Should().Equal(empresa.Id, otraEmpresa.Id);

        identidad.Establecer(cliente);

        IReadOnlyList<PeriodoDto> periodosDeLaOtra = await casos.Consultar<ListarPeriodosQuery, IReadOnlyList<PeriodoDto>>(
            new ListarPeriodosQuery(otraEmpresa.Id, IncluirCerrados: true));
        periodosDeLaOtra.Should().BeEmpty();

        Func<Task> verEmpresaAjena = () => casos.Consultar<ListarPeriodosQuery, IReadOnlyList<PeriodoDto>>(
            new ListarPeriodosQuery(ajena.Id, IncluirCerrados: true));
        await verEmpresaAjena.Should().ThrowAsync<AccesoNoAutorizadoException>();

        PaginaDto<EmpleadoResumenDto> empleadosDelCliente = await casos.Consultar<ListarEmpleadosQuery, PaginaDto<EmpleadoResumenDto>>(
            new ListarEmpleadosQuery(null, true, null, 1, 50));
        empleadosDelCliente.Elementos.Should().HaveCount(2).And.OnlyContain(e => e.EmpresaId == empresa.Id);

        identidad.Establecer(administrador);

        // ---- La empresa consulta sólo la corrida vigente -------------------
        identidad.Establecer(cliente);

        IReadOnlyList<CorridaDeNominaDto> vistaDelCliente = await casos.Consultar<ListarCorridasQuery, IReadOnlyList<CorridaDeNominaDto>>(
            new ListarCorridasQuery(periodo.Id, null));
        vistaDelCliente.Should().ContainSingle().Which.Numero.Should().Be(2);

        identidad.Establecer(administrador);

        // ---- Cotejo contra el archivo manual publicado en el período --------
        await casos.Ejecutar(new CambiarEstadoPeriodoCommand(periodo.Id, EstadoPeriodo.EnProceso, null, empresa.Id));

        byte[] archivoManual = Encoding.UTF8.GetBytes(
            "Clave,Total Percepciones,ISR,IMSS,Sindicato,Suma\n" +
            "1,3086.09,0,0,15494.02,24867.2370453552\n" +
            "9,3086.09,0,0,6900.00,11973.6570453552\n");

        Guid documentoManual = await SubirAsync(casos, periodo.Id, empresa.Id, TipoDocumento.Resultado, "manual.csv", archivoManual);

        CotejoDto cotejo = await casos.Ejecutar<CotejarNominaCommand, CotejoDto>(
            new CotejarNominaCommand(corrida.Id, empresa.Id, documentoManual, 0.05m));

        cotejo.NombreArchivo.Should().Be("manual.csv");
        cotejo.TotalComparaciones.Should().Be(10);
        cotejo.TotalFueraDeTolerancia.Should().Be(1);

        CotejoDto conDetalle = await casos.Consultar<ObtenerCotejoQuery, CotejoDto>(new ObtenerCotejoQuery(cotejo.Id, empresa.Id));
        conDetalle.Diferencias.Where(d => !d.DentroDeTolerancia).Should().ContainSingle()
            .Which.Should().Match<DiferenciaDeCotejoDto>(d => d.ClaveEmpleado == "9" && d.ConceptoClave == "COMPLEMENTO_SINDICAL");

        // ---- Aprobación: la nómina del período queda definitiva ------------
        await casos.Ejecutar(new CambiarEstadoCorridaCommand(corrida.Id, EstadoDeCorrida.Aprobada, empresa.Id, "Cuadra con el manual"));

        Func<Task> reprocesarAprobada = () => casos.Ejecutar<CalcularNominaCommand, CorridaDeNominaDto>(
            new CalcularNominaCommand(periodo.Id, empresa.Id, null));
        await reprocesarAprobada.Should().ThrowAsync<NominaInvalidaException>();

        IReadOnlyList<CorridaDeNominaDto> corridas = await casos.Consultar<ListarCorridasQuery, IReadOnlyList<CorridaDeNominaDto>>(
            new ListarCorridasQuery(periodo.Id, empresa.Id));
        corridas.Should().HaveCount(2);
        corridas.Single(c => c.Numero == 2).Estado.Should().Be(EstadoDeCorrida.Aprobada);
    }

    private static async Task<ContratoDto> AltaAsync(
        Casos casos, Guid empresaId, Guid razonSocialId, string clave, string nombre, string paterno, string materno, string noi, decimal sueldoReal)
    {
        EmpleadoDto empleado = await casos.Ejecutar<GuardarEmpleadoCommand, EmpleadoDto>(new GuardarEmpleadoCommand(
            null, new GuardarEmpleadoRequest(empresaId, clave, nombre, paterno, materno, null, null, null, null, null, null)));

        return await casos.Ejecutar<GuardarContratoCommand, ContratoDto>(new GuardarContratoCommand(
            empleado.Id,
            null,
            new GuardarContratoRequest(
                empresaId, razonSocialId, EsquemaDePago.Imss, noi, "Asesor", "Ensenada Advos", "Presencial",
                sueldoReal, 440.87m, 465.03m, ZonaSalarioMinimo.B, TipoDeCreditoInfonavit.Ninguno, 0m, 0m, 0m, 0m, 0m, 0m, 0m,
                HonorariosAplicaIva: false, PagaComplementoSindical: true, new DateOnly(2021, 1, 4), null)));
    }

    /// <summary>
    /// Sube un archivo al período con el mismo flujo que la web y la app:
    /// solicitud firmada, carga directa al almacén y confirmación con la huella.
    /// </summary>
    private static async Task<Guid> SubirAsync(
        Casos casos, Guid periodoId, Guid? empresaId, TipoDocumento tipo, string nombre, byte[] contenido)
    {
        SolicitarCargaResponse autorizacion = await casos.Ejecutar<SolicitarCargaDocumentoCommand, SolicitarCargaResponse>(
            new SolicitarCargaDocumentoCommand(periodoId, tipo, nombre, contenido.LongLength, empresaId));

        Documento documento = await casos.Usar<IDocumentoRepository, Documento?>(
                r => r.ObtenerPorIdSinFiltroDeEmpresaAsync(autorizacion.DocumentoId, Ct))
            ?? throw new InvalidOperationException("La solicitud de carga no registró el documento.");

        // Hace las veces del navegador: escribe el archivo directo en el almacén local.
        await using (var flujo = new MemoryStream(contenido))
        {
            await casos.Usar<ServicioDeAlmacenLocal, long>(s => s.GuardarAsync(documento.RutaBlob, flujo, contenido.LongLength, Ct));
        }

        await casos.Ejecutar<ConfirmarCargaCommand, DocumentoDto>(
            new ConfirmarCargaCommand(autorizacion.DocumentoId, HuellaArchivo.DesdeBytes(SHA256.HashData(contenido)).ValorHex));

        return autorizacion.DocumentoId;
    }
}
