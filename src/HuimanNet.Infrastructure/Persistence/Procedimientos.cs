namespace HuimanNet.Infrastructure.Persistence;

/// <summary>
/// Nombres de los procedimientos almacenados por los que pasa todo el acceso a
/// datos.
/// </summary>
/// <remarks>
/// Única lista de nombres del lado del código: los repositorios y las consultas
/// los toman de aquí en lugar de escribir cadenas sueltas, y las pruebas
/// comprueban contra la base de datos que cada uno existe y que no sobra
/// ninguno. Su definición vive en <c>Persistence/Procedimientos/*.sql</c>, un
/// archivo por módulo.
/// </remarks>
public static class Procedimientos
{
    /// <summary>Procedimientos de <c>dbo.Empresas</c>.</summary>
    public static class Empresas
    {
        /// <summary>Obtiene la empresa con el identificador indicado.</summary>
        public const string Obtener = "dbo.Empresas_Obtener";

        /// <summary>Lista las empresas, todas o sólo las activas.</summary>
        public const string Listar = "dbo.Empresas_Listar";

        /// <summary>Da de alta una empresa.</summary>
        public const string Insertar = "dbo.Empresas_Insertar";

        /// <summary>Actualiza los datos editables de una empresa.</summary>
        public const string Actualizar = "dbo.Empresas_Actualizar";

        /// <summary>Obtiene el resumen (identificador, nombre y estado) de una empresa.</summary>
        public const string ObtenerResumen = "dbo.Empresas_ObtenerResumen";

        /// <summary>Lista el resumen de las empresas para los selectores de la interfaz.</summary>
        public const string ListarResumen = "dbo.Empresas_ListarResumen";

        /// <summary>Obtiene el detalle administrativo de una empresa, con sus recuentos.</summary>
        public const string ObtenerDetalle = "dbo.Empresas_ObtenerDetalle";

        /// <summary>Lista el detalle administrativo de las empresas, con sus recuentos.</summary>
        public const string ListarDetalle = "dbo.Empresas_ListarDetalle";
    }

    /// <summary>Procedimientos de <c>dbo.Periodos</c>.</summary>
    public static class Periodos
    {
        /// <summary>Obtiene un período de una empresa.</summary>
        public const string Obtener = "dbo.Periodos_Obtener";

        /// <summary>Obtiene el período de una empresa por su posición en el calendario.</summary>
        public const string ObtenerPorCalendario = "dbo.Periodos_ObtenerPorCalendario";

        /// <summary>Lista los períodos de una empresa.</summary>
        public const string ListarPorEmpresa = "dbo.Periodos_ListarPorEmpresa";

        /// <summary>Lista los períodos que esperan trabajo del operador de nómina.</summary>
        public const string ListarBandeja = "dbo.Periodos_ListarBandeja";

        /// <summary>Abre un período.</summary>
        public const string Insertar = "dbo.Periodos_Insertar";

        /// <summary>Actualiza la descripción, el estado y las fechas de un período.</summary>
        public const string Actualizar = "dbo.Periodos_Actualizar";

        /// <summary>Obtiene un período con el nombre de su empresa y sus recuentos de documentos.</summary>
        public const string ObtenerDetalle = "dbo.Periodos_ObtenerDetalle";

        /// <summary>Lista los períodos de una empresa con sus recuentos de documentos.</summary>
        public const string ListarDetallePorEmpresa = "dbo.Periodos_ListarDetallePorEmpresa";

        /// <summary>Lista la bandeja del operador con los recuentos de documentos.</summary>
        public const string ListarDetalleBandeja = "dbo.Periodos_ListarDetalleBandeja";
    }

    /// <summary>Procedimientos de <c>dbo.Documentos</c>.</summary>
    public static class Documentos
    {
        /// <summary>Obtiene un documento de una empresa.</summary>
        public const string Obtener = "dbo.Documentos_Obtener";

        /// <summary>Obtiene un documento sin filtrar por empresa, para el escaneo de malware.</summary>
        public const string ObtenerSinEmpresa = "dbo.Documentos_ObtenerSinEmpresa";

        /// <summary>Obtiene el documento almacenado en una ruta de blob.</summary>
        public const string ObtenerPorRuta = "dbo.Documentos_ObtenerPorRuta";

        /// <summary>Lista los documentos de un período.</summary>
        public const string ListarPorPeriodo = "dbo.Documentos_ListarPorPeriodo";

        /// <summary>Cuenta los documentos disponibles de un tipo en un período.</summary>
        public const string ContarDisponibles = "dbo.Documentos_ContarDisponibles";

        /// <summary>Registra la solicitud de carga de un documento.</summary>
        public const string Insertar = "dbo.Documentos_Insertar";

        /// <summary>Actualiza el estado, la huella y las fechas de un documento.</summary>
        public const string Actualizar = "dbo.Documentos_Actualizar";

        /// <summary>Lista los documentos de un período con el nombre de quien los cargó.</summary>
        public const string ListarDetallePorPeriodo = "dbo.Documentos_ListarDetallePorPeriodo";
    }

    /// <summary>Procedimientos de <c>dbo.RazonesSociales</c>.</summary>
    public static class RazonesSociales
    {
        /// <summary>Obtiene una razón social de una empresa.</summary>
        public const string Obtener = "dbo.RazonesSociales_Obtener";

        /// <summary>Lista las razones sociales de una empresa.</summary>
        public const string ListarPorEmpresa = "dbo.RazonesSociales_ListarPorEmpresa";

        /// <summary>Da de alta una razón social.</summary>
        public const string Insertar = "dbo.RazonesSociales_Insertar";

        /// <summary>Actualiza una razón social.</summary>
        public const string Actualizar = "dbo.RazonesSociales_Actualizar";
    }

    /// <summary>Procedimientos de <c>dbo.Auditoria</c>.</summary>
    public static class Auditoria
    {
        /// <summary>Añade un asiento a la bitácora.</summary>
        public const string Insertar = "dbo.Auditoria_Insertar";

        /// <summary>Consulta la bitácora con filtros y paginación.</summary>
        public const string Consultar = "dbo.Auditoria_Consultar";
    }

    /// <summary>Procedimientos del panel de inicio.</summary>
    public static class Inicio
    {
        /// <summary>Obtiene indicadores, períodos abiertos y corridas recientes en un viaje.</summary>
        public const string ObtenerDatos = "dbo.Inicio_ObtenerDatos";
    }

    /// <summary>Procedimientos de <c>dbo.Empleados</c> y <c>dbo.Contratos</c>.</summary>
    public static class Empleados
    {
        /// <summary>Obtiene un empleado de una empresa.</summary>
        public const string Obtener = "dbo.Empleados_Obtener";

        /// <summary>Obtiene un empleado por su clave dentro de la empresa.</summary>
        public const string ObtenerPorClave = "dbo.Empleados_ObtenerPorClave";

        /// <summary>Lista los empleados de una empresa.</summary>
        public const string ListarPorEmpresa = "dbo.Empleados_ListarPorEmpresa";

        /// <summary>Da de alta un empleado.</summary>
        public const string Insertar = "dbo.Empleados_Insertar";

        /// <summary>Actualiza los datos personales y el estado de un empleado.</summary>
        public const string Actualizar = "dbo.Empleados_Actualizar";

        /// <summary>Lista empleados paginados, con su total, para la pantalla de empleados.</summary>
        public const string ListarPaginado = "dbo.Empleados_ListarPaginado";

        /// <summary>Obtiene un empleado con sus contratos.</summary>
        public const string ObtenerDetalle = "dbo.Empleados_ObtenerDetalle";

        /// <summary>Obtiene un contrato de una empresa.</summary>
        public const string ObtenerContrato = "dbo.Contratos_Obtener";

        /// <summary>Lista los contratos de un empleado.</summary>
        public const string ListarContratosDeEmpleado = "dbo.Contratos_ListarPorEmpleado";

        /// <summary>Lista los contratos vigentes de una empresa en una fecha.</summary>
        public const string ListarContratosVigentes = "dbo.Contratos_ListarVigentes";

        /// <summary>Da de alta un contrato.</summary>
        public const string InsertarContrato = "dbo.Contratos_Insertar";

        /// <summary>Actualiza un contrato.</summary>
        public const string ActualizarContrato = "dbo.Contratos_Actualizar";
    }

    /// <summary>Procedimientos de <c>dbo.Incidencias</c>.</summary>
    public static class Incidencias
    {
        /// <summary>Obtiene una incidencia de una empresa.</summary>
        public const string Obtener = "dbo.Incidencias_Obtener";

        /// <summary>Obtiene la incidencia de un contrato en un período.</summary>
        public const string ObtenerPorContrato = "dbo.Incidencias_ObtenerPorContrato";

        /// <summary>Lista las incidencias capturadas de un período.</summary>
        public const string ListarPorPeriodo = "dbo.Incidencias_ListarPorPeriodo";

        /// <summary>Captura una incidencia.</summary>
        public const string Insertar = "dbo.Incidencias_Insertar";

        /// <summary>Actualiza una incidencia capturada.</summary>
        public const string Actualizar = "dbo.Incidencias_Actualizar";

        /// <summary>Borra una incidencia capturada.</summary>
        public const string Eliminar = "dbo.Incidencias_Eliminar";

        /// <summary>Lista una fila por contrato vigente, con su incidencia si la hay.</summary>
        public const string ListarDetallePorPeriodo = "dbo.Incidencias_ListarDetallePorPeriodo";
    }

    /// <summary>Procedimientos de los catálogos de cálculo.</summary>
    public static class Catalogos
    {
        /// <summary>Lista los parámetros globales y los de una empresa.</summary>
        public const string ParametrosListar = "dbo.Catalogos_ParametrosListar";

        /// <summary>Obtiene un parámetro de cálculo.</summary>
        public const string ParametroObtener = "dbo.Catalogos_ParametroObtener";

        /// <summary>Da de alta un parámetro de cálculo.</summary>
        public const string ParametroInsertar = "dbo.Catalogos_ParametroInsertar";

        /// <summary>Actualiza un parámetro de cálculo.</summary>
        public const string ParametroActualizar = "dbo.Catalogos_ParametroActualizar";

        /// <summary>Borra un parámetro de cálculo.</summary>
        public const string ParametroEliminar = "dbo.Catalogos_ParametroEliminar";

        /// <summary>Lista las tablas de rangos globales y las de una empresa, con sus rangos.</summary>
        public const string TablasListar = "dbo.Catalogos_TablasListar";

        /// <summary>Obtiene una tabla de rangos con sus rangos.</summary>
        public const string TablaObtener = "dbo.Catalogos_TablaObtener";

        /// <summary>Da de alta una tabla de rangos con sus rangos.</summary>
        public const string TablaInsertar = "dbo.Catalogos_TablaInsertar";

        /// <summary>Actualiza una tabla de rangos y sustituye sus rangos.</summary>
        public const string TablaActualizar = "dbo.Catalogos_TablaActualizar";

        /// <summary>Borra una tabla de rangos y sus rangos.</summary>
        public const string TablaEliminar = "dbo.Catalogos_TablaEliminar";

        /// <summary>Lista los conceptos globales y los de una empresa.</summary>
        public const string ConceptosListar = "dbo.Catalogos_ConceptosListar";

        /// <summary>Obtiene un concepto de nómina.</summary>
        public const string ConceptoObtener = "dbo.Catalogos_ConceptoObtener";

        /// <summary>Da de alta un concepto de nómina.</summary>
        public const string ConceptoInsertar = "dbo.Catalogos_ConceptoInsertar";

        /// <summary>Actualiza un concepto de nómina.</summary>
        public const string ConceptoActualizar = "dbo.Catalogos_ConceptoActualizar";

        /// <summary>Borra un concepto de nómina.</summary>
        public const string ConceptoEliminar = "dbo.Catalogos_ConceptoEliminar";

        /// <summary>Lista las secciones de la explicación del cálculo.</summary>
        public const string ExplicacionesListar = "dbo.Catalogos_ExplicacionesListar";

        /// <summary>Obtiene una sección de la explicación del cálculo.</summary>
        public const string ExplicacionObtener = "dbo.Catalogos_ExplicacionObtener";

        /// <summary>Da de alta una sección de la explicación.</summary>
        public const string ExplicacionInsertar = "dbo.Catalogos_ExplicacionInsertar";

        /// <summary>Actualiza una sección de la explicación.</summary>
        public const string ExplicacionActualizar = "dbo.Catalogos_ExplicacionActualizar";

        /// <summary>Borra una sección de la explicación.</summary>
        public const string ExplicacionEliminar = "dbo.Catalogos_ExplicacionEliminar";
    }

    /// <summary>Procedimientos de <c>dbo.Usuarios</c> y sus tablas asociadas.</summary>
    public static class Usuarios
    {
        /// <summary>Obtiene un usuario con sus permisos y empresas adicionales.</summary>
        public const string Obtener = "dbo.Usuarios_Obtener";

        /// <summary>Obtiene un usuario por su identificador en el proveedor de identidad.</summary>
        public const string ObtenerPorIdentificadorExterno = "dbo.Usuarios_ObtenerPorIdentificadorExterno";

        /// <summary>Obtiene un usuario por su correo.</summary>
        public const string ObtenerPorCorreo = "dbo.Usuarios_ObtenerPorCorreo";

        /// <summary>Lista usuarios con sus permisos y empresas adicionales.</summary>
        public const string Listar = "dbo.Usuarios_Listar";

        /// <summary>Lista los usuarios activos de un rol que reciben los avisos de una empresa.</summary>
        public const string ListarDestinatarios = "dbo.Usuarios_ListarDestinatarios";

        /// <summary>Da de alta un usuario con sus permisos y empresas adicionales.</summary>
        public const string Insertar = "dbo.Usuarios_Insertar";

        /// <summary>Actualiza un usuario y sustituye sus permisos y empresas adicionales.</summary>
        public const string Actualizar = "dbo.Usuarios_Actualizar";

        /// <summary>Lista usuarios para la administración, con el nombre de su empresa.</summary>
        public const string ListarParaAdministracion = "dbo.Usuarios_ListarParaAdministracion";
    }

    /// <summary>Procedimientos de corridas, resultados y cotejos de nómina.</summary>
    public static class Nomina
    {
        /// <summary>Obtiene una corrida de una empresa.</summary>
        public const string CorridaObtener = "dbo.Corridas_Obtener";

        /// <summary>Lista las corridas de un período.</summary>
        public const string CorridasListarPorPeriodo = "dbo.Corridas_ListarPorPeriodo";

        /// <summary>Calcula el número que le toca a la siguiente corrida del período.</summary>
        public const string CorridaSiguienteNumero = "dbo.Corridas_SiguienteNumero";

        /// <summary>Guarda el encabezado de una corrida.</summary>
        public const string CorridaInsertar = "dbo.Corridas_Insertar";

        /// <summary>Actualiza el estado y las observaciones de una corrida.</summary>
        public const string CorridaActualizar = "dbo.Corridas_Actualizar";

        /// <summary>Lista los resultados de una corrida con su detalle de conceptos.</summary>
        public const string ResultadosListarPorCorrida = "dbo.Resultados_ListarPorCorrida";

        /// <summary>Obtiene un resultado con su detalle de conceptos.</summary>
        public const string ResultadoObtener = "dbo.Resultados_Obtener";

        /// <summary>Lista las corridas de un período con la clave del período y quién las calculó.</summary>
        public const string CorridasListarDetallePorPeriodo = "dbo.Corridas_ListarDetallePorPeriodo";

        /// <summary>Lista las corridas más recientes para el panel de inicio.</summary>
        public const string CorridasListarDetalleRecientes = "dbo.Corridas_ListarDetalleRecientes";

        /// <summary>Obtiene una corrida con la clave de su período y quién la calculó.</summary>
        public const string CorridaObtenerDetalle = "dbo.Corridas_ObtenerDetalle";

        /// <summary>Lista los resultados de una corrida sin el detalle de conceptos.</summary>
        public const string ResultadosListarDetalle = "dbo.Resultados_ListarDetalle";

        /// <summary>Obtiene un resultado sin el detalle de conceptos.</summary>
        public const string ResultadoObtenerDetalle = "dbo.Resultados_ObtenerDetalle";

        /// <summary>Guarda un cotejo con sus diferencias.</summary>
        public const string CotejoInsertar = "dbo.Cotejos_Insertar";

        /// <summary>Obtiene un cotejo con sus diferencias.</summary>
        public const string CotejoObtener = "dbo.Cotejos_Obtener";

        /// <summary>Lista los cotejos de una corrida, sin diferencias.</summary>
        public const string CotejosListarPorCorrida = "dbo.Cotejos_ListarPorCorrida";

        /// <summary>Lista los cotejos de una corrida con el nombre de quien los hizo.</summary>
        public const string CotejosListarDetalle = "dbo.Cotejos_ListarDetalle";
    }
}
