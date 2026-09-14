/* ============================================================================
   HuimanNet — Datos semilla imprescindibles
   ----------------------------------------------------------------------------
   La cuenta de sistema existe para que la bitácora nunca contenga asientos
   huérfanos: las acciones automáticas —en particular los veredictos del escaneo
   de malware— se firman con ella.
   Su identificador coincide con Application.Common.IdentidadesDelSistema.Sistema.
   ========================================================================== */

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Id = '00000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO dbo.Usuarios
        (Id, IdentificadorExterno, NombreCompleto, Correo, Rol, EmpresaId, Activo, FechaAlta)
    VALUES
        ('00000000-0000-0000-0000-000000000001',
         N'system:huimannet',
         N'Sistema HuimanNet',
         N'no-reply@huimannet.local',
         3,      /* Administrador */
         NULL,
         0,      /* Inactivo: no puede iniciar sesión */
         SYSDATETIMEOFFSET());
END;
GO
