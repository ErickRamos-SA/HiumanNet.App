/* ============================================================================
   HuimanNet — Empresas adicionales de los usuarios de empresa cliente
   ----------------------------------------------------------------------------
   Un usuario de empresa cliente conserva su empresa principal en
   dbo.Usuarios.EmpresaId (con la que entra por omisión) y puede operar además
   sobre las empresas de esta tabla. El operador de nómina y el administrador
   no la usan: operan sobre todas las empresas.
   Idempotente: comprueba la existencia de la tabla antes de crearla.
   ========================================================================== */

IF OBJECT_ID(N'dbo.EmpresasAdicionalesDeUsuario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmpresasAdicionalesDeUsuario
    (
        UsuarioId UNIQUEIDENTIFIER NOT NULL,
        EmpresaId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_EmpresasAdicionalesDeUsuario PRIMARY KEY CLUSTERED (UsuarioId, EmpresaId),
        CONSTRAINT FK_EmpresasAdicionalesDeUsuario_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (Id) ON DELETE CASCADE,
        CONSTRAINT FK_EmpresasAdicionalesDeUsuario_Empresas FOREIGN KEY (EmpresaId) REFERENCES dbo.Empresas (Id)
    );

    /* Sostiene la búsqueda de los usuarios de una empresa (avisos y administración). */
    CREATE INDEX IX_EmpresasAdicionalesDeUsuario_Empresa ON dbo.EmpresasAdicionalesDeUsuario (EmpresaId) INCLUDE (UsuarioId);
END;
GO
