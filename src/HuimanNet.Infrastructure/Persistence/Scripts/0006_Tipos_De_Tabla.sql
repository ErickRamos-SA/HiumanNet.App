/* ============================================================================
   HuimanNet — Tipos de tabla para los procedimientos almacenados
   ----------------------------------------------------------------------------
   Los procedimientos que sustituyen una colección completa (los rangos de una
   tabla de cálculo, los permisos de un usuario, sus empresas adicionales)
   reciben las filas en un parámetro de tabla. Antes el repositorio armaba una
   lista de VALUES en un bucle: ahora envía las filas de una vez y el plan de
   ejecución se reutiliza sea cual sea el número de filas.

   Un tipo de tabla no admite CREATE OR ALTER y no puede modificarse mientras un
   procedimiento lo use, por eso vive en un script numerado y no en la carpeta
   de procedimientos. Idempotente: comprueba el tipo antes de crearlo.
   ========================================================================== */

IF TYPE_ID(N'dbo.IdentificadoresTipo') IS NULL
BEGIN
    CREATE TYPE dbo.IdentificadoresTipo AS TABLE
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF TYPE_ID(N'dbo.RangosDeTablaTipo') IS NULL
BEGIN
    CREATE TYPE dbo.RangosDeTablaTipo AS TABLE
    (
        Orden          INT           NOT NULL,
        LimiteInferior DECIMAL(18,4) NOT NULL,
        LimiteSuperior DECIMAL(18,4) NULL,
        CuotaFija      DECIMAL(18,4) NOT NULL,
        Porcentaje     DECIMAL(19,8) NOT NULL,
        Valor          DECIMAL(18,4) NOT NULL,
        PRIMARY KEY CLUSTERED (Orden)
    );
END;
GO

IF TYPE_ID(N'dbo.PermisosDeUsuarioTipo') IS NULL
BEGIN
    CREATE TYPE dbo.PermisosDeUsuarioTipo AS TABLE
    (
        Accion     TINYINT NOT NULL,
        Habilitado BIT     NOT NULL,
        PRIMARY KEY CLUSTERED (Accion)
    );
END;
GO
