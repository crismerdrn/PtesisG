-- ============================================================================
-- Migración ÚNICA de los datos de prueba a unidades chicas (gr, ml, Un.).
-- Ejecutar DESPUÉS de correcciones_proyecto_grado.sql y datos_iniciales_presas.sql.
-- Se ejecuta una sola vez: queda registrada en MIGRACION_DATOS.
--
-- Qué hace:
--  1. Insumos en kg o @ pasan a gramos, y los de litros a mililitros: stock,
--     stock mínimo, kardex, compras (cantidad y precio unitario) y presentaciones.
--     Arroz: la unidad ya decía "gr", pero su stock estaba en kg; también se
--     multiplica por 1000.
--  2. Recetas de esos insumos: un valor de 1 o más ya era gr/ml y se deja igual;
--     un valor menor a 1 estaba en kg, lts o @ y se convierte.
--  3. Desactiva "Pollo Entero Crudo" (ahora el pollo se lleva por presas) y el
--     insumo duplicado "papas" si no tiene stock.
--  4. Crea el insumo Plátano (Un.) y presentaciones de compra estándar.
--  5. Deja recetas de ejemplo para 1/4 Pecho, 1/4 Pierna y los económicos.
-- ============================================================================
USE proyecto_grado;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID('dbo.MIGRACION_DATOS', 'U') IS NULL
    CREATE TABLE dbo.MIGRACION_DATOS (
        clave VARCHAR(100) PRIMARY KEY,
        fecha DATETIME NOT NULL DEFAULT GETDATE()
    );

IF EXISTS (SELECT 1 FROM dbo.MIGRACION_DATOS WHERE clave = 'unidades_chicas')
BEGIN
    PRINT 'La migración a unidades chicas ya se ejecutó antes. No se hizo ningún cambio.';
    RETURN;
END

IF OBJECT_ID('dbo.PRESENTACION_COMPONENTE', 'U') IS NULL
   OR COL_LENGTH('dbo.INSUMO_PRODUCTO', 'tipo_consumo') IS NULL
BEGIN
    PRINT 'Primero ejecute correcciones_proyecto_grado.sql.';
    RETURN;
END

BEGIN TRANSACTION;

-- Medidas destino
DECLARE @IdGr INT = (SELECT TOP 1 id_medida FROM MEDIDAS WHERE LOWER(Abreviatura) IN ('gr', 'g') ORDER BY id_medida);
IF @IdGr IS NULL
BEGIN
    INSERT INTO MEDIDAS (Descripcion, Abreviatura, Estado, FechaRegistro, FactorConversion) VALUES ('Gramos', 'gr', 1, GETDATE(), 1);
    SET @IdGr = SCOPE_IDENTITY();
END
DECLARE @IdMl INT = (SELECT TOP 1 id_medida FROM MEDIDAS WHERE LOWER(Abreviatura) IN ('mls', 'ml') ORDER BY id_medida);
IF @IdMl IS NULL
BEGIN
    INSERT INTO MEDIDAS (Descripcion, Abreviatura, Estado, FechaRegistro, FactorConversion) VALUES ('Mililitros', 'mls', 1, GETDATE(), 1);
    SET @IdMl = SCOPE_IDENTITY();
END
DECLARE @IdUn INT = (SELECT TOP 1 id_medida FROM MEDIDAS
                     WHERE Descripcion = 'Unidad' OR LOWER(Abreviatura) IN ('un.', 'und', 'un') ORDER BY id_medida);

-- 1. Insumos a convertir: factor y medida destino
DECLARE @Conv TABLE (IdInsumo INT PRIMARY KEY, Factor DECIMAL(18,6), IdMedidaNueva INT, Tipo VARCHAR(10));

INSERT INTO @Conv (IdInsumo, Factor, IdMedidaNueva, Tipo)
SELECT i.id_insumo,
       CASE
           WHEN LOWER(m.Abreviatura) IN ('kg', 'kilo', 'kgs') THEN 1000
           WHEN LOWER(m.Abreviatura) = '@' THEN 11500
           WHEN LOWER(m.Abreviatura) IN ('lts', 'lt', 'l', 'litros') THEN 1000
           ELSE 1000 -- arroz: dice gr pero el stock está en kg
       END,
       CASE WHEN LOWER(m.Abreviatura) IN ('lts', 'lt', 'l', 'litros') THEN @IdMl ELSE @IdGr END,
       CASE WHEN LOWER(m.Abreviatura) IN ('lts', 'lt', 'l', 'litros') THEN 'ml' ELSE 'gr' END
FROM INSUMO i
INNER JOIN MEDIDAS m ON m.id_medida = i.MEDIDAS_id_medida
WHERE i.estado = 1
  AND LOWER(i.nombre) NOT LIKE 'pollo%entero%'
  AND (LOWER(m.Abreviatura) IN ('kg', 'kilo', 'kgs', '@', 'lts', 'lt', 'l', 'litros')
       OR (LOWER(LTRIM(RTRIM(i.nombre))) = 'arroz' AND LOWER(m.Abreviatura) IN ('gr', 'g')));

UPDATE si SET stock_actual = si.stock_actual * c.Factor, stock_minimo = si.stock_minimo * c.Factor
FROM STOCK_INSUMO si INNER JOIN @Conv c ON c.IdInsumo = si.INSUMO_id_insumo;

UPDATE ms SET cantidad = ms.cantidad * c.Factor,
              stock_anterior = ms.stock_anterior * c.Factor,
              stock_nuevo = ms.stock_nuevo * c.Factor
FROM MOVIMIENTOS_STOCK ms INNER JOIN @Conv c ON c.IdInsumo = ms.id_item
WHERE ms.tipo_item = 'INSUMO';

UPDATE dc SET cantidad = dc.cantidad * c.Factor, precioUnitario = dc.precioUnitario / c.Factor
FROM DETALLE_COMPRA dc INNER JOIN @Conv c ON c.IdInsumo = dc.INSUMO_id_insumo;

UPDATE p SET factor = p.factor * c.Factor
FROM PRESENTACION_INSUMO p INNER JOIN @Conv c ON c.IdInsumo = p.INSUMO_id_insumo;

UPDATE ip SET cantidad_requerida = ip.cantidad_requerida * c.Factor, fecha_actualizacion = GETDATE()
FROM INSUMO_PRODUCTO ip INNER JOIN @Conv c ON c.IdInsumo = ip.INSUMO_id_insumo
WHERE ip.cantidad_requerida < 1;

UPDATE i SET MEDIDAS_id_medida = c.IdMedidaNueva
FROM INSUMO i INNER JOIN @Conv c ON c.IdInsumo = i.id_insumo;

-- 3. Pollo entero en kg y papa duplicada sin stock
DECLARE @Desactivar TABLE (IdInsumo INT PRIMARY KEY);
INSERT INTO @Desactivar (IdInsumo)
SELECT id_insumo FROM INSUMO WHERE estado = 1 AND LOWER(nombre) LIKE 'pollo%entero%'
UNION
SELECT i.id_insumo FROM INSUMO i
WHERE i.estado = 1 AND LOWER(LTRIM(RTRIM(i.nombre))) = 'papas'
  AND EXISTS (SELECT 1 FROM INSUMO p WHERE p.estado = 1 AND LOWER(LTRIM(RTRIM(p.nombre))) = 'papa')
  AND NOT EXISTS (SELECT 1 FROM STOCK_INSUMO s WHERE s.INSUMO_id_insumo = i.id_insumo AND s.stock_actual <> 0);

UPDATE ip SET estado = 0, fecha_actualizacion = GETDATE()
FROM INSUMO_PRODUCTO ip INNER JOIN @Desactivar d ON d.IdInsumo = ip.INSUMO_id_insumo;
UPDATE i SET estado = 0 FROM INSUMO i INNER JOIN @Desactivar d ON d.IdInsumo = i.id_insumo;

-- 4. Plátano y presentaciones estándar
IF NOT EXISTS (SELECT 1 FROM INSUMO WHERE estado = 1 AND (LOWER(nombre) LIKE 'platano%' OR LOWER(nombre) LIKE 'plátano%'))
    INSERT INTO INSUMO (nombre, descripcion, MEDIDAS_id_medida, estado, fecha_registro)
    VALUES ('Plátano', 'Guarnición, se compra por racimo', @IdUn, 1, GETDATE());

DECLARE @Arroz INT = (SELECT TOP 1 id_insumo FROM INSUMO WHERE estado = 1 AND LOWER(LTRIM(RTRIM(nombre))) = 'arroz' ORDER BY id_insumo);
DECLARE @Papa INT = (SELECT TOP 1 id_insumo FROM INSUMO WHERE estado = 1 AND LOWER(LTRIM(RTRIM(nombre))) = 'papa' ORDER BY id_insumo);
DECLARE @Aceite INT = (SELECT TOP 1 id_insumo FROM INSUMO WHERE estado = 1 AND LOWER(nombre) LIKE 'aceite%' ORDER BY id_insumo);
DECLARE @Platano INT = (SELECT TOP 1 id_insumo FROM INSUMO WHERE estado = 1 AND (LOWER(nombre) LIKE 'platano%' OR LOWER(nombre) LIKE 'plátano%') ORDER BY id_insumo);
DECLARE @Pecho INT = (SELECT TOP 1 id_insumo FROM INSUMO WHERE estado = 1 AND nombre = 'Pecho' ORDER BY id_insumo);
DECLARE @Ala INT = (SELECT TOP 1 id_insumo FROM INSUMO WHERE estado = 1 AND nombre = 'Ala' ORDER BY id_insumo);
DECLARE @Pierna INT = (SELECT TOP 1 id_insumo FROM INSUMO WHERE estado = 1 AND nombre = 'Pierna' ORDER BY id_insumo);
DECLARE @Contra INT = (SELECT TOP 1 id_insumo FROM INSUMO WHERE estado = 1 AND nombre = 'Contrapierna' ORDER BY id_insumo);

DECLARE @Pres TABLE (IdInsumo INT, Nombre VARCHAR(100), Factor DECIMAL(12,4));
INSERT INTO @Pres (IdInsumo, Nombre, Factor)
SELECT c.IdInsumo, v.Nombre, v.Factor
FROM @Conv c
CROSS APPLY (VALUES ('Kilo', 1000), ('Arroba', 11500), ('Quintal', 46000)) v(Nombre, Factor)
WHERE c.Tipo = 'gr'
UNION ALL
SELECT c.IdInsumo, 'Litro', 1000 FROM @Conv c WHERE c.Tipo = 'ml'
UNION ALL
SELECT @Platano, v.Nombre, v.Factor FROM (VALUES ('Racimo chico', 50), ('Racimo grande', 100)) v(Nombre, Factor)
WHERE @Platano IS NOT NULL;

INSERT INTO PRESENTACION_INSUMO (INSUMO_id_insumo, nombre, factor)
SELECT p.IdInsumo, p.Nombre, p.Factor
FROM @Pres p
WHERE NOT EXISTS (SELECT 1 FROM PRESENTACION_INSUMO x WHERE x.INSUMO_id_insumo = p.IdInsumo AND x.nombre = p.Nombre);

-- 5. Recetas de ejemplo (si existen los productos y los insumos)
DECLARE @Receta TABLE (IdProducto INT, IdInsumo INT, Cantidad DECIMAL(10,4), Tipo VARCHAR(20));

-- 1/4 Pecho: pecho + ala; 1/4 Pierna: pierna + contrapierna. Aceite 50 ml por presa.
INSERT INTO @Receta (IdProducto, IdInsumo, Cantidad, Tipo)
SELECT p.id_producto, r.IdInsumo, r.Cantidad, r.Tipo
FROM PRODUCTO p
CROSS APPLY (VALUES
    (CASE WHEN LOWER(p.Nombre) LIKE '%pecho%' THEN @Pecho ELSE @Pierna END, 1, 'FIJO'),
    (CASE WHEN LOWER(p.Nombre) LIKE '%pecho%' THEN @Ala ELSE @Contra END, 1, 'FIJO'),
    (@Aceite, 100, 'FIJO'), (@Arroz, 150, 'GUARNICION'), (@Papa, 150, 'GUARNICION'), (@Platano, 0.5, 'GUARNICION')
) r(IdInsumo, Cantidad, Tipo)
WHERE p.Estado = 1 AND p.Nombre LIKE '1/4%' AND (LOWER(p.Nombre) LIKE '%pecho%' OR LOWER(p.Nombre) LIKE '%pierna%');

-- Económico / presa suelta: la presa del nombre o, si no la dice, presa a elegir.
INSERT INTO @Receta (IdProducto, IdInsumo, Cantidad, Tipo)
SELECT p.id_producto, r.IdInsumo, r.Cantidad, r.Tipo
FROM PRODUCTO p
CROSS APPLY (SELECT CASE
        WHEN LOWER(p.Nombre) LIKE '%contra%' THEN @Contra
        WHEN LOWER(p.Nombre) LIKE '%pecho%' THEN @Pecho
        WHEN LOWER(p.Nombre) LIKE '%pierna%' THEN @Pierna
        WHEN LOWER(p.Nombre) LIKE '%ala%' THEN @Ala END AS PresaNombre) n
CROSS APPLY (
    SELECT n.PresaNombre, 1.0, 'FIJO' WHERE n.PresaNombre IS NOT NULL
    UNION ALL
    SELECT v.IdInsumo, 1.0, 'PRESA' FROM (VALUES (@Pecho), (@Ala), (@Pierna), (@Contra)) v(IdInsumo) WHERE n.PresaNombre IS NULL
    UNION ALL
    SELECT v.IdInsumo, v.Cantidad, v.Tipo FROM (VALUES (@Aceite, 50.0, 'FIJO'), (@Arroz, 100.0, 'GUARNICION'),
                                                       (@Papa, 100.0, 'GUARNICION'), (@Platano, 0.5, 'GUARNICION')) v(IdInsumo, Cantidad, Tipo)
) r(IdInsumo, Cantidad, Tipo)
WHERE p.Estado = 1 AND (LOWER(p.Nombre) LIKE 'econ%' OR LOWER(p.Nombre) LIKE 'presa suelta%');

DELETE FROM @Receta WHERE IdInsumo IS NULL;

UPDATE ip SET estado = 0, fecha_actualizacion = GETDATE()
FROM INSUMO_PRODUCTO ip
WHERE ip.PRODUCTO_id_producto IN (SELECT IdProducto FROM @Receta)
  AND NOT EXISTS (SELECT 1 FROM @Receta r WHERE r.IdProducto = ip.PRODUCTO_id_producto AND r.IdInsumo = ip.INSUMO_id_insumo);

UPDATE ip SET cantidad_requerida = r.Cantidad, tipo_consumo = r.Tipo, estado = 1, fecha_actualizacion = GETDATE()
FROM INSUMO_PRODUCTO ip
INNER JOIN @Receta r ON r.IdProducto = ip.PRODUCTO_id_producto AND r.IdInsumo = ip.INSUMO_id_insumo;

INSERT INTO INSUMO_PRODUCTO (PRODUCTO_id_producto, INSUMO_id_insumo, cantidad_requerida, observaciones, tipo_consumo, estado, fecha_registro)
SELECT r.IdProducto, r.IdInsumo, r.Cantidad, NULL, r.Tipo, 1, GETDATE()
FROM @Receta r
WHERE NOT EXISTS (SELECT 1 FROM INSUMO_PRODUCTO ip WHERE ip.PRODUCTO_id_producto = r.IdProducto AND ip.INSUMO_id_insumo = r.IdInsumo);

INSERT INTO MIGRACION_DATOS (clave) VALUES ('unidades_chicas');
COMMIT TRANSACTION;

-- Resumen
SELECT i.nombre AS Insumo, m.Abreviatura AS Unidad, c.Factor AS Multiplicado_por
FROM @Conv c INNER JOIN INSUMO i ON i.id_insumo = c.IdInsumo INNER JOIN MEDIDAS m ON m.id_medida = i.MEDIDAS_id_medida;
SELECT p.Nombre AS Producto, i.nombre AS Insumo, r.Cantidad, r.Tipo
FROM @Receta r INNER JOIN PRODUCTO p ON p.id_producto = r.IdProducto INNER JOIN INSUMO i ON i.id_insumo = r.IdInsumo
ORDER BY p.Nombre, r.Tipo, i.nombre;
PRINT 'Migración a unidades chicas completada.';
