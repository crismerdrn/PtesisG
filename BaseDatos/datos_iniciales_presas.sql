-- ============================================================================
-- Datos iniciales para llevar el pollo por presas (se puede ejecutar más de una vez).
-- Crea los insumos Pecho, Ala, Pierna y Contrapierna (en Unidad) y la presentación
-- de compra "Pollo entero": 1 pollo = 2 pechos + 2 alas + 2 piernas + 2 contrapiernas.
-- Ejecutar después de correcciones_proyecto_grado.sql.
-- ============================================================================
USE proyecto_grado;
GO

SET NOCOUNT ON;

DECLARE @IdUnidad INT = (SELECT TOP 1 id_medida FROM MEDIDAS
                         WHERE Descripcion = 'Unidad' OR Abreviatura IN ('Un.', 'und', 'un')
                         ORDER BY id_medida);
IF @IdUnidad IS NULL
BEGIN
    INSERT INTO MEDIDAS (Descripcion, Abreviatura, Estado, FechaRegistro) VALUES ('Unidad', 'Un.', 1, GETDATE());
    SET @IdUnidad = SCOPE_IDENTITY();
END

DECLARE @Presas TABLE (Orden INT, Nombre VARCHAR(100));
INSERT INTO @Presas VALUES (1, 'Pecho'), (2, 'Ala'), (3, 'Pierna'), (4, 'Contrapierna');

INSERT INTO INSUMO (nombre, descripcion, MEDIDAS_id_medida, estado, fecha_registro)
SELECT p.Nombre, 'Presa de pollo crudo', @IdUnidad, 1, GETDATE()
FROM @Presas p
WHERE NOT EXISTS (SELECT 1 FROM INSUMO i WHERE i.nombre = p.Nombre);

DECLARE @IdPecho INT = (SELECT TOP 1 id_insumo FROM INSUMO WHERE nombre = 'Pecho' ORDER BY id_insumo);

IF NOT EXISTS (SELECT 1 FROM PRESENTACION_INSUMO WHERE INSUMO_id_insumo = @IdPecho AND nombre = 'Pollo entero')
    INSERT INTO PRESENTACION_INSUMO (INSUMO_id_insumo, nombre, factor) VALUES (@IdPecho, 'Pollo entero', 2);

DECLARE @IdPresentacion INT = (SELECT id_presentacion FROM PRESENTACION_INSUMO
                               WHERE INSUMO_id_insumo = @IdPecho AND nombre = 'Pollo entero');

INSERT INTO PRESENTACION_COMPONENTE (PRESENTACION_id_presentacion, INSUMO_id_insumo, factor)
SELECT @IdPresentacion, i.id_insumo, 2
FROM @Presas p
CROSS APPLY (SELECT TOP 1 id_insumo FROM INSUMO WHERE nombre = p.Nombre ORDER BY id_insumo) i
WHERE p.Nombre <> 'Pecho'
  AND NOT EXISTS (SELECT 1 FROM PRESENTACION_COMPONENTE pc
                  WHERE pc.PRESENTACION_id_presentacion = @IdPresentacion AND pc.INSUMO_id_insumo = i.id_insumo);

PRINT 'Presas y presentación "Pollo entero" listas.';
