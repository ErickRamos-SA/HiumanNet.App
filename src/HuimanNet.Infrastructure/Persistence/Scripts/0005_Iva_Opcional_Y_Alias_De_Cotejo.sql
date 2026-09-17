/* ============================================================================
   HuimanNet — IVA opcional por razón social y alias de cotejo en el catálogo
   ----------------------------------------------------------------------------
   1. dbo.RazonesSociales.TasaIva admite NULL: la razón social que no define una
      tasa propia usa el parámetro general IVA_TASA del catálogo vigente, igual
      que PrimaRiesgo usa IMSS_PRT_DEFAULT. Las tasas ya capturadas se conservan.
   2. dbo.ConceptosDeNomina.AliasDeCotejo guarda, separados por '|', otros
      encabezados con los que el concepto aparece en el archivo de resultados
      manual. Sustituye la tabla de equivalencias que estaba escrita en el
      código; se editan desde la administración de catálogos.
   Idempotente: comprueba la columna antes de crearla y sólo rellena los
   conceptos globales que aún no tienen alias.
   ========================================================================== */

ALTER TABLE dbo.RazonesSociales ALTER COLUMN TasaIva DECIMAL(19,8) NULL;
GO

IF COL_LENGTH(N'dbo.ConceptosDeNomina', N'AliasDeCotejo') IS NULL
BEGIN
    ALTER TABLE dbo.ConceptosDeNomina ADD AliasDeCotejo NVARCHAR(1000) NULL;
END;
GO

/* Equivalencias de la hoja de nómina del modelo de referencia. */
UPDATE c
SET    c.AliasDeCotejo = a.Alias
FROM   dbo.ConceptosDeNomina AS c
INNER JOIN (VALUES
    (N'DIAS_TRABAJADOS_FISCAL',  N'DIASTRABAJADOS'),
    (N'FALTAS_FISCAL',           N'FALTAS'),
    (N'AGUINALDO_FISCAL',        N'AGUINALDO'),
    (N'VACACIONES_FISCAL',       N'VACACIONES'),
    (N'PRIMA_VACACIONAL',        N'PRIMAVACACIONAL'),
    (N'GRATIFICACION_FISCAL',    N'GRATIFICACION'),
    (N'TELETRABAJO_FISCAL',      N'TELETRABAJO'),
    (N'TOTAL_PERCEPCIONES',      N'TOTALPERCEPCIONES'),
    (N'SUBSIDIO_ENTREGADO',      N'SUBSIDIOEMPLEO|SUBSIDIO'),
    (N'IMSS_TRABAJADOR',         N'IMSS|CUOTAOBRERA'),
    (N'FONACOT',                 N'CREDITOFONACOT'),
    (N'INFONAVIT_TRABAJADOR',    N'CREDITOINFONAVIT|INFONAVIT'),
    (N'PENSION_ALIMENTICIA',     N'PENSIONALIMENTICIA'),
    (N'TOTAL_DEDUCCIONES',       N'TOTALDEDUCCIONES'),
    (N'NETO_PAGADO',             N'NETOPAGADO|NETO'),
    (N'COMPLEMENTO_SINDICAL',    N'SINDICATO|COMPLEMENTOSINDICAL'),
    (N'BRUTO_INCIDENCIAS',       N'TOTALDEPERCEPCIONES|BRUTO'),
    (N'TOTAL_NOMINA_FACTURABLE', N'TOTALNOMINA'),
    (N'COSTO_TOTAL',             N'SUMA|COSTOTOTAL'),
    (N'BASE_GRAVABLE',           N'BASEGRAVABLE'),
    (N'ISR_DETERMINADO',         N'IMPUESTODETERMINADO')
) AS a (Clave, Alias) ON a.Clave = c.Clave
WHERE  c.EmpresaId IS NULL
  AND  c.AliasDeCotejo IS NULL;
GO
