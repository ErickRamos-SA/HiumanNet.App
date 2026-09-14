/* ============================================================================
   HuimanNet — Esquema de nómina, catálogos de cálculo e identidad local
   ----------------------------------------------------------------------------
   Idempotente: cada bloque comprueba la existencia del objeto antes de crearlo.
   Tipos numéricos:
     · Importes           DECIMAL(18,4)
     · Porcentajes/tasas  DECIMAL(19,8)  (fracción: 16 % = 0.16000000)
     · Días y horas       DECIMAL(9,4)
   ========================================================================== */

/* ---- Identidad local y permisos ----------------------------------------- */

IF COL_LENGTH(N'dbo.Usuarios', N'HashContrasena') IS NULL
BEGIN
    ALTER TABLE dbo.Usuarios ADD
        HashContrasena           NVARCHAR(400) NULL,
        RequiereCambioContrasena BIT           NOT NULL CONSTRAINT DF_Usuarios_RequiereCambio DEFAULT (0),
        /* 1 = Español, 2 = Inglés */
        Idioma                   TINYINT       NOT NULL CONSTRAINT DF_Usuarios_Idioma DEFAULT (1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Usuarios_Correo' AND object_id = OBJECT_ID(N'dbo.Usuarios'))
BEGIN
    CREATE UNIQUE INDEX UX_Usuarios_Correo ON dbo.Usuarios (Correo);
END;
GO

IF OBJECT_ID(N'dbo.PermisosDeUsuario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PermisosDeUsuario
    (
        UsuarioId  UNIQUEIDENTIFIER NOT NULL,
        /* Enumerado AccionDelSistema */
        Accion     TINYINT          NOT NULL,
        Habilitado BIT              NOT NULL,
        CONSTRAINT PK_PermisosDeUsuario PRIMARY KEY CLUSTERED (UsuarioId, Accion),
        CONSTRAINT FK_PermisosDeUsuario_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (Id) ON DELETE CASCADE
    );
END;
GO

/* ---- Razones sociales ---------------------------------------------------- */

IF OBJECT_ID(N'dbo.RazonesSociales', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RazonesSociales
    (
        Id                         UNIQUEIDENTIFIER  NOT NULL,
        EmpresaId                  UNIQUEIDENTIFIER  NOT NULL,
        Nombre                     NVARCHAR(200)     NOT NULL,
        Rfc                        NVARCHAR(20)      NOT NULL,
        RegistroPatronal           NVARCHAR(30)      NULL,
        /* 1 = A, 2 = B */
        Zona                       TINYINT           NOT NULL,
        /* 1 = Nomina, 2 = Maquila */
        TipoServicio               TINYINT           NOT NULL,
        SubsidioAbsorbido          BIT               NOT NULL,
        AplicaFaltasProporcionales BIT               NOT NULL,
        /* 1 = SobreCosto, 2 = SobreBrutos */
        ModalidadComision          TINYINT           NOT NULL,
        PorcentajeComision         DECIMAL(19,8)     NOT NULL,
        /* 0 = SegunZonaDelTrabajador, 1 = ForzarZonaA, 2 = ForzarZonaB */
        ZonaIsn                    TINYINT           NOT NULL,
        TasaIva                    DECIMAL(19,8)     NOT NULL,
        PorcentajeOtrosCostos      DECIMAL(19,8)     NOT NULL,
        PrimaRiesgo                DECIMAL(19,8)     NULL,
        BancoDispersor             NVARCHAR(100)     NULL,
        Activa                     BIT               NOT NULL CONSTRAINT DF_RazonesSociales_Activa DEFAULT (1),
        FechaAlta                  DATETIMEOFFSET(7) NOT NULL,
        CONSTRAINT PK_RazonesSociales PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_RazonesSociales_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas (Id)
    );

    CREATE INDEX IX_RazonesSociales_Empresa ON dbo.RazonesSociales (EmpresaId, Nombre);
END;
GO

/* ---- Empleados y contratos ---------------------------------------------- */

IF OBJECT_ID(N'dbo.Empleados', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Empleados
    (
        Id              UNIQUEIDENTIFIER  NOT NULL,
        EmpresaId       UNIQUEIDENTIFIER  NOT NULL,
        Clave           NVARCHAR(20)      NOT NULL,
        Nombre          NVARCHAR(100)     NOT NULL,
        ApellidoPaterno NVARCHAR(100)     NOT NULL,
        ApellidoMaterno NVARCHAR(100)     NULL,
        /* Datos sensibles: nunca deben escribirse en registros de log. */
        Rfc             NVARCHAR(13)      NULL,
        Curp            NVARCHAR(18)      NULL,
        Nss             NVARCHAR(11)      NULL,
        FechaNacimiento DATE              NULL,
        Correo          NVARCHAR(256)     NULL,
        Telefono        NVARCHAR(30)      NULL,
        Activo          BIT               NOT NULL CONSTRAINT DF_Empleados_Activo DEFAULT (1),
        FechaAlta       DATETIMEOFFSET(7) NOT NULL,
        CONSTRAINT PK_Empleados PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Empleados_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas (Id)
    );

    /* La clave identifica al trabajador en los archivos de incidencias y resultados. */
    CREATE UNIQUE INDEX UX_Empleados_Empresa_Clave ON dbo.Empleados (EmpresaId, Clave);
    CREATE INDEX IX_Empleados_Empresa_Nombre ON dbo.Empleados (EmpresaId, ApellidoPaterno, Nombre) INCLUDE (Clave, Activo);
END;
GO

IF OBJECT_ID(N'dbo.Contratos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Contratos
    (
        Id                           UNIQUEIDENTIFIER  NOT NULL,
        EmpleadoId                   UNIQUEIDENTIFIER  NOT NULL,
        EmpresaId                    UNIQUEIDENTIFIER  NOT NULL,
        RazonSocialId                UNIQUEIDENTIFIER  NOT NULL,
        /* 1 = Imss, 2 = Sindicato, 3 = Honorarios */
        Esquema                      TINYINT           NOT NULL,
        NumeroTrabajador             NVARCHAR(30)      NULL,
        Puesto                       NVARCHAR(100)     NULL,
        Departamento                 NVARCHAR(100)     NULL,
        TipoContrato                 NVARCHAR(50)      NULL,
        SueldoPeriodoReal            DECIMAL(18,4)     NOT NULL,
        SalarioDiarioFiscal          DECIMAL(18,4)     NOT NULL,
        SalarioDiarioIntegrado       DECIMAL(18,4)     NOT NULL,
        Zona                         TINYINT           NOT NULL,
        /* 0 = Ninguno, 1 = CuotaFija, 2 = VecesSalarioMinimo, 3 = Porcentaje */
        InfonavitTipo                TINYINT           NOT NULL,
        InfonavitValor               DECIMAL(19,8)     NOT NULL,
        InfonavitSeguroVivienda      DECIMAL(18,4)     NOT NULL,
        FonacotMensual               DECIMAL(18,4)     NOT NULL,
        PensionAlimenticiaImporte    DECIMAL(18,4)     NOT NULL,
        PensionAlimenticiaPorcentaje DECIMAL(19,8)     NOT NULL,
        PrestamoPersonalFijo         DECIMAL(18,4)     NOT NULL,
        BonoFijo                     DECIMAL(18,4)     NOT NULL,
        HonorariosAplicaIva          BIT               NOT NULL,
        /* Sueldo mixto: la diferencia con el sueldo real se paga vía sindicato. */
        PagaComplementoSindical      BIT               NOT NULL,
        FechaAlta                    DATE              NOT NULL,
        FechaBaja                    DATE              NULL,
        FechaModificacion            DATETIMEOFFSET(7) NOT NULL,
        CONSTRAINT PK_Contratos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Contratos_Empleados FOREIGN KEY (EmpleadoId) REFERENCES dbo.Empleados (Id),
        CONSTRAINT FK_Contratos_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas (Id),
        CONSTRAINT FK_Contratos_RazonesSociales FOREIGN KEY (RazonSocialId) REFERENCES dbo.RazonesSociales (Id)
    );

    /* Sostiene la consulta de contratos vigentes que alimenta el cálculo. */
    CREATE INDEX IX_Contratos_Empresa_Vigencia ON dbo.Contratos (EmpresaId, FechaAlta, FechaBaja) INCLUDE (EmpleadoId, RazonSocialId, Esquema);
    CREATE INDEX IX_Contratos_Empleado ON dbo.Contratos (EmpleadoId, FechaAlta DESC);
END;
GO

/* ---- Catálogos de cálculo ----------------------------------------------- */

IF OBJECT_ID(N'dbo.ParametrosDeCalculo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ParametrosDeCalculo
    (
        Id                UNIQUEIDENTIFIER  NOT NULL,
        Clave             NVARCHAR(64)      NOT NULL,
        Descripcion       NVARCHAR(300)     NOT NULL,
        Grupo             NVARCHAR(50)      NOT NULL,
        Valor             DECIMAL(19,8)     NOT NULL,
        Unidad            NVARCHAR(20)      NOT NULL,
        EmpresaId         UNIQUEIDENTIFIER  NULL,
        VigenteDesde      DATE              NOT NULL,
        VigenteHasta      DATE              NULL,
        FechaModificacion DATETIMEOFFSET(7) NOT NULL,
        CONSTRAINT PK_ParametrosDeCalculo PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ParametrosDeCalculo_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas (Id)
    );

    CREATE INDEX IX_ParametrosDeCalculo_Clave ON dbo.ParametrosDeCalculo (Clave, EmpresaId, VigenteDesde);
END;
GO

IF OBJECT_ID(N'dbo.TablasDeRangos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TablasDeRangos
    (
        Id                UNIQUEIDENTIFIER  NOT NULL,
        Clave             NVARCHAR(64)      NOT NULL,
        Descripcion       NVARCHAR(300)     NOT NULL,
        EmpresaId         UNIQUEIDENTIFIER  NULL,
        VigenteDesde      DATE              NOT NULL,
        VigenteHasta      DATE              NULL,
        FechaModificacion DATETIMEOFFSET(7) NOT NULL,
        CONSTRAINT PK_TablasDeRangos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_TablasDeRangos_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas (Id)
    );

    CREATE INDEX IX_TablasDeRangos_Clave ON dbo.TablasDeRangos (Clave, EmpresaId, VigenteDesde);

    CREATE TABLE dbo.RangosDeTabla
    (
        TablaId        UNIQUEIDENTIFIER NOT NULL,
        Orden          INT              NOT NULL,
        LimiteInferior DECIMAL(18,4)    NOT NULL,
        LimiteSuperior DECIMAL(18,4)    NULL,
        CuotaFija      DECIMAL(18,4)    NOT NULL,
        Porcentaje     DECIMAL(19,8)    NOT NULL,
        Valor          DECIMAL(18,4)    NOT NULL,
        CONSTRAINT PK_RangosDeTabla PRIMARY KEY CLUSTERED (TablaId, Orden),
        CONSTRAINT FK_RangosDeTabla_Tablas FOREIGN KEY (TablaId) REFERENCES dbo.TablasDeRangos (Id) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'dbo.ConceptosDeNomina', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConceptosDeNomina
    (
        Id                UNIQUEIDENTIFIER  NOT NULL,
        Clave             NVARCHAR(64)      NOT NULL,
        Nombre            NVARCHAR(120)     NOT NULL,
        Descripcion       NVARCHAR(1000)    NOT NULL,
        /* 1 = Base, 2 = Percepcion, 3 = Deduccion, 4 = Patronal, 5 = Costo, 6 = Total */
        Tipo              TINYINT           NOT NULL,
        /* Banderas: 1 = Imss, 2 = Sindicato, 4 = Honorarios */
        Esquemas          TINYINT           NOT NULL,
        Orden             INT               NOT NULL,
        Formula           NVARCHAR(4000)    NOT NULL,
        VisibleEnRecibo   BIT               NOT NULL,
        Activo            BIT               NOT NULL CONSTRAINT DF_ConceptosDeNomina_Activo DEFAULT (1),
        EmpresaId         UNIQUEIDENTIFIER  NULL,
        FechaModificacion DATETIMEOFFSET(7) NOT NULL,
        CONSTRAINT PK_ConceptosDeNomina PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ConceptosDeNomina_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas (Id)
    );

    /* Un concepto se identifica por clave y esquemas dentro de cada ámbito. */
    CREATE UNIQUE INDEX UX_ConceptosDeNomina_Clave ON dbo.ConceptosDeNomina (Clave, Esquemas, EmpresaId);
END;
GO

IF OBJECT_ID(N'dbo.ExplicacionesDeCalculo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExplicacionesDeCalculo
    (
        Id                UNIQUEIDENTIFIER  NOT NULL,
        Esquema           TINYINT           NOT NULL,
        Idioma            TINYINT           NOT NULL,
        Orden             INT               NOT NULL,
        Titulo            NVARCHAR(200)     NOT NULL,
        Cuerpo            NVARCHAR(MAX)     NOT NULL,
        FechaModificacion DATETIMEOFFSET(7) NOT NULL,
        CONSTRAINT PK_ExplicacionesDeCalculo PRIMARY KEY CLUSTERED (Id)
    );

    CREATE INDEX IX_ExplicacionesDeCalculo_Esquema ON dbo.ExplicacionesDeCalculo (Esquema, Idioma, Orden);
END;
GO

/* ---- Incidencias --------------------------------------------------------- */

IF OBJECT_ID(N'dbo.Incidencias', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Incidencias
    (
        Id                         UNIQUEIDENTIFIER  NOT NULL,
        EmpresaId                  UNIQUEIDENTIFIER  NOT NULL,
        PeriodoId                  UNIQUEIDENTIFIER  NOT NULL,
        ContratoId                 UNIQUEIDENTIFIER  NOT NULL,
        DiasPeriodo                DECIMAL(9,4)      NOT NULL,
        Vacaciones                 DECIMAL(9,4)      NOT NULL,
        Ausentismos                DECIMAL(9,4)      NOT NULL,
        Incapacidades              DECIMAL(9,4)      NOT NULL,
        Festivos                   DECIMAL(9,4)      NOT NULL,
        HorasDobles                DECIMAL(9,4)      NOT NULL,
        HorasTriples               DECIMAL(9,4)      NOT NULL,
        DomingosTrabajados         DECIMAL(9,4)      NOT NULL,
        Gratificacion              DECIMAL(18,4)     NOT NULL,
        Reembolsos                 DECIMAL(18,4)     NOT NULL,
        Teletrabajo                DECIMAL(18,4)     NOT NULL,
        Finiquito                  DECIMAL(18,4)     NOT NULL,
        Cafeteria                  DECIMAL(18,4)     NOT NULL,
        HorasDescontadas           DECIMAL(9,4)      NOT NULL,
        OtrosDescuentos            DECIMAL(18,4)     NOT NULL,
        PrestamoPersonal           DECIMAL(18,4)     NOT NULL,
        Aguinaldo                  DECIMAL(18,4)     NOT NULL,
        DescuentosFiscales         DECIMAL(18,4)     NOT NULL,
        FonacotCapturado           DECIMAL(18,4)     NOT NULL,
        DescuentoSindicalAdicional DECIMAL(18,4)     NOT NULL,
        AjusteSindical             DECIMAL(18,4)     NOT NULL,
        IsrManual                  DECIMAL(18,4)     NULL,
        /* 1 = Ordinaria, 2 = Finiquito */
        TipoMovimiento             TINYINT           NOT NULL,
        Observaciones              NVARCHAR(500)     NULL,
        CapturadoPorUsuarioId      UNIQUEIDENTIFIER  NOT NULL,
        FechaCaptura               DATETIMEOFFSET(7) NOT NULL,
        CONSTRAINT PK_Incidencias PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Incidencias_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas (Id),
        CONSTRAINT FK_Incidencias_Periodos FOREIGN KEY (PeriodoId) REFERENCES dbo.Periodos (Id),
        CONSTRAINT FK_Incidencias_Contratos FOREIGN KEY (ContratoId) REFERENCES dbo.Contratos (Id),
        CONSTRAINT FK_Incidencias_Usuarios FOREIGN KEY (CapturadoPorUsuarioId) REFERENCES dbo.Usuarios (Id)
    );

    CREATE UNIQUE INDEX UX_Incidencias_Periodo_Contrato ON dbo.Incidencias (PeriodoId, ContratoId);
    CREATE INDEX IX_Incidencias_Empresa_Periodo ON dbo.Incidencias (EmpresaId, PeriodoId);
END;
GO

/* ---- Corridas y resultados ---------------------------------------------- */

IF OBJECT_ID(N'dbo.CorridasDeNomina', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorridasDeNomina
    (
        Id                     UNIQUEIDENTIFIER  NOT NULL,
        EmpresaId              UNIQUEIDENTIFIER  NOT NULL,
        PeriodoId              UNIQUEIDENTIFIER  NOT NULL,
        Numero                 INT               NOT NULL,
        /* 1 = Calculada, 2 = Cotejada, 3 = Aprobada, 4 = Descartada */
        Estado                 TINYINT           NOT NULL,
        FechaReferencia        DATE              NOT NULL,
        FechaCalculo           DATETIMEOFFSET(7) NOT NULL,
        CalculadaPorUsuarioId  UNIQUEIDENTIFIER  NOT NULL,
        Trabajadores           INT               NOT NULL,
        TotalBruto             DECIMAL(18,4)     NOT NULL,
        TotalPercepciones      DECIMAL(18,4)     NOT NULL,
        TotalDeducciones       DECIMAL(18,4)     NOT NULL,
        TotalNeto              DECIMAL(18,4)     NOT NULL,
        TotalIsr               DECIMAL(18,4)     NOT NULL,
        TotalImssTrabajador    DECIMAL(18,4)     NOT NULL,
        TotalImssPatronal      DECIMAL(18,4)     NOT NULL,
        TotalInfonavit         DECIMAL(18,4)     NOT NULL,
        TotalIsn               DECIMAL(18,4)     NOT NULL,
        TotalComplemento       DECIMAL(18,4)     NOT NULL,
        TotalFacturable        DECIMAL(18,4)     NOT NULL,
        TotalComision          DECIMAL(18,4)     NOT NULL,
        TotalCosto             DECIMAL(18,4)     NOT NULL,
        DuracionMs             BIGINT            NOT NULL,
        Observaciones          NVARCHAR(1000)    NULL,
        Advertencias           NVARCHAR(MAX)     NULL,
        CONSTRAINT PK_CorridasDeNomina PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_CorridasDeNomina_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas (Id),
        CONSTRAINT FK_CorridasDeNomina_Periodos FOREIGN KEY (PeriodoId) REFERENCES dbo.Periodos (Id),
        CONSTRAINT FK_CorridasDeNomina_Usuarios FOREIGN KEY (CalculadaPorUsuarioId) REFERENCES dbo.Usuarios (Id)
    );

    CREATE UNIQUE INDEX UX_CorridasDeNomina_Periodo_Numero ON dbo.CorridasDeNomina (PeriodoId, Numero);
    CREATE INDEX IX_CorridasDeNomina_Empresa_Fecha ON dbo.CorridasDeNomina (EmpresaId, FechaCalculo DESC);
END;
GO

IF OBJECT_ID(N'dbo.ResultadosDeNomina', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ResultadosDeNomina
    (
        Id                  UNIQUEIDENTIFIER NOT NULL,
        CorridaId           UNIQUEIDENTIFIER NOT NULL,
        EmpresaId           UNIQUEIDENTIFIER NOT NULL,
        ContratoId          UNIQUEIDENTIFIER NOT NULL,
        EmpleadoId          UNIQUEIDENTIFIER NOT NULL,
        RazonSocialId       UNIQUEIDENTIFIER NOT NULL,
        Esquema             TINYINT          NOT NULL,
        ClaveEmpleado       NVARCHAR(20)     NOT NULL,
        NombreEmpleado      NVARCHAR(300)    NOT NULL,
        TipoMovimiento      TINYINT          NOT NULL,
        Bruto               DECIMAL(18,4)    NOT NULL,
        TotalPercepciones   DECIMAL(18,4)    NOT NULL,
        TotalDeducciones    DECIMAL(18,4)    NOT NULL,
        Neto                DECIMAL(18,4)    NOT NULL,
        Isr                 DECIMAL(18,4)    NOT NULL,
        Subsidio            DECIMAL(18,4)    NOT NULL,
        ImssTrabajador      DECIMAL(18,4)    NOT NULL,
        ImssPatronal        DECIMAL(18,4)    NOT NULL,
        InfonavitPatronal   DECIMAL(18,4)    NOT NULL,
        InfonavitTrabajador DECIMAL(18,4)    NOT NULL,
        Fonacot             DECIMAL(18,4)    NOT NULL,
        Isn                 DECIMAL(18,4)    NOT NULL,
        ComplementoSindical DECIMAL(18,4)    NOT NULL,
        Facturable          DECIMAL(18,4)    NOT NULL,
        Comision            DECIMAL(18,4)    NOT NULL,
        CostoTotal          DECIMAL(18,4)    NOT NULL,
        CostoIsr            DECIMAL(18,4)    NOT NULL,
        CostoImss           DECIMAL(18,4)    NOT NULL,
        CostoInfonavit      DECIMAL(18,4)    NOT NULL,
        CostoOtros          DECIMAL(18,4)    NOT NULL,
        Advertencia         NVARCHAR(1000)   NULL,
        /* Detalle de conceptos en orden de evaluación: {"CLAVE": importe, ...}.
           Una columna por resultado en lugar de una fila por concepto: una
           corrida de 5 000 trabajadores con 100 conceptos serían 500 000 filas. */
        Conceptos           NVARCHAR(MAX)    NOT NULL,
        CONSTRAINT PK_ResultadosDeNomina PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ResultadosDeNomina_Corridas FOREIGN KEY (CorridaId) REFERENCES dbo.CorridasDeNomina (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_ResultadosDeNomina_Corrida ON dbo.ResultadosDeNomina (CorridaId, RazonSocialId, ClaveEmpleado);
END;
GO

/* ---- Cotejos -------------------------------------------------------------- */

IF OBJECT_ID(N'dbo.CotejosDeNomina', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CotejosDeNomina
    (
        Id                     UNIQUEIDENTIFIER  NOT NULL,
        CorridaId              UNIQUEIDENTIFIER  NOT NULL,
        EmpresaId              UNIQUEIDENTIFIER  NOT NULL,
        FechaCotejo            DATETIMEOFFSET(7) NOT NULL,
        UsuarioId              UNIQUEIDENTIFIER  NOT NULL,
        NombreArchivo          NVARCHAR(255)     NOT NULL,
        ToleranciaAbsoluta     DECIMAL(18,4)     NOT NULL,
        TotalComparaciones     INT               NOT NULL,
        TotalFueraDeTolerancia INT               NOT NULL,
        /* Detalle de comparaciones en JSON (una entrada por trabajador y concepto). */
        Diferencias            NVARCHAR(MAX)     NOT NULL,
        CONSTRAINT PK_CotejosDeNomina PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_CotejosDeNomina_Corridas FOREIGN KEY (CorridaId) REFERENCES dbo.CorridasDeNomina (Id) ON DELETE CASCADE,
        CONSTRAINT FK_CotejosDeNomina_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (Id)
    );

    CREATE INDEX IX_CotejosDeNomina_Corrida ON dbo.CotejosDeNomina (CorridaId, FechaCotejo DESC);
END;
GO
