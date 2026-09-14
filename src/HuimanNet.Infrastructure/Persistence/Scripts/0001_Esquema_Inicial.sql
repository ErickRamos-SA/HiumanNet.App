/* ============================================================================
   HuimanNet — Esquema inicial (V1: portal de intercambio de documentos)
   ----------------------------------------------------------------------------
   Migraciones con SQL versionado, no con un ORM (ESPECIFICACION.md §6.1).
   Cada script se aplica una sola vez y queda anotado en dbo.__HistorialScripts.
   El script es idempotente: puede volver a ejecutarse sin efectos colaterales.
   ========================================================================== */

IF OBJECT_ID(N'dbo.Empresas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Empresas
    (
        Id                  UNIQUEIDENTIFIER    NOT NULL,
        RazonSocial         NVARCHAR(200)       NOT NULL,
        IdentificadorFiscal NVARCHAR(50)        NOT NULL,
        PrefijoContenedor   NVARCHAR(100)       NOT NULL,
        Activa              BIT                 NOT NULL CONSTRAINT DF_Empresas_Activa DEFAULT (1),
        FechaAlta           DATETIMEOFFSET(7)   NOT NULL,
        CONSTRAINT PK_Empresas PRIMARY KEY CLUSTERED (Id)
    );

    CREATE UNIQUE INDEX UX_Empresas_IdentificadorFiscal
        ON dbo.Empresas (IdentificadorFiscal);
END;
GO

IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios
    (
        Id                   UNIQUEIDENTIFIER   NOT NULL,
        IdentificadorExterno NVARCHAR(128)      NOT NULL,
        NombreCompleto       NVARCHAR(200)      NOT NULL,
        Correo               NVARCHAR(256)      NOT NULL,
        /* 1 = ClienteEmpresa, 2 = OperadorNomina, 3 = Administrador */
        Rol                  TINYINT            NOT NULL,
        EmpresaId            UNIQUEIDENTIFIER   NULL,
        Activo               BIT                NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
        FechaAlta            DATETIMEOFFSET(7)  NOT NULL,
        CONSTRAINT PK_Usuarios PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Usuarios_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas (Id),
        /* Un usuario de empresa cliente debe estar vinculado a una empresa. */
        CONSTRAINT CK_Usuarios_EmpresaSegunRol CHECK (Rol <> 1 OR EmpresaId IS NOT NULL)
    );

    CREATE UNIQUE INDEX UX_Usuarios_IdentificadorExterno
        ON dbo.Usuarios (IdentificadorExterno);

    CREATE INDEX IX_Usuarios_EmpresaId_Rol
        ON dbo.Usuarios (EmpresaId, Rol) INCLUDE (Correo, NombreCompleto, Activo);
END;
GO

IF OBJECT_ID(N'dbo.Periodos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Periodos
    (
        Id               UNIQUEIDENTIFIER   NOT NULL,
        EmpresaId        UNIQUEIDENTIFIER   NOT NULL,
        Anio             SMALLINT           NOT NULL,
        Mes              TINYINT            NOT NULL,
        Consecutivo      TINYINT            NOT NULL,
        Descripcion      NVARCHAR(200)      NOT NULL,
        /* 1 = Abierto, 2 = Recibido, 3 = EnProceso, 4 = ResultadosDisponibles, 5 = Cerrado */
        Estado           TINYINT            NOT NULL,
        FechaApertura    DATETIMEOFFSET(7)  NOT NULL,
        FechaLimiteCarga DATETIMEOFFSET(7)  NULL,
        FechaCierre      DATETIMEOFFSET(7)  NULL,
        CONSTRAINT PK_Periodos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Periodos_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas (Id),
        CONSTRAINT CK_Periodos_Mes CHECK (Mes BETWEEN 1 AND 12),
        CONSTRAINT CK_Periodos_Consecutivo CHECK (Consecutivo BETWEEN 1 AND 5)
    );

    /* Una empresa no puede tener dos períodos en la misma posición del calendario. */
    CREATE UNIQUE INDEX UX_Periodos_Empresa_Calendario
        ON dbo.Periodos (EmpresaId, Anio, Mes, Consecutivo);

    /* Sostiene la bandeja del operador (consulta transversal por estado). */
    CREATE INDEX IX_Periodos_Estado_FechaApertura
        ON dbo.Periodos (Estado, FechaApertura);
END;
GO

IF OBJECT_ID(N'dbo.Documentos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Documentos
    (
        Id                   UNIQUEIDENTIFIER   NOT NULL,
        EmpresaId            UNIQUEIDENTIFIER   NOT NULL,
        PeriodoId            UNIQUEIDENTIFIER   NOT NULL,
        /* 1 = Incidencia, 2 = DatosEmpleado, 3 = Resultado, 4 = Ajuste */
        Tipo                 TINYINT            NOT NULL,
        NombreOriginal       NVARCHAR(255)      NOT NULL,
        /* Ruta generada por el sistema: {empresa}/{periodo}/{tipo}/{guid}{ext} */
        RutaBlob             NVARCHAR(400)      NOT NULL,
        TamanoBytes          BIGINT             NOT NULL,
        /* 1 = Pendiente, 2 = Escaneando, 3 = Disponible, 4 = EnCuarentena, 5 = Descartado */
        Estado               TINYINT            NOT NULL,
        HuellaSha256         CHAR(64)           NULL,
        CargadoPorUsuarioId  UNIQUEIDENTIFIER   NOT NULL,
        FechaSolicitud       DATETIMEOFFSET(7)  NOT NULL,
        FechaCargaConfirmada DATETIMEOFFSET(7)  NULL,
        FechaEscaneo         DATETIMEOFFSET(7)  NULL,
        MotivoCuarentena     NVARCHAR(500)      NULL,
        CONSTRAINT PK_Documentos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Documentos_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas (Id),
        CONSTRAINT FK_Documentos_Periodos FOREIGN KEY (PeriodoId) REFERENCES dbo.Periodos (Id),
        CONSTRAINT FK_Documentos_Usuarios FOREIGN KEY (CargadoPorUsuarioId) REFERENCES dbo.Usuarios (Id),
        CONSTRAINT CK_Documentos_TamanoPositivo CHECK (TamanoBytes > 0)
    );

    CREATE UNIQUE INDEX UX_Documentos_RutaBlob ON dbo.Documentos (RutaBlob);

    /* Índice principal de la bandeja: siempre se filtra por empresa (aislamiento). */
    CREATE INDEX IX_Documentos_Empresa_Periodo_Tipo
        ON dbo.Documentos (EmpresaId, PeriodoId, Tipo, Estado)
        INCLUDE (NombreOriginal, TamanoBytes, FechaSolicitud, CargadoPorUsuarioId);
END;
GO

IF OBJECT_ID(N'dbo.Lotes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Lotes
    (
        Id                 UNIQUEIDENTIFIER   NOT NULL,
        EmpresaId          UNIQUEIDENTIFIER   NOT NULL,
        PeriodoId          UNIQUEIDENTIFIER   NOT NULL,
        CreadoPorUsuarioId UNIQUEIDENTIFIER   NOT NULL,
        FechaCreacion      DATETIMEOFFSET(7)  NOT NULL,
        Comentario         NVARCHAR(500)      NULL,
        CONSTRAINT PK_Lotes PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Lotes_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas (Id),
        CONSTRAINT FK_Lotes_Periodos FOREIGN KEY (PeriodoId) REFERENCES dbo.Periodos (Id),
        CONSTRAINT FK_Lotes_Usuarios FOREIGN KEY (CreadoPorUsuarioId) REFERENCES dbo.Usuarios (Id)
    );

    CREATE TABLE dbo.LoteDocumentos
    (
        LoteId      UNIQUEIDENTIFIER NOT NULL,
        DocumentoId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_LoteDocumentos PRIMARY KEY CLUSTERED (LoteId, DocumentoId),
        CONSTRAINT FK_LoteDocumentos_Lotes FOREIGN KEY (LoteId) REFERENCES dbo.Lotes (Id),
        CONSTRAINT FK_LoteDocumentos_Documentos FOREIGN KEY (DocumentoId) REFERENCES dbo.Documentos (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.Auditoria', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Auditoria
    (
        Id          UNIQUEIDENTIFIER   NOT NULL,
        Momento     DATETIMEOFFSET(7)  NOT NULL,
        /* 1 = SolicitudDeCarga, 2 = CargaConfirmada, 3 = Descarga,
           4 = ResultadoDeEscaneo, 5 = CambioEstadoPeriodo, 6 = AccesoDenegado */
        Accion      TINYINT            NOT NULL,
        UsuarioId   UNIQUEIDENTIFIER   NOT NULL,
        EmpresaId   UNIQUEIDENTIFIER   NULL,
        RecursoTipo NVARCHAR(64)       NOT NULL,
        RecursoId   UNIQUEIDENTIFIER   NULL,
        Exito       BIT                NOT NULL,
        /* Nunca debe contener datos personales ni identificadores fiscales. */
        Detalle     NVARCHAR(1000)     NULL,
        DireccionIp NVARCHAR(64)       NULL,
        CONSTRAINT PK_Auditoria PRIMARY KEY CLUSTERED (Id)
    );

    CREATE INDEX IX_Auditoria_Momento
        ON dbo.Auditoria (Momento DESC) INCLUDE (Accion, UsuarioId, EmpresaId, Exito);

    CREATE INDEX IX_Auditoria_Empresa_Momento
        ON dbo.Auditoria (EmpresaId, Momento DESC);
END;
GO
