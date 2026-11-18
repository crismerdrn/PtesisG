-- Objetos que usan los procedimientos almacenados pero no estaban en el script de la base proyecto_grado.
USE proyecto_grado;
GO

-- Los procedimientos usan métodos XML; deben crearse con QUOTED_IDENTIFIER ON.
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- El stock de insumos y el kardex guardaban 2 decimales: una porción de 150 gr de arroz por quintal (0.0033 qq)
-- se descontaba como 0.00 y una presa (0.125 pollo) como 0.13. Se amplía a 4 decimales.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.STOCK_INSUMO') AND name = 'stock_actual' AND scale < 4)
    ALTER TABLE dbo.STOCK_INSUMO ALTER COLUMN stock_actual DECIMAL(12,4) NOT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.STOCK_INSUMO') AND name = 'stock_minimo' AND scale < 4)
    ALTER TABLE dbo.STOCK_INSUMO ALTER COLUMN stock_minimo DECIMAL(12,4) NOT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MOVIMIENTOS_STOCK') AND name = 'cantidad' AND scale < 4)
    ALTER TABLE dbo.MOVIMIENTOS_STOCK ALTER COLUMN cantidad DECIMAL(12,4) NOT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MOVIMIENTOS_STOCK') AND name = 'stock_anterior' AND scale < 4)
    ALTER TABLE dbo.MOVIMIENTOS_STOCK ALTER COLUMN stock_anterior DECIMAL(12,4) NOT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MOVIMIENTOS_STOCK') AND name = 'stock_nuevo' AND scale < 4)
    ALTER TABLE dbo.MOVIMIENTOS_STOCK ALTER COLUMN stock_nuevo DECIMAL(12,4) NOT NULL;
GO

-- sp_Venta_Anular inserta en esta tabla; sin ella la anulación de ventas falla.
IF OBJECT_ID('dbo.AUDITORIA_VENTAS', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AUDITORIA_VENTAS (
        id_auditoria INT IDENTITY(1,1) PRIMARY KEY,
        id_venta INT NOT NULL,
        accion VARCHAR(50) NOT NULL,
        fecha DATETIME NOT NULL DEFAULT GETDATE(),
        id_usuario INT NOT NULL,
        motivo VARCHAR(500) NULL,
        total_venta DECIMAL(10,2) NULL,
        CONSTRAINT FK_AUDITORIA_VENTAS_VENTAS FOREIGN KEY (id_venta) REFERENCES dbo.VENTAS (id_venta),
        CONSTRAINT FK_AUDITORIA_VENTAS_USUARIOS FOREIGN KEY (id_usuario) REFERENCES dbo.USUARIOS (id_usuario)
    );
END
GO


-- Permite varias pollerías activas (sucursales). Antes sp_Polleria_Registrar y sp_Polleria_Editar
-- rechazaban activar una segunda pollería.
CREATE OR ALTER PROCEDURE [dbo].[sp_Polleria_Registrar]
    @Nombre VARCHAR(250),
    @Direccion VARCHAR(250),
    @Telefono VARCHAR(100),
    @Estado BIT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Resultado = 0;
    SET @Mensaje = '';

    BEGIN TRY

        -- Validar nombre único
        IF EXISTS (SELECT * FROM POLLERIA WHERE Nombre = @Nombre)
        BEGIN
            SET @Mensaje = 'Ya existe una pollería con ese nombre';
            RETURN;
        END

        INSERT INTO POLLERIA (
            Nombre,
            Direccion,
            Telefono,
            Estado,
            FechaRegistro
        ) VALUES (
            @Nombre,
            @Direccion,
            @Telefono,
            @Estado,
            GETDATE()
        );

        SET @Resultado = 1;
        SET @Mensaje = 'Polleria registrada exitosamente';
    END TRY
    BEGIN CATCH
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Polleria_Editar]
    @IdPolleria INT,
    @Nombre VARCHAR(250),
    @Direccion VARCHAR(250),
    @Telefono VARCHAR(100),
    @Estado BIT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Resultado = 0;
    SET @Mensaje = '';

    BEGIN TRY
        -- Validar existencia
        IF NOT EXISTS (SELECT * FROM POLLERIA WHERE id_polleria = @IdPolleria)
        BEGIN
            SET @Mensaje = 'La pollería no existe';
            RETURN;
        END

        -- Validar nombre único
        IF EXISTS (SELECT * FROM POLLERIA WHERE Nombre = @Nombre AND id_polleria != @IdPolleria)
        BEGIN
            SET @Mensaje = 'Ya existe otra pollería con ese nombre';
            RETURN;
        END


        UPDATE POLLERIA
        SET Nombre = @Nombre,
            Direccion = @Direccion,
            Telefono = @Telefono,
            Estado = @Estado
        WHERE id_polleria = @IdPolleria;

        SET @Resultado = 1;
        SET @Mensaje = 'Polleria actualizada exitosamente';
    END TRY
    BEGIN CATCH
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH
END
GO

-- La anulación revertía solo STOCK de productos; los productos elaborados (1/4 de pollo, económicos,
-- porciones) descuentan insumos, que no se devolvían. Ahora se revierte lo que registró la venta en
-- MOVIMIENTOS_STOCK, sea producto o insumo.
CREATE OR ALTER PROCEDURE [dbo].[sp_Venta_Anular]
    @IdVenta INT,
    @IdUsuario INT,
    @Motivo VARCHAR(500),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Resultado = 0;
    SET @Mensaje = '';

    DECLARE @IdPolleria INT;
    DECLARE @EstadoVenta VARCHAR(50);
    DECLARE @TotalVenta DECIMAL(10,2);
    DECLARE @Obs VARCHAR(500) = 'Anulación venta: ' + ISNULL(NULLIF(@Motivo, ''), 'Sin motivo');

    -- Lo que la venta descontó, por ítem: productos de reventa (STOCK) e insumos de productos elaborados (STOCK_INSUMO)
    DECLARE @Reversion TABLE (id_item INT, tipo_item VARCHAR(20), cantidad DECIMAL(12,4));

    BEGIN TRY
        SELECT
            @IdPolleria = POLLERIA_id_polleria,
            @EstadoVenta = Estado,
            @TotalVenta = Total_venta
        FROM VENTAS
        WHERE id_venta = @IdVenta;

        IF @IdPolleria IS NULL
        BEGIN
            SET @Mensaje = 'Venta no encontrada';
            RETURN;
        END

        IF @EstadoVenta = 'ANULADA'
        BEGIN
            SET @Mensaje = 'La venta ya está anulada';
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM USUARIOS WHERE id_usuario = @IdUsuario AND estado = 1)
        BEGIN
            SET @Mensaje = 'Usuario no válido';
            RETURN;
        END

        BEGIN TRANSACTION;

        -- Marca la venta dentro de la transacción: si otra anulación llegó antes, no se revierte dos veces.
        UPDATE VENTAS
        SET Estado = 'ANULADA'
        WHERE id_venta = @IdVenta AND Estado <> 'ANULADA';

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'La venta ya está anulada';
            RETURN;
        END

        -- Se revierte exactamente lo que registró la venta en el kardex.
        INSERT INTO @Reversion (id_item, tipo_item, cantidad)
        SELECT id_item, tipo_item, SUM(stock_anterior - stock_nuevo)
        FROM MOVIMIENTOS_STOCK
        WHERE tipo_movimiento = 'VENTA'
          AND id_referencia = @IdVenta
          AND id_polleria = @IdPolleria
        GROUP BY id_item, tipo_item;

        -- Ventas antiguas sin movimientos: se revierte el detalle sobre STOCK como antes.
        IF NOT EXISTS (SELECT 1 FROM @Reversion)
            INSERT INTO @Reversion (id_item, tipo_item, cantidad)
            SELECT dv.PRODUCTO_id_producto, 'PRODUCTO', SUM(dv.cantidad)
            FROM DETALLE_VENTAS dv
            WHERE dv.VENTAS_id_venta = @IdVenta
            GROUP BY dv.PRODUCTO_id_producto;

        INSERT INTO MOVIMIENTOS_STOCK (
            fecha_movimiento, tipo_movimiento, id_referencia, tipo_referencia,
            id_item, tipo_item, cantidad, stock_anterior, stock_nuevo,
            id_polleria, id_usuario, observaciones, fecha_registro
        )
        SELECT GETDATE(), 'ANULACION', @IdVenta, 'VENTA_ANULADA',
               r.id_item, 'PRODUCTO', r.cantidad, s.stock_actual, s.stock_actual + r.cantidad,
               @IdPolleria, @IdUsuario, @Obs, GETDATE()
        FROM @Reversion r
        INNER JOIN STOCK s ON s.PRODUCTO_id_producto = r.id_item AND s.POLLERIA_id_polleria = @IdPolleria
        WHERE r.tipo_item = 'PRODUCTO'
        UNION ALL
        SELECT GETDATE(), 'ANULACION', @IdVenta, 'VENTA_ANULADA',
               r.id_item, 'INSUMO', r.cantidad, si.stock_actual, si.stock_actual + r.cantidad,
               @IdPolleria, @IdUsuario, @Obs, GETDATE()
        FROM @Reversion r
        INNER JOIN STOCK_INSUMO si ON si.INSUMO_id_insumo = r.id_item AND si.POLLERIA_id_polleria = @IdPolleria
        WHERE r.tipo_item = 'INSUMO';

        UPDATE s
        SET s.stock_actual = s.stock_actual + r.cantidad,
            s.Fecha_ultimo_movimiento = GETDATE()
        FROM STOCK s
        INNER JOIN @Reversion r ON r.tipo_item = 'PRODUCTO' AND s.PRODUCTO_id_producto = r.id_item
        WHERE s.POLLERIA_id_polleria = @IdPolleria;

        UPDATE si
        SET si.stock_actual = si.stock_actual + r.cantidad,
            si.Fecha_ultimo_movimiento = GETDATE()
        FROM STOCK_INSUMO si
        INNER JOIN @Reversion r ON r.tipo_item = 'INSUMO' AND si.INSUMO_id_insumo = r.id_item
        WHERE si.POLLERIA_id_polleria = @IdPolleria;

        INSERT INTO AUDITORIA_VENTAS (id_venta, accion, fecha, id_usuario, motivo, total_venta)
        VALUES (@IdVenta, 'ANULACION', GETDATE(), @IdUsuario, @Motivo, @TotalVenta);

        COMMIT TRANSACTION;
        SET @Resultado = 1;
        SET @Mensaje = 'Venta anulada exitosamente. Stock revertido.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        SET @Mensaje = 'Error al anular venta: ' + ERROR_MESSAGE();
    END CATCH
END
GO

-- @CantReq era DECIMAL(10,2) y redondeaba la cantidad de la receta (DECIMAL(10,4)): 0.125 kg se descontaba como 0.13 kg.
CREATE OR ALTER PROCEDURE [dbo].[sp_Venta_RegistrarConStock]
    @IdUsuario INT,
    @IdCliente INT = NULL,
    @IdPolleria INT,
    @TipoVenta VARCHAR(50) = 'EFECTIVO',
    @IdTipoVenta INT = 1,
    @Detalles XML,
    @Observaciones VARCHAR(500) = NULL,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT,
    @IdVentaGenerada INT OUTPUT,
    @NumeroVenta VARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Resultado = 0;
    SET @IdVentaGenerada = 0;
    SET @NumeroVenta = '';
    SET @Mensaje = '';

    DECLARE @TotalVenta DECIMAL(10,2) = 0;
    DECLARE @Contador INT;
    DECLARE @IdTipoPago INT;
    DECLARE @ClienteFinal INT;

    DECLARE @DetallesTemp TABLE (
        RowIndex INT IDENTITY(1,1),
        IdProducto INT,
        Cantidad DECIMAL(10,2),
        PrecioUnitario DECIMAL(10,2),
        Subtotal DECIMAL(10,2)
    );

    BEGIN TRY
        -- 1. Validaciones de existencia
        IF NOT EXISTS (SELECT 1 FROM USUARIOS WHERE id_usuario = @IdUsuario AND estado = 1)
        BEGIN
            SET @Mensaje = 'Usuario no válido o inactivo.';
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM POLLERIA WHERE id_polleria = @IdPolleria AND Estado = 1)
        BEGIN
            SET @Mensaje = 'Pollería no válida o inactiva.';
            RETURN;
        END

        -- 2. Resolver Cliente
        IF @IdCliente IS NULL OR @IdCliente = 0
        BEGIN
            SELECT TOP 1 @ClienteFinal = id_cliente
            FROM CLIENTE
            WHERE UPPER(LTRIM(RTRIM(Nombre))) = 'GENERAL' AND UPPER(LTRIM(RTRIM(Apellido))) = 'GENERAL';

            IF @ClienteFinal IS NULL
            BEGIN
                INSERT INTO CLIENTE (Nombre, Apellido, Telefono, Direccion, Email, Estado, Fecha_Registro)
                VALUES ('GENERAL', 'GENERAL', '0000000', 'N/A', 'cliente@general.com', 1, GETDATE());

                SET @ClienteFinal = SCOPE_IDENTITY();
            END
        END
        ELSE
        BEGIN
            SET @ClienteFinal = @IdCliente;
            IF NOT EXISTS (SELECT 1 FROM CLIENTE WHERE id_cliente = @IdCliente AND Estado = 1)
            BEGIN
                SET @Mensaje = 'El cliente seleccionado no existe o está inactivo.';
                RETURN;
            END
        END

        -- 3. Parsear XML
        INSERT INTO @DetallesTemp (IdProducto, Cantidad, PrecioUnitario, Subtotal)
        SELECT
            T.Item.value('@IdProducto', 'INT'),
            T.Item.value('@Cantidad', 'DECIMAL(10,2)'),
            T.Item.value('@PrecioUnitario', 'DECIMAL(10,2)'),
            T.Item.value('@Subtotal', 'DECIMAL(10,2)')
        FROM @Detalles.nodes('//Detalle') AS T(Item)
        UNION ALL
        SELECT
            T.Item.value('(IdProducto)[1]', 'INT'),
            T.Item.value('(Cantidad)[1]', 'DECIMAL(10,2)'),
            T.Item.value('(PrecioUnitario)[1]', 'DECIMAL(10,2)'),
            T.Item.value('(Subtotal)[1]', 'DECIMAL(10,2)')
        FROM @Detalles.nodes('//DetalleVentaStockXML') AS T(Item);

        IF NOT EXISTS (SELECT 1 FROM @DetallesTemp)
        BEGIN
            SET @Mensaje = 'El XML de detalles no contiene productos válidos o el formato es incorrecto.';
            RETURN;
        END

        -- 4. Total Venta
        SELECT @TotalVenta = SUM(Subtotal) FROM @DetallesTemp;

        IF ISNULL(@TotalVenta, 0) <= 0
        BEGIN
            SET @Mensaje = 'El total de la venta debe ser mayor a 0.';
            RETURN;
        END

        -- 5. Resolver Medio de Pago
        IF ISNUMERIC(@TipoVenta) = 1
        BEGIN
            SELECT @IdTipoPago = id_tipoPago FROM TIPO_PAGO WHERE id_tipoPago = CAST(@TipoVenta AS INT) AND Estado = 1;
        END
        ELSE
        BEGIN
            SELECT @IdTipoPago = id_tipoPago FROM TIPO_PAGO WHERE UPPER(nombre) = UPPER(LTRIM(RTRIM(@TipoVenta))) AND Estado = 1;
        END

        IF @IdTipoPago IS NULL
            SET @IdTipoPago = 1;

        -- INICIO CONTROL TRANSACCIONAL
        BEGIN TRANSACTION;

        -- 6. Validar Stock
        DECLARE @StockOk BIT = 1;
        DECLARE @MsgStock VARCHAR(1000) = '';
        DECLARE @XmlOut XML;

        EXEC dbo.sp_Venta_ValidarStock
            @IdPolleria = @IdPolleria,
            @Detalles = @Detalles,
            @StockSuficiente = @StockOk OUTPUT,
            @Mensaje = @MsgStock OUTPUT,
            @DetallesValidados = @XmlOut OUTPUT;

        IF @StockOk = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'Stock insuficiente: ' + @MsgStock;
            RETURN;
        END

        -- 7. Correlativo Seguro
        SELECT @Contador = ISNULL(MAX(TRY_CAST(SUBSTRING(NumeroVenta, 3, 50) AS INT)), 0) + 1
        FROM VENTAS WITH (UPDLOCK, HOLDLOCK)
        WHERE YEAR(Fecha_venta) = YEAR(GETDATE());

        SET @NumeroVenta = 'VT' + RIGHT('000000' + CAST(@Contador AS VARCHAR(6)), 6);

        -- 8. Cabecera Venta
        INSERT INTO VENTAS (
            NumeroVenta, Total_venta, Fecha_venta, hora_venta, Estado, Tipo_venta,
            USUARIOS_id_usuario, CLIENTE_id_cliente, POLLERIA_id_polleria,
            TIPO_VENTA_id_tipoVenta
        ) VALUES (
            @NumeroVenta, @TotalVenta, CAST(GETDATE() AS DATE), CONVERT(TIME, GETDATE()), 'COMPLETADA',
            @TipoVenta, @IdUsuario, @ClienteFinal, @IdPolleria,
            ISNULL(@IdTipoVenta, 1)
        );

        SET @IdVentaGenerada = SCOPE_IDENTITY();

        -- 9. Detalles Venta
        INSERT INTO DETALLE_VENTAS (
            cantidad, precio_unitario, subtotal, VENTAS_id_venta, PRODUCTO_id_producto
        )
        SELECT
            Cantidad, PrecioUnitario, Subtotal, @IdVentaGenerada, IdProducto
        FROM @DetallesTemp;

        -- 10. Descuento de Stock Normalizado y Kardex
        DECLARE @TotalItems INT = (SELECT COUNT(*) FROM @DetallesTemp);
        DECLARE @Idx INT = 1;
        DECLARE @IdP INT, @CantP DECIMAL(10,2);

        WHILE @Idx <= @TotalItems
        BEGIN
            SELECT @IdP = IdProducto, @CantP = Cantidad
            FROM @DetallesTemp WHERE RowIndex = @Idx;

            IF EXISTS (SELECT 1 FROM INSUMO_PRODUCTO WHERE PRODUCTO_id_producto = @IdP AND estado = 1)
            BEGIN
                -- Descontar insumos con factor de conversión
                DECLARE @CursorInsumos CURSOR;
                DECLARE @IdIns INT, @CantReq DECIMAL(10,4), @TotalDescontar DECIMAL(10,4);
                DECLARE @Factor DECIMAL(18,6);
                DECLARE @AbrevIns VARCHAR(20);
                DECLARE @StockAntIns DECIMAL(12,4);

                SET @CursorInsumos = CURSOR LOCAL FAST_FORWARD FOR
                    SELECT
                        ip.INSUMO_id_insumo,
                        ip.cantidad_requerida,
                        CASE
                            WHEN m.FactorConversion = 1.0 AND ip.cantidad_requerida >= 1.0 AND LOWER(m.Abreviatura) IN ('kg', 'lts')
                                THEN 0.001
                            ELSE ISNULL(m.FactorConversion, 1.0)
                        END,
                        ISNULL(m.Abreviatura, '')
                    FROM INSUMO_PRODUCTO ip
                    INNER JOIN INSUMO i ON ip.INSUMO_id_insumo = i.id_insumo
                    INNER JOIN MEDIDAS m ON i.MEDIDAS_id_medida = m.id_medida
                    WHERE ip.PRODUCTO_id_producto = @IdP AND ip.estado = 1;

                OPEN @CursorInsumos;
                FETCH NEXT FROM @CursorInsumos INTO @IdIns, @CantReq, @Factor, @AbrevIns;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    -- Descuento proporcional a la unidad de almacenamiento
                    SET @TotalDescontar = CAST((@CantReq * @Factor * @CantP) AS DECIMAL(10,4));

                    SELECT @StockAntIns = ISNULL(stock_actual, 0)
                    FROM STOCK_INSUMO WITH (UPDLOCK, ROWLOCK)
                    WHERE INSUMO_id_insumo = @IdIns AND POLLERIA_id_polleria = @IdPolleria;

                    -- Si no existía fila en esa pollería, se inicializa
                    IF @StockAntIns IS NULL
                    BEGIN
                        SET @StockAntIns = 0;
                        INSERT INTO STOCK_INSUMO (stock_actual, stock_minimo, INSUMO_id_insumo, POLLERIA_id_polleria, Fecha_ultimo_movimiento)
                        VALUES (0, 0, @IdIns, @IdPolleria, GETDATE());
                    END

                    UPDATE STOCK_INSUMO
                    SET stock_actual = stock_actual - @TotalDescontar,
                        Fecha_ultimo_movimiento = GETDATE()
                    WHERE INSUMO_id_insumo = @IdIns AND POLLERIA_id_polleria = @IdPolleria;

                    INSERT INTO MOVIMIENTOS_STOCK (
                        fecha_movimiento, tipo_movimiento, id_referencia, tipo_referencia,
                        id_item, tipo_item, cantidad, stock_anterior, stock_nuevo,
                        id_polleria, id_usuario, observaciones, fecha_registro
                    ) VALUES (
                        GETDATE(), 'VENTA', @IdVentaGenerada, 'VENTA',
                        @IdIns, 'INSUMO', -@TotalDescontar, @StockAntIns, (@StockAntIns - @TotalDescontar),
                        @IdPolleria, @IdUsuario, 'Consumo venta ' + @NumeroVenta, GETDATE()
                    );

                    FETCH NEXT FROM @CursorInsumos INTO @IdIns, @CantReq, @Factor, @AbrevIns;
                END;

                CLOSE @CursorInsumos;
                DEALLOCATE @CursorInsumos;
            END
            ELSE
            BEGIN
                -- Descontar producto de reventa directa (sodas, postres)
                DECLARE @StockAntProd DECIMAL(10,2);

                SELECT @StockAntProd = ISNULL(stock_actual, 0)
                FROM STOCK WITH (UPDLOCK, ROWLOCK)
                WHERE PRODUCTO_id_producto = @IdP AND POLLERIA_id_polleria = @IdPolleria;

                UPDATE STOCK
                SET stock_actual = stock_actual - @CantP,
                    Fecha_ultimo_movimiento = GETDATE()
                WHERE PRODUCTO_id_producto = @IdP AND POLLERIA_id_polleria = @IdPolleria;

                INSERT INTO MOVIMIENTOS_STOCK (
                    fecha_movimiento, tipo_movimiento, id_referencia, tipo_referencia,
                    id_item, tipo_item, cantidad, stock_anterior, stock_nuevo,
                    id_polleria, id_usuario, observaciones, fecha_registro
                ) VALUES (
                    GETDATE(), 'VENTA', @IdVentaGenerada, 'VENTA',
                    @IdP, 'PRODUCTO', -@CantP, @StockAntProd, (@StockAntProd - @CantP),
                    @IdPolleria, @IdUsuario, ISNULL(@Observaciones, 'Venta ' + @NumeroVenta), GETDATE()
                );
            END;

            SET @Idx = @Idx + 1;
        END;

        -- 11. Pago
        INSERT INTO PAGO_VENTAS (
            monto, fecha_pago, estado, VENTAS_id_venta, TIPO_PAGO_id_tipoPago
        ) VALUES (
            @TotalVenta, GETDATE(), 1, @IdVentaGenerada, @IdTipoPago
        );

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Venta registrada exitosamente. N°: ' + @NumeroVenta;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @Resultado = 0;
        SET @IdVentaGenerada = 0;
        SET @NumeroVenta = '';
        SET @Mensaje = 'Error al registrar venta: ' + ERROR_MESSAGE();
    END CATCH
END;
GO

-- La tabla temporal guardaba cantidad_requerida como DECIMAL(10,2): una receta de 0.125 kg se mostraba como 0.13.
CREATE OR ALTER PROCEDURE [dbo].[sp_InsumoProducto_CalcularCostoProduccion]
    @IdProducto INT,
    @IdPolleria INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PrecioVenta DECIMAL(10,2),
            @NombreProducto NVARCHAR(200),
            @CostoTotal DECIMAL(10,2) = 0,
            @TotalInsumos INT = 0,
            @UnidadesMaximasProducibles INT = NULL;

    -- 1. Obtener producto
    SELECT
        @PrecioVenta = p.PrecioVenta,
        @NombreProducto = p.Nombre
    FROM PRODUCTO p
    WHERE p.id_producto = @IdProducto
        AND p.Estado = 1;

    IF @NombreProducto IS NULL
    BEGIN
        SELECT TOP 0 * FROM (
            SELECT
                0 AS id_insumo_producto, '' AS NombreInsumo, 0.00 AS cantidad_requerida,
                '' AS MedidaInsumo, '' AS AbreviaturaMedida, 0.00 AS UltimoPrecioCompra,
                0.00 AS PrecioPromedio3Meses, 0.00 AS CostoInsumo, 0.00 AS StockActual, 0 AS UnidadesProducibles
        ) AS EmptyResult;

        SELECT
            @IdProducto AS IdProducto, 'Producto no encontrado' AS NombreProducto,
            0.00 AS PrecioVenta, 0.00 AS CostoTotalProduccion, 0.00 AS GananciaUnitaria,
            0.00 AS PorcentajeMargen, 0 AS TotalInsumos, NULL AS UnidadesMaximasProducibles;
        RETURN;
    END

    CREATE TABLE #DetallesCosto (
        id_insumo_producto INT,
        NombreInsumo NVARCHAR(200),
        cantidad_requerida DECIMAL(10,4),
        MedidaInsumo NVARCHAR(50),
        AbreviaturaMedida NVARCHAR(20),
        UltimoPrecioCompra DECIMAL(10,2),
        PrecioPromedio3Meses DECIMAL(10,2),
        CostoInsumo DECIMAL(10,2),
        StockActual DECIMAL(12,4),
        UnidadesProducibles INT
    );

    INSERT INTO #DetallesCosto
    SELECT
        ip.id_insumo_producto,
        i.nombre AS NombreInsumo,
        ip.cantidad_requerida,
        m.Descripcion AS MedidaInsumo,
        m.Abreviatura AS AbreviaturaMedida,

        -- 1. Último precio compra
        ISNULL((
            SELECT TOP 1 dc.precioUnitario
            FROM DETALLE_COMPRA dc
            INNER JOIN COMPRA c ON dc.COMPRA_id_compra = c.id_compra
            WHERE dc.INSUMO_id_insumo = i.id_insumo
                AND c.Estado IN ('COMPLETADA', 'COMPLETADO')
                AND dc.precioUnitario > 0
            ORDER BY c.FechaCompra DESC, c.id_compra DESC
        ), 0) AS UltimoPrecioCompra,

        -- 2. Precio promedio móvil
        ISNULL((
            SELECT AVG(dc.precioUnitario)
            FROM DETALLE_COMPRA dc
            INNER JOIN COMPRA c ON dc.COMPRA_id_compra = c.id_compra
            WHERE dc.INSUMO_id_insumo = i.id_insumo
                AND c.Estado IN ('COMPLETADA', 'COMPLETADO')
                AND dc.precioUnitario > 0
                AND c.FechaCompra >= DATEADD(MONTH, -3, GETDATE())
        ),
        ISNULL((
            SELECT TOP 1 dc.precioUnitario
            FROM DETALLE_COMPRA dc
            INNER JOIN COMPRA c ON dc.COMPRA_id_compra = c.id_compra
            WHERE dc.INSUMO_id_insumo = i.id_insumo
                AND c.Estado IN ('COMPLETADA', 'COMPLETADO')
                AND dc.precioUnitario > 0
            ORDER BY c.FechaCompra DESC
        ), 0)) AS PrecioPromedio3Meses,

        -- 3. Costo del insumo (NORMALIZACIÓN INTELIGENTE)
        CAST(
            (
                ip.cantidad_requerida *
                CASE
                    -- Si la medida explícitamente dice gramos o mililitros:
                    WHEN LOWER(m.Abreviatura) IN ('gr', 'g', 'mls', 'ml')
                        THEN 0.001

                    -- Si la medida dice Kilos o Litros, pero el usuario puso >= 1.0 (ej. 40 ml de aceite, 150 gr de pollo):
                    WHEN LOWER(m.Abreviatura) IN ('kg', 'kilo', 'lts', 'lt', 'litros') AND ip.cantidad_requerida >= 1.0
                        THEN (1.0 / 1000.0)

                    -- Si la medida dice Arroba y puso >= 1.0:
                    WHEN LOWER(m.Abreviatura) IN ('@') AND ip.cantidad_requerida >= 1.0
                        THEN (1.0 / 11500.0)

                    ELSE 1.0
                END
            ) * ISNULL(
                COALESCE(
                    (
                        SELECT AVG(dc.precioUnitario)
                        FROM DETALLE_COMPRA dc
                        INNER JOIN COMPRA c ON dc.COMPRA_id_compra = c.id_compra
                        WHERE dc.INSUMO_id_insumo = i.id_insumo
                            AND c.Estado IN ('COMPLETADA', 'COMPLETADO')
                            AND dc.precioUnitario > 0
                            AND c.FechaCompra >= DATEADD(MONTH, -3, GETDATE())
                    ),
                    (
                        SELECT TOP 1 dc.precioUnitario
                        FROM DETALLE_COMPRA dc
                        INNER JOIN COMPRA c ON dc.COMPRA_id_compra = c.id_compra
                        WHERE dc.INSUMO_id_insumo = i.id_insumo
                            AND c.Estado IN ('COMPLETADA', 'COMPLETADO')
                            AND dc.precioUnitario > 0
                        ORDER BY c.FechaCompra DESC
                    )
                ), 0
            ) AS DECIMAL(10,2)
        ) AS CostoInsumo,

        -- 4. Stock actual
        ISNULL((
            SELECT TOP 1 si.stock_actual
            FROM STOCK_INSUMO si
            WHERE si.INSUMO_id_insumo = i.id_insumo
                AND (@IdPolleria IS NULL OR si.POLLERIA_id_polleria = @IdPolleria)
            ORDER BY
                CASE WHEN @IdPolleria IS NOT NULL AND si.POLLERIA_id_polleria = @IdPolleria THEN 1 ELSE 2 END,
                si.Fecha_ultimo_movimiento DESC
        ), 0) AS StockActual,

        -- 5. Capacidad de platos producibles
        CASE
            WHEN ISNULL((
                SELECT TOP 1 si.stock_actual
                FROM STOCK_INSUMO si
                WHERE si.INSUMO_id_insumo = i.id_insumo
                    AND (@IdPolleria IS NULL OR si.POLLERIA_id_polleria = @IdPolleria)
            ), 0) > 0
            AND ip.cantidad_requerida > 0
            THEN FLOOR(
                ISNULL((
                    SELECT TOP 1 si.stock_actual
                    FROM STOCK_INSUMO si
                    WHERE si.INSUMO_id_insumo = i.id_insumo
                        AND (@IdPolleria IS NULL OR si.POLLERIA_id_polleria = @IdPolleria)
                ), 0) /
                (
                    ip.cantidad_requerida *
                    CASE
                        WHEN LOWER(m.Abreviatura) IN ('gr', 'g', 'mls', 'ml')
                            THEN 0.001
                        WHEN LOWER(m.Abreviatura) IN ('kg', 'kilo', 'lts', 'lt', 'litros') AND ip.cantidad_requerida >= 1.0
                            THEN (1.0 / 1000.0)
                        WHEN LOWER(m.Abreviatura) IN ('@') AND ip.cantidad_requerida >= 1.0
                            THEN (1.0 / 11500.0)
                        ELSE 1.0
                    END
                )
            )
            ELSE 0
        END AS UnidadesProducibles

    FROM INSUMO_PRODUCTO ip
    INNER JOIN INSUMO i ON ip.INSUMO_id_insumo = i.id_insumo
    INNER JOIN MEDIDAS m ON i.MEDIDAS_id_medida = m.id_medida
    WHERE ip.PRODUCTO_id_producto = @IdProducto
        AND ip.estado = 1
        AND i.estado = 1
        AND m.Estado = 1;

    -- Totales
    SELECT
        @CostoTotal = ISNULL(SUM(CostoInsumo), 0),
        @TotalInsumos = ISNULL(COUNT(*), 0),
        @UnidadesMaximasProducibles = ISNULL(MIN(UnidadesProducibles), 0)
    FROM #DetallesCosto;

    IF @TotalInsumos = 0
    BEGIN
        SELECT * FROM #DetallesCosto ORDER BY NombreInsumo;

        SELECT
            @IdProducto AS IdProducto,
            @NombreProducto AS NombreProducto,
            ISNULL(@PrecioVenta, 0) AS PrecioVenta,
            0.00 AS CostoTotalProduccion,
            ISNULL(@PrecioVenta, 0) AS GananciaUnitaria,
            100.00 AS PorcentajeMargen,
            0 AS TotalInsumos,
            0 AS UnidadesMaximasProducibles;
    END
    ELSE
    BEGIN
        SELECT * FROM #DetallesCosto ORDER BY CostoInsumo DESC;

        SELECT
            @IdProducto AS IdProducto,
            @NombreProducto AS NombreProducto,
            ISNULL(@PrecioVenta, 0) AS PrecioVenta,
            @CostoTotal AS CostoTotalProduccion,
            ISNULL(@PrecioVenta - @CostoTotal, 0) AS GananciaUnitaria,
            CASE
                WHEN ISNULL(@PrecioVenta, 0) > 0
                THEN ((@PrecioVenta - @CostoTotal) / @PrecioVenta) * 100
                ELSE 0
            END AS PorcentajeMargen,
            @TotalInsumos AS TotalInsumos,
            @UnidadesMaximasProducibles AS UnidadesMaximasProducibles;
    END

    DROP TABLE #DetallesCosto;
END;
GO

-- Procedimientos de stock de insumos: cantidades con 4 decimales (antes DECIMAL(10,2)).
CREATE OR ALTER PROCEDURE [dbo].[sp_ActualizarStockInsumo]
    @id_insumo INT,
    @id_polleria INT,
    @cantidad DECIMAL(12,4),
    @id_usuario INT,
    @tipo_movimiento VARCHAR(50),
    @id_referencia INT = NULL,
    @observaciones VARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @stock_actual DECIMAL(12,4);
    DECLARE @stock_nuevo DECIMAL(12,4);
    DECLARE @nombre_insumo VARCHAR(250);

    BEGIN TRY
        -- Obtener nombre del insumo
        SELECT @nombre_insumo = nombre
        FROM INSUMO
        WHERE id_insumo = @id_insumo;

        -- Obtener stock actual
        SELECT @stock_actual = ISNULL(stock_actual, 0)
        FROM STOCK_INSUMO
        WHERE INSUMO_id_insumo = @id_insumo
            AND POLLERIA_id_polleria = @id_polleria;

        -- Si no existe registro, crear uno
        IF @stock_actual IS NULL
        BEGIN
            INSERT INTO STOCK_INSUMO (
                stock_actual,
                stock_minimo,
                Fecha_ultimo_movimiento,
                INSUMO_id_insumo,
                POLLERIA_id_polleria
            ) VALUES (
                0,
                0,
                GETDATE(),
                @id_insumo,
                @id_polleria
            );

            SET @stock_actual = 0;
        END

        -- Calcular nuevo stock
        SET @stock_nuevo = @stock_actual + @cantidad;

        -- Actualizar stock
        UPDATE STOCK_INSUMO
        SET stock_actual = @stock_nuevo,
            Fecha_ultimo_movimiento = GETDATE()
        WHERE INSUMO_id_insumo = @id_insumo
            AND POLLERIA_id_polleria = @id_polleria;

        -- Registrar movimiento
        INSERT INTO MOVIMIENTOS_STOCK (
            fecha_movimiento,
            tipo_movimiento,
            id_referencia,
            tipo_referencia,
            id_item,
            tipo_item,
            cantidad,
            stock_anterior,
            stock_nuevo,
            id_polleria,
            id_usuario,
            observaciones,
            fecha_registro
        ) VALUES (
            GETDATE(),
            @tipo_movimiento,
            @id_referencia,
            'COMPRA',
            @id_insumo,
            'INSUMO',
            @cantidad,
            @stock_actual,
            @stock_nuevo,
            @id_polleria,
            @id_usuario,
            ISNULL(@observaciones, 'Compra de insumo: ' + @nombre_insumo),
            GETDATE()
        );

        PRINT 'Stock de insumo actualizado: ' + @nombre_insumo +
              ' - Anterior: ' + CAST(@stock_actual AS VARCHAR) +
              ' - Nuevo: ' + CAST(@stock_nuevo AS VARCHAR);
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;

GO

-- SP 10: Actualizar stock mínimo de insumo
CREATE OR ALTER PROCEDURE [dbo].[sp_ActualizarStockMinimoInsumo]
    @id_insumo INT,
    @id_polleria INT,
    @nuevo_stock_minimo DECIMAL(12,4),
    @id_usuario INT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Resultado = 0;
    SET @Mensaje = '';

    BEGIN TRY
        -- Verificar si existe el registro de stock
        IF NOT EXISTS (
            SELECT 1 FROM STOCK_INSUMO
            WHERE INSUMO_id_insumo = @id_insumo
            AND POLLERIA_id_polleria = @id_polleria
        )
        BEGIN
            -- Crear registro si no existe
            INSERT INTO STOCK_INSUMO (
                stock_actual,
                stock_minimo,
                Fecha_ultimo_movimiento,
                INSUMO_id_insumo,
                POLLERIA_id_polleria
            ) VALUES (
                0,
                @nuevo_stock_minimo,
                GETDATE(),
                @id_insumo,
                @id_polleria
            );
        END
        ELSE
        BEGIN
            -- Actualizar stock mínimo
            UPDATE STOCK_INSUMO
            SET stock_minimo = @nuevo_stock_minimo
            WHERE INSUMO_id_insumo = @id_insumo
            AND POLLERIA_id_polleria = @id_polleria;
        END

        SET @Resultado = 1;
        SET @Mensaje = 'Stock mínimo actualizado correctamente';
    END TRY
    BEGIN CATCH
        SET @Resultado = 0;
        SET @Mensaje = ERROR_MESSAGE();
    END CATCH
END

GO

-- SP 6: Ajustar stock de insumo
CREATE OR ALTER PROCEDURE [dbo].[sp_AjustarStockInsumo]
    @id_insumo INT,
    @id_polleria INT,
    @nuevo_stock DECIMAL(12,4),
    @id_usuario INT,
    @observaciones VARCHAR(500) = NULL,
    @id_movimiento_resultado INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @stock_actual DECIMAL(12,4);
    DECLARE @diferencia DECIMAL(12,4);

    BEGIN TRANSACTION;

    BEGIN TRY
        -- Obtener stock actual
        SELECT @stock_actual = ISNULL(stock_actual, 0)
        FROM STOCK_INSUMO
        WHERE INSUMO_id_insumo = @id_insumo
        AND POLLERIA_id_polleria = @id_polleria;

        -- Si no existe registro, crear uno
        IF @stock_actual IS NULL
        BEGIN
            INSERT INTO STOCK_INSUMO (stock_actual, stock_minimo, Fecha_ultimo_movimiento,
                                     INSUMO_id_insumo, POLLERIA_id_polleria)
            VALUES (0, 0, GETDATE(), @id_insumo, @id_polleria);

            SET @stock_actual = 0;
        END

        -- Calcular diferencia
        SET @diferencia = @nuevo_stock - @stock_actual;

        -- Actualizar stock
        UPDATE STOCK_INSUMO
        SET stock_actual = @nuevo_stock,
            Fecha_ultimo_movimiento = GETDATE()
        WHERE INSUMO_id_insumo = @id_insumo
        AND POLLERIA_id_polleria = @id_polleria;

        -- Registrar movimiento
        INSERT INTO MOVIMIENTOS_STOCK (
            fecha_movimiento, tipo_movimiento, id_referencia, tipo_referencia,
            id_item, tipo_item, cantidad, stock_anterior, stock_nuevo,
            id_polleria, id_usuario, observaciones, fecha_registro
        ) VALUES (
            GETDATE(),
            CASE WHEN @diferencia > 0 THEN 'ENTRADA' ELSE 'SALIDA' END,
            NULL,
            'AJUSTE_MANUAL',
            @id_insumo,
            'INSUMO',
            @diferencia,
            @stock_actual,
            @nuevo_stock,
            @id_polleria,
            @id_usuario,
            ISNULL(@observaciones, 'Ajuste manual de stock'),
            GETDATE()
        );

        SET @id_movimiento_resultado = SCOPE_IDENTITY();

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END

GO

CREATE OR ALTER PROCEDURE [dbo].[sp_MovimientoStock_Registrar]
    @TipoMovimiento VARCHAR(50),
    @IdReferencia INT = NULL,
    @TipoReferencia VARCHAR(50) = NULL,
    @IdItem INT,
    @TipoItem VARCHAR(20), -- 'PRODUCTO' o 'INSUMO'
    @Cantidad DECIMAL(12,4), -- Positiva=entrada, Negativa=salida
    @IdPolleria INT,
    @IdUsuario INT,
    @Observaciones VARCHAR(500) = NULL
AS
BEGIN
    DECLARE @StockAnterior DECIMAL(12,4) = 0;
    DECLARE @StockNuevo DECIMAL(12,4) = 0;

    -- Obtener stock anterior
    IF @TipoItem = 'PRODUCTO'
    BEGIN
        SELECT @StockAnterior = ISNULL(stock_actual, 0)
        FROM STOCK
        WHERE PRODUCTO_id_producto = @IdItem
            AND POLLERIA_id_polleria = @IdPolleria;
    END
    ELSE -- INSUMO
    BEGIN
        SELECT @StockAnterior = ISNULL(stock_actual, 0)
        FROM STOCK_INSUMO
        WHERE INSUMO_id_insumo = @IdItem
            AND POLLERIA_id_polleria = @IdPolleria;
    END

    -- Calcular nuevo stock
    SET @StockNuevo = @StockAnterior + @Cantidad;

    -- Insertar movimiento
    INSERT INTO MOVIMIENTOS_STOCK (
        fecha_movimiento,
        tipo_movimiento,
        id_referencia,
        tipo_referencia,
        id_item,
        tipo_item,
        cantidad,
        stock_anterior,
        stock_nuevo,
        id_polleria,
        id_usuario,
        observaciones,
        fecha_registro
    ) VALUES (
        GETDATE(),
        @TipoMovimiento,
        @IdReferencia,
        @TipoReferencia,
        @IdItem,
        @TipoItem,
        @Cantidad,
        @StockAnterior,
        @StockNuevo,
        @IdPolleria,
        @IdUsuario,
        @Observaciones,
        GETDATE()
    );

    RETURN SCOPE_IDENTITY();
END

GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Compra_Anular]
    @IdCompra INT,
    @IdUsuario INT,
    @Motivo VARCHAR(500),
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Resultado = 0;
    SET @Mensaje = '';

    BEGIN TRY
        DECLARE @EstadoActual VARCHAR(50);
        DECLARE @Referencia VARCHAR(100);

        -- 1. Validar existencia de la compra
        SELECT
            @EstadoActual = Estado,
            @Referencia = Referencia
        FROM COMPRA
        WHERE id_compra = @IdCompra;

        IF @EstadoActual IS NULL
        BEGIN
            SET @Mensaje = 'La compra no existe.';
            RETURN;
        END

        IF @EstadoActual = 'ANULADA'
        BEGIN
            SET @Mensaje = 'La compra ya se encuentra anulada.';
            RETURN;
        END

        -- 2. Obtener la pollería activa asociada
        DECLARE @IdPolleria INT;
        SELECT TOP 1 @IdPolleria = id_polleria FROM POLLERIA WHERE Estado = 1;

        IF @IdPolleria IS NULL
        BEGIN
            SET @Mensaje = 'No se encontró una sucursal/pollería activa configurada.';
            RETURN;
        END

        -- 3. Cargar los items comprados en memoria
        DECLARE @TablaDetalles TABLE (
            RowID INT IDENTITY(1,1),
            IdProducto INT,
            IdInsumo INT,
            Cantidad DECIMAL(12,4)
        );

        INSERT INTO @TablaDetalles (IdProducto, IdInsumo, Cantidad)
        SELECT PRODUCTO_id_producto, INSUMO_id_insumo, cantidad
        FROM DETALLE_COMPRA
        WHERE COMPRA_id_compra = @IdCompra;

        -- 4. VALIDACIÓN PREVENTIVA DE STOCK (Solo si la compra está COMPLETADA)
        IF @EstadoActual = 'COMPLETADA'
        BEGIN
            -- 4.1 Verificar stock insuficiente en Productos (Tabla: STOCK)
            DECLARE @NombreProdSinStock VARCHAR(250);
            DECLARE @StockActualProd DECIMAL(12,4);
            DECLARE @CantRequeridaProd DECIMAL(12,4);

            SELECT TOP 1
                @NombreProdSinStock = p.Nombre,
                @StockActualProd = ISNULL(s.stock_actual, 0),
                @CantRequeridaProd = td.Cantidad
            FROM @TablaDetalles td
            INNER JOIN PRODUCTO p ON td.IdProducto = p.id_producto
            LEFT JOIN STOCK s ON s.PRODUCTO_id_producto = td.IdProducto
                             AND s.POLLERIA_id_polleria = @IdPolleria
            WHERE td.IdProducto IS NOT NULL
              AND ISNULL(s.stock_actual, 0) < td.Cantidad;

            IF @NombreProdSinStock IS NOT NULL
            BEGIN
                SET @Mensaje = 'No se puede anular la compra: El producto "' + @NombreProdSinStock +
                               '" ya fue consumido/vendido (Stock actual: ' +
                               CAST(@StockActualProd AS VARCHAR(20)) + ', Necesario para revertir: ' +
                               CAST(@CantRequeridaProd AS VARCHAR(20)) + ').';
                RETURN;
            END

            -- 4.2 Verificar stock insuficiente en Insumos (Tabla: STOCK_INSUMO)
            DECLARE @NombreInsSinStock VARCHAR(250);
            DECLARE @StockActualIns DECIMAL(12,4);
            DECLARE @CantRequeridaIns DECIMAL(12,4);

            SELECT TOP 1
                @NombreInsSinStock = i.nombre,
                @StockActualIns = ISNULL(si.stock_actual, 0),
                @CantRequeridaIns = td.Cantidad
            FROM @TablaDetalles td
            INNER JOIN INSUMO i ON td.IdInsumo = i.id_insumo
            LEFT JOIN STOCK_INSUMO si ON si.INSUMO_id_insumo = td.IdInsumo
                                      AND si.POLLERIA_id_polleria = @IdPolleria
            WHERE td.IdInsumo IS NOT NULL
              AND ISNULL(si.stock_actual, 0) < td.Cantidad;

            IF @NombreInsSinStock IS NOT NULL
            BEGIN
                SET @Mensaje = 'No se puede anular la compra: El insumo "' + @NombreInsSinStock +
                               '" ya fue consumido/vendido (Stock actual: ' +
                               CAST(@StockActualIns AS VARCHAR(20)) + ', Necesario para revertir: ' +
                               CAST(@CantRequeridaIns AS VARCHAR(20)) + ').';
                RETURN;
            END
        END

        -- 5. Ejecutar la reversión dentro de una transacción
        BEGIN TRANSACTION;

        IF @EstadoActual = 'COMPLETADA'
        BEGIN
            -- Anular los pagos asociados a esta compra
            UPDATE PAGO_COMPRA
            SET estado = 0
            WHERE COMPRA_id_compra = @IdCompra;

            DECLARE @Row INT = 1;
            DECLARE @MaxRow INT = (SELECT MAX(RowID) FROM @TablaDetalles);
            DECLARE @IdP INT, @IdI INT, @Cant DECIMAL(12,4), @CantRevertir DECIMAL(12,4);
            DECLARE @Obs VARCHAR(500) = 'Anulación Compra N° ' + ISNULL(@Referencia, CAST(@IdCompra AS VARCHAR(10))) + ' - Motivo: ' + ISNULL(@Motivo, 'Sin motivo');

            WHILE @Row <= @MaxRow
            BEGIN
                SELECT
                    @IdP = IdProducto,
                    @IdI = IdInsumo,
                    @Cant = Cantidad
                FROM @TablaDetalles
                WHERE RowID = @Row;

                SET @CantRevertir = -1 * @Cant;

                IF @IdP IS NOT NULL
                BEGIN
                    EXEC dbo.sp_ActualizarStockProducto
                        @id_producto = @IdP,
                        @id_polleria = @IdPolleria,
                        @cantidad = @CantRevertir,
                        @id_usuario = @IdUsuario,
                        @tipo_movimiento = 'ANULACION_COMPRA',
                        @id_referencia = @IdCompra,
                        @observaciones = @Obs;
                END
                ELSE IF @IdI IS NOT NULL
                BEGIN
                    EXEC dbo.sp_ActualizarStockInsumo
                        @id_insumo = @IdI,
                        @id_polleria = @IdPolleria,
                        @cantidad = @CantRevertir,
                        @id_usuario = @IdUsuario,
                        @tipo_movimiento = 'ANULACION_COMPRA',
                        @id_referencia = @IdCompra,
                        @observaciones = @Obs;
                END

                SET @Row = @Row + 1;
            END
        END

        -- 6. Actualizar estado de la cabecera
        UPDATE COMPRA
        SET Estado = 'ANULADA'
        WHERE id_compra = @IdCompra;

        SET @Resultado = 1;
        SET @Mensaje = 'Compra anulada correctamente.';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @Resultado = 0;
        SET @Mensaje = 'Error al anular la compra: ' + ERROR_MESSAGE();
    END CATCH
END;

GO

-- =============================================
-- SP PARA GESTIÓN DE INSUMO_PRODUCTO (RECETAS)
-- =============================================

-- SP PARA AGREGAR INSUMO A PRODUCTO
-- sp_InsumoProducto_Agregar recibía la cantidad con 2 decimales y rechazaba recetas como 0.0033 qq.
CREATE OR ALTER PROCEDURE [dbo].[sp_InsumoProducto_Agregar]
    @IdProducto INT,
    @IdInsumo INT,
    @CantidadRequerida DECIMAL(10,4),
    @Observaciones VARCHAR(500) = NULL,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET @Resultado = 0;
    SET @Mensaje = '';

    BEGIN TRY
        -- Validar que el producto exista y esté activo
        IF NOT EXISTS (SELECT * FROM PRODUCTO
                      WHERE id_producto = @IdProducto AND Estado = 1)
        BEGIN
            SET @Mensaje = 'El producto no existe o está inactivo';
            RETURN;
        END

        -- Validar que el insumo exista y esté activo
        IF NOT EXISTS (SELECT * FROM INSUMO
                      WHERE id_insumo = @IdInsumo AND estado = 1)
        BEGIN
            SET @Mensaje = 'El insumo no existe o está inactivo';
            RETURN;
        END

        -- Validar que no exista ya la relación
        IF EXISTS (SELECT * FROM INSUMO_PRODUCTO
                  WHERE PRODUCTO_id_producto = @IdProducto
                  AND INSUMO_id_insumo = @IdInsumo)
        BEGIN
            SET @Mensaje = 'Este insumo ya está asignado al producto';
            RETURN;
        END

        -- Validar cantidad
        IF @CantidadRequerida <= 0
        BEGIN
            SET @Mensaje = 'La cantidad requerida debe ser mayor a 0';
            RETURN;
        END

        BEGIN TRANSACTION;

        -- Insertar relación insumo-producto
        INSERT INTO INSUMO_PRODUCTO (
            PRODUCTO_id_producto,
            INSUMO_id_insumo,
            cantidad_requerida,
            observaciones
        ) VALUES (
            @IdProducto,
            @IdInsumo,
            @CantidadRequerida,
            @Observaciones
        );

        SET @Resultado = 1;
        SET @Mensaje = 'Insumo agregado al producto exitosamente';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @Mensaje = 'Error al agregar insumo al producto: ' + ERROR_MESSAGE();
    END CATCH
END

GO

-- Validación de producción con la misma conversión de medidas que la venta.
CREATE OR ALTER PROCEDURE [dbo].[sp_InsumoProducto_ValidarStockProduccion]
    @IdProducto INT,
    @CantidadProducir INT,
    @IdPolleria INT,
    @Resultado BIT OUTPUT,
    @Mensaje NVARCHAR(1000) OUTPUT,
    @UnidadesPosibles INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Variables locales
    DECLARE @StockSuficiente BIT = 1,
            @MensajeValidacion NVARCHAR(1000) = '',
            @UnidadesMaximasProducibles INT = 999999,
            @InsumoLimitante NVARCHAR(200),
            @CantidadLimitante INT,
            @NombreProducto NVARCHAR(200);

    -- Obtener nombre del producto
    SELECT @NombreProducto = Nombre
    FROM PRODUCTO
    WHERE id_producto = @IdProducto;

    -- Crear tabla temporal para resultados
    CREATE TABLE #ValidacionStock (
        IdInsumo INT,
        NombreInsumo NVARCHAR(200),
        CantidadRequerida DECIMAL(10,4),
        StockActual DECIMAL(12,4),
        CantidadNecesaria DECIMAL(12,4),
        Suficiente BIT,
        UnidadesPosibles INT
    );

    -- Obtener insumos del producto y validar stock
    -- Misma conversión de medidas que sp_Venta_RegistrarConStock al descontar.
    INSERT INTO #ValidacionStock
    SELECT
        i.id_insumo,
        i.nombre AS NombreInsumo,
        ip.cantidad_requerida,
        ISNULL(si.stock_actual, 0) AS StockActual,
        ip.cantidad_requerida * CASE WHEN m.FactorConversion = 1.0 AND ip.cantidad_requerida >= 1.0 AND LOWER(m.Abreviatura) IN ('kg', 'lts') THEN 0.001 ELSE ISNULL(m.FactorConversion, 1.0) END * @CantidadProducir AS CantidadNecesaria,
        CASE
            WHEN ISNULL(si.stock_actual, 0) >= (ip.cantidad_requerida * CASE WHEN m.FactorConversion = 1.0 AND ip.cantidad_requerida >= 1.0 AND LOWER(m.Abreviatura) IN ('kg', 'lts') THEN 0.001 ELSE ISNULL(m.FactorConversion, 1.0) END * @CantidadProducir)
            THEN 1
            ELSE 0
        END AS Suficiente,
        CASE
            WHEN ip.cantidad_requerida > 0
            THEN FLOOR(ISNULL(si.stock_actual, 0) / (ip.cantidad_requerida * CASE WHEN m.FactorConversion = 1.0 AND ip.cantidad_requerida >= 1.0 AND LOWER(m.Abreviatura) IN ('kg', 'lts') THEN 0.001 ELSE ISNULL(m.FactorConversion, 1.0) END))
            ELSE 999999
        END AS UnidadesPosibles
    FROM INSUMO_PRODUCTO ip
    INNER JOIN INSUMO i ON ip.INSUMO_id_insumo = i.id_insumo
    INNER JOIN MEDIDAS m ON i.MEDIDAS_id_medida = m.id_medida
    LEFT JOIN STOCK_INSUMO si ON i.id_insumo = si.INSUMO_id_insumo
        AND si.POLLERIA_id_polleria = @IdPolleria
    WHERE ip.PRODUCTO_id_producto = @IdProducto
        AND ip.estado = 1
        AND i.estado = 1;

    -- Verificar si existen insumos para el producto
    IF NOT EXISTS (SELECT 1 FROM #ValidacionStock)
    BEGIN
        SET @StockSuficiente = 0;
        SET @MensajeValidacion = 'El producto no tiene insumos asignados.';
        SET @UnidadesMaximasProducibles = 0;
        SET @Resultado = @StockSuficiente;
        SET @Mensaje = @MensajeValidacion;
        SET @UnidadesPosibles = @UnidadesMaximasProducibles;

        -- Retornar resultado vacío
        SELECT * FROM #ValidacionStock;

        SELECT
            @StockSuficiente AS StockSuficiente,
            @MensajeValidacion AS MensajeValidacion,
            @UnidadesMaximasProducibles AS UnidadesMaximasProducibles,
            @CantidadProducir AS CantidadSolicitada,
            'Asigne insumos al producto antes de validar stock.' AS Recomendacion;

        DROP TABLE #ValidacionStock;
        RETURN;
    END

    -- Determinar si hay stock suficiente
    IF EXISTS (SELECT 1 FROM #ValidacionStock WHERE Suficiente = 0)
    BEGIN
        SET @StockSuficiente = 0;

        -- Encontrar el insumo más limitante
        SELECT TOP 1
            @InsumoLimitante = NombreInsumo,
            @CantidadLimitante = UnidadesPosibles
        FROM #ValidacionStock
        WHERE UnidadesPosibles IS NOT NULL
        ORDER BY UnidadesPosibles;

        SET @UnidadesMaximasProducibles = ISNULL(@CantidadLimitante, 0);
        SET @MensajeValidacion = 'Stock insuficiente para producir ' + CAST(@CantidadProducir AS NVARCHAR) +
                                ' unidades de "' + @NombreProducto + '". ' +
                                'El insumo más limitante es "' + @InsumoLimitante +
                                '". Máximo posible: ' + CAST(@CantidadLimitante AS NVARCHAR) + ' unidades.';
    END
    ELSE
    BEGIN
        -- Calcular unidades máximas producibles
        SELECT @UnidadesMaximasProducibles = MIN(UnidadesPosibles)
        FROM #ValidacionStock
        WHERE UnidadesPosibles IS NOT NULL;

        IF @CantidadProducir <= @UnidadesMaximasProducibles
        BEGIN
            SET @MensajeValidacion = 'Stock suficiente para producir ' + CAST(@CantidadProducir AS NVARCHAR) +
                                    ' unidades de "' + @NombreProducto + '". ' +
                                    'Se pueden producir hasta ' +
                                    CAST(@UnidadesMaximasProducibles AS NVARCHAR) + ' unidades.';
        END
        ELSE
        BEGIN
            SET @MensajeValidacion = 'Stock suficiente, pero se pueden producir solo ' +
                                    CAST(@UnidadesMaximasProducibles AS NVARCHAR) +
                                    ' unidades de "' + @NombreProducto + '". ' +
                                    'Se solicitó: ' + CAST(@CantidadProducir AS NVarchar);
        END
    END

    -- 1. Retornar detalles de validación
    SELECT * FROM #ValidacionStock
    ORDER BY Suficiente, UnidadesPosibles;

    -- 2. Retornar resumen
    SELECT
        @StockSuficiente AS StockSuficiente,
        @MensajeValidacion AS MensajeValidacion,
        @UnidadesMaximasProducibles AS UnidadesMaximasProducibles,
        @CantidadProducir AS CantidadSolicitada,
        CASE
            WHEN @StockSuficiente = 1 AND @CantidadProducir <= @UnidadesMaximasProducibles
            THEN 'Puede proceder con la producción de ' + @NombreProducto + '.'
            WHEN @StockSuficiente = 1 AND @CantidadProducir > @UnidadesMaximasProducibles
            THEN 'Ajuste la cantidad a ' + CAST(@UnidadesMaximasProducibles AS NVARCHAR) + ' unidades.'
            ELSE 'Necesita reponer stock de insumos antes de producir ' + @NombreProducto + '.'
        END AS Recomendacion;

    -- Establecer valores de salida
    SET @Resultado = @StockSuficiente;
    SET @Mensaje = @MensajeValidacion;
    SET @UnidadesPosibles = @UnidadesMaximasProducibles;

    -- Limpiar tabla temporal
    DROP TABLE #ValidacionStock;
END
GO

-- Antes se validaba cada línea por separado: dos líneas (o dos productos) que usan el mismo insumo
-- pasaban la validación aunque juntas superaran el stock, y el stock quedaba negativo.
-- Ahora se suma lo que consume todo el carrito por insumo (y por producto de reventa).
CREATE OR ALTER PROCEDURE [dbo].[sp_Venta_ValidarStock]
    @IdPolleria INT,
    @Detalles XML,
    @StockSuficiente BIT OUTPUT,
    @Mensaje VARCHAR(1000) OUTPUT,
    @DetallesValidados XML OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @StockSuficiente = 1;
    SET @Mensaje = '';

    DECLARE @DetallesTemp TABLE (
        IdProducto INT,
        Cantidad DECIMAL(10,2)
    );

    -- Producto pedido (sumado si se repite en varias líneas)
    DECLARE @Pedido TABLE (
        IdProducto INT PRIMARY KEY,
        Cantidad DECIMAL(12,2),
        EsElaborado BIT
    );

    -- Consumo total del carrito por insumo, en la unidad de stock del insumo
    DECLARE @Consumo TABLE (
        IdInsumo INT PRIMARY KEY,
        NombreInsumo VARCHAR(250),
        Abreviatura VARCHAR(20),
        Requerido DECIMAL(18,4),
        Disponible DECIMAL(12,4)
    );

    DECLARE @Resultados TABLE (
        IdProducto INT,
        NombreProducto VARCHAR(250),
        CantidadSolicitada DECIMAL(12,2),
        StockActual DECIMAL(12,4),
        StockMinimo DECIMAL(12,4),
        Suficiente BIT,
        Mensaje VARCHAR(500)
    );

    -- Parsear XML (compatible con atributos y elementos)
    INSERT INTO @DetallesTemp (IdProducto, Cantidad)
    SELECT
        T.Item.value('@IdProducto', 'INT'),
        T.Item.value('@Cantidad', 'DECIMAL(10,2)')
    FROM @Detalles.nodes('//Detalle') AS T(Item)
    UNION ALL
    SELECT
        T.Item.value('(IdProducto)[1]', 'INT'),
        T.Item.value('(Cantidad)[1]', 'DECIMAL(10,2)')
    FROM @Detalles.nodes('//DetalleVentaStockXML') AS T(Item);

    INSERT INTO @Pedido (IdProducto, Cantidad, EsElaborado)
    SELECT
        d.IdProducto,
        SUM(d.Cantidad),
        CASE WHEN EXISTS (SELECT 1 FROM INSUMO_PRODUCTO ip WHERE ip.PRODUCTO_id_producto = d.IdProducto AND ip.estado = 1)
             THEN 1 ELSE 0 END
    FROM @DetallesTemp d
    GROUP BY d.IdProducto;

    -- Misma conversión y redondeo por línea que sp_Venta_RegistrarConStock al descontar
    INSERT INTO @Consumo (IdInsumo, NombreInsumo, Abreviatura, Requerido, Disponible)
    SELECT
        i.id_insumo,
        i.nombre,
        ISNULL(m.Abreviatura, ''),
        SUM(CAST(ip.cantidad_requerida *
            CASE
                WHEN m.FactorConversion = 1.0 AND ip.cantidad_requerida >= 1.0 AND LOWER(m.Abreviatura) IN ('kg', 'lts')
                    THEN 0.001
                ELSE ISNULL(m.FactorConversion, 1.0)
            END * d.Cantidad AS DECIMAL(10,4))),
        MAX(ISNULL(si.stock_actual, 0))
    FROM @DetallesTemp d
    INNER JOIN INSUMO_PRODUCTO ip ON ip.PRODUCTO_id_producto = d.IdProducto AND ip.estado = 1
    INNER JOIN INSUMO i ON ip.INSUMO_id_insumo = i.id_insumo
    INNER JOIN MEDIDAS m ON i.MEDIDAS_id_medida = m.id_medida
    LEFT JOIN STOCK_INSUMO si ON si.INSUMO_id_insumo = i.id_insumo AND si.POLLERIA_id_polleria = @IdPolleria
    GROUP BY i.id_insumo, i.nombre, m.Abreviatura;

    INSERT INTO @Resultados (IdProducto, NombreProducto, CantidadSolicitada, StockActual, StockMinimo, Suficiente, Mensaje)
    SELECT
        p.IdProducto,
        pr.Nombre,
        p.Cantidad,
        CASE WHEN p.EsElaborado = 1 THEN ISNULL(f.Disponible, 9999) ELSE ISNULL(s.stock_actual, 0) END,
        CASE WHEN p.EsElaborado = 1 THEN 0 ELSE ISNULL(s.stock_minimo, 0) END,
        CASE
            WHEN p.EsElaborado = 1 THEN CASE WHEN f.IdInsumo IS NULL THEN 1 ELSE 0 END
            ELSE CASE WHEN ISNULL(s.stock_actual, 0) >= p.Cantidad THEN 1 ELSE 0 END
        END,
        CASE
            WHEN p.EsElaborado = 1 AND f.IdInsumo IS NULL THEN 'Insumos suficientes para preparar'
            WHEN p.EsElaborado = 1 THEN
                'Falta insumo: ' + f.NombreInsumo
                + ' (Disp: ' + FORMAT(f.Disponible, '0.####') + ' ' + f.Abreviatura
                + ', Req: ' + FORMAT(f.Requerido, '0.####') + ' ' + f.Abreviatura + ')'
            WHEN ISNULL(s.stock_actual, 0) >= p.Cantidad THEN 'Stock suficiente'
            ELSE 'Stock insuficiente. Disponible: ' + FORMAT(ISNULL(s.stock_actual, 0), '0.##')
        END
    FROM @Pedido p
    LEFT JOIN PRODUCTO pr ON pr.id_producto = p.IdProducto
    LEFT JOIN STOCK s ON s.PRODUCTO_id_producto = p.IdProducto AND s.POLLERIA_id_polleria = @IdPolleria
    OUTER APPLY (
        SELECT TOP 1 c.IdInsumo, c.NombreInsumo, c.Abreviatura, c.Requerido, c.Disponible
        FROM @Consumo c
        INNER JOIN INSUMO_PRODUCTO ip ON ip.INSUMO_id_insumo = c.IdInsumo
            AND ip.PRODUCTO_id_producto = p.IdProducto AND ip.estado = 1
        WHERE c.Disponible < c.Requerido
        ORDER BY c.Disponible - c.Requerido
    ) f;

    IF EXISTS (SELECT 1 FROM @Resultados WHERE Suficiente = 0)
    BEGIN
        SET @StockSuficiente = 0;

        SELECT @Mensaje = LEFT(STRING_AGG(
            CAST(ISNULL(NombreProducto, '') + ': ' + Mensaje AS VARCHAR(MAX)),
            CHAR(10)
        ), 1000)
        FROM @Resultados
        WHERE Suficiente = 0;
    END
    ELSE
    BEGIN
        SET @Mensaje = 'Stock suficiente para todos los productos';
    END

    SET @DetallesValidados = (
        SELECT
            IdProducto AS '@IdProducto',
            NombreProducto AS '@NombreProducto',
            CantidadSolicitada AS '@Cantidad',
            StockActual AS '@StockActual',
            StockMinimo AS '@StockMinimo',
            Suficiente AS '@Suficiente',
            Mensaje AS '@MensajeValidacion'
        FROM @Resultados
        FOR XML PATH('Detalle'), ROOT('DetallesValidados')
    );
END;
GO

-- ============================================================================
-- Presentaciones de compra por insumo (ej. Arroz en kg: Quintal = 46, Arroba = 11.5;
-- Plátano en unidades: Racimo = 50; Pollo en presas: Pollo entero = 8).
-- La compra se registra en la presentación y el stock se guarda en la unidad del insumo.
-- ============================================================================
IF OBJECT_ID('dbo.PRESENTACION_INSUMO', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PRESENTACION_INSUMO (
        id_presentacion INT IDENTITY(1,1) PRIMARY KEY,
        INSUMO_id_insumo INT NOT NULL,
        nombre VARCHAR(100) NOT NULL,
        factor DECIMAL(12,4) NOT NULL,
        estado BIT NOT NULL DEFAULT 1,
        fecha_registro DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_PRESENTACION_INSUMO_INSUMO FOREIGN KEY (INSUMO_id_insumo) REFERENCES dbo.INSUMO(id_insumo),
        CONSTRAINT UQ_PRESENTACION_INSUMO_NOMBRE UNIQUE (INSUMO_id_insumo, nombre),
        CONSTRAINT CK_PRESENTACION_INSUMO_FACTOR CHECK (factor > 0)
    );
END
GO

IF COL_LENGTH('dbo.DETALLE_COMPRA', 'PRESENTACION_id_presentacion') IS NULL
BEGIN
    ALTER TABLE dbo.DETALLE_COMPRA ADD
        PRESENTACION_id_presentacion INT NULL
            CONSTRAINT FK_DETALLE_COMPRA_PRESENTACION REFERENCES dbo.PRESENTACION_INSUMO(id_presentacion),
        cantidad_presentacion DECIMAL(12,4) NULL;
END
GO

-- cantidad queda en la unidad del insumo (2 qq = 92 kg) y precioUnitario por esa unidad (368 Bs/qq = 8 Bs/kg).
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DETALLE_COMPRA') AND name = 'cantidad' AND scale < 4)
    ALTER TABLE dbo.DETALLE_COMPRA ALTER COLUMN cantidad DECIMAL(12,4) NOT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DETALLE_COMPRA') AND name = 'precioUnitario' AND scale < 4)
    ALTER TABLE dbo.DETALLE_COMPRA ALTER COLUMN precioUnitario DECIMAL(12,4) NOT NULL;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Compra_Registrar]
    @IdUsuario INT,
    @IdProveedor INT,
    @IdPolleria INT,
    @IdTipoCompra INT = NULL,
    @IdTipoPago INT = 1, -- Parámetro para seleccionar la forma de pago (1 por defecto si no se especifica)
    @Referencia VARCHAR(100) = NULL,
    @MontoTotal DECIMAL(10,2),
    @Estado VARCHAR(50) = 'COMPLETADA',
    @Detalles XML,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT,
    @IdCompraGenerada INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Resultado = 0;
    SET @IdCompraGenerada = 0;
    SET @Mensaje = '';

    DECLARE @TablaDetalles TABLE (
        RowID INT IDENTITY(1,1),
        IdItem INT,
        Tipo VARCHAR(20),
        IdPresentacion INT NULL,
        CantidadPresentacion DECIMAL(12,4) NULL,
        Cantidad DECIMAL(12,4),
        PrecioUnitario DECIMAL(12,4),
        Subtotal DECIMAL(10,2)
    );

    BEGIN TRY
        -- 1. VALIDACIONES BÁSICAS
        IF NOT EXISTS (SELECT 1 FROM USUARIOS WHERE id_usuario = @IdUsuario AND estado = 1)
        BEGIN
            SET @Mensaje = 'Usuario no válido o inactivo';
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM PROVEEDOR WHERE id_proveedor = @IdProveedor AND Estado = 1)
        BEGIN
            SET @Mensaje = 'Proveedor no válido o inactivo';
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM POLLERIA WHERE id_polleria = @IdPolleria AND Estado = 1)
        BEGIN
            SET @Mensaje = 'Pollería no válida o inactiva';
            RETURN;
        END

        IF @IdTipoPago IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TIPO_PAGO WHERE id_tipoPago = @IdTipoPago AND Estado = 1)
        BEGIN
            SET @Mensaje = 'El tipo de pago seleccionado no es válido o está inactivo';
            RETURN;
        END

        -- 2. PARSEAR DETALLES XML
        INSERT INTO @TablaDetalles (IdItem, Tipo, IdPresentacion, Cantidad, PrecioUnitario, Subtotal)
        SELECT
            T.Item.value('@IdItem', 'INT'),
            T.Item.value('@Tipo', 'VARCHAR(20)'),
            NULLIF(T.Item.value('@IdPresentacion', 'INT'), 0),
            T.Item.value('@Cantidad', 'DECIMAL(12,4)'),
            T.Item.value('@PrecioUnitario', 'DECIMAL(12,4)'),
            T.Item.value('@Subtotal', 'DECIMAL(10,2)')
        FROM @Detalles.nodes('/Detalles/Detalle') AS T(Item);

        IF NOT EXISTS (SELECT 1 FROM @TablaDetalles)
        BEGIN
            SET @Mensaje = 'Debe ingresar al menos un detalle';
            RETURN;
        END

        -- 2.1 PRESENTACIONES DE COMPRA: la cantidad llega en la presentación (ej. 2 quintales)
        --     y se convierte a la unidad del insumo (ej. 92 kg) para el stock y el precio unitario.
        IF EXISTS (
            SELECT 1 FROM @TablaDetalles td
            WHERE td.IdPresentacion IS NOT NULL
              AND (td.Tipo <> 'INSUMO' OR NOT EXISTS (
                    SELECT 1 FROM PRESENTACION_INSUMO pi
                    WHERE pi.id_presentacion = td.IdPresentacion
                      AND pi.INSUMO_id_insumo = td.IdItem
                      AND pi.estado = 1))
        )
        BEGIN
            SET @Mensaje = 'La presentación seleccionada no es válida para el insumo';
            RETURN;
        END

        UPDATE td
        SET CantidadPresentacion = td.Cantidad,
            Cantidad = td.Cantidad * pi.factor,
            PrecioUnitario = CASE WHEN td.Cantidad * pi.factor > 0 THEN td.Subtotal / (td.Cantidad * pi.factor) ELSE 0 END
        FROM @TablaDetalles td
        INNER JOIN PRESENTACION_INSUMO pi ON pi.id_presentacion = td.IdPresentacion;

        -- 3. VALIDAR ITEMS
        DECLARE @IdItem INT, @Tipo VARCHAR(20), @Cantidad DECIMAL(12,4);
        DECLARE @RowCount INT = 1, @MaxRow INT = (SELECT MAX(RowID) FROM @TablaDetalles);

        WHILE @RowCount <= @MaxRow
        BEGIN
            SELECT @IdItem = IdItem, @Tipo = Tipo, @Cantidad = Cantidad
            FROM @TablaDetalles WHERE RowID = @RowCount;

            IF @Tipo = 'PRODUCTO'
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM PRODUCTO WHERE id_producto = @IdItem AND Estado = 1)
                BEGIN
                    SET @Mensaje = 'Producto ID ' + CAST(@IdItem AS VARCHAR) + ' no existe o está inactivo';
                    RETURN;
                END
            END
            ELSE IF @Tipo = 'INSUMO'
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM INSUMO WHERE id_insumo = @IdItem AND estado = 1)
                BEGIN
                    SET @Mensaje = 'Insumo ID ' + CAST(@IdItem AS VARCHAR) + ' no existe o está inactivo';
                    RETURN;
                END
            END
            ELSE
            BEGIN
                SET @Mensaje = 'Tipo de item no válido: ' + @Tipo;
                RETURN;
            END

            IF @Cantidad <= 0
            BEGIN
                SET @Mensaje = 'La cantidad debe ser mayor a 0 para el item ID ' + CAST(@IdItem AS VARCHAR);
                RETURN;
            END

            SET @RowCount = @RowCount + 1;
        END

        -- 4. INICIAR TRANSACCIÓN
        BEGIN TRANSACTION;

        -- 5. INSERTAR CABECERA DE COMPRA
        INSERT INTO COMPRA (
            Referencia,
            MontoTotal,
            FechaCompra,
            FechaRegistro,
            Estado,
            USUARIOS_id_usuario,
            PROVEEDOR_id_proveedor,
            TIPO_COMPRA_id_tipoCompra
        ) VALUES (
            @Referencia,
            @MontoTotal,
            GETDATE(),
            GETDATE(),
            @Estado,
            @IdUsuario,
            @IdProveedor,
            @IdTipoCompra
        );

        SET @IdCompraGenerada = SCOPE_IDENTITY();

        -- 6. INSERTAR DETALLES
        INSERT INTO DETALLE_COMPRA (
            precioUnitario,
            cantidad,
            subtotal,
            COMPRA_id_compra,
            PRODUCTO_id_producto,
            INSUMO_id_insumo,
            PRESENTACION_id_presentacion,
            cantidad_presentacion
        )
        SELECT
            PrecioUnitario,
            Cantidad,
            Subtotal,
            @IdCompraGenerada,
            CASE WHEN Tipo = 'PRODUCTO' THEN IdItem ELSE NULL END,
            CASE WHEN Tipo = 'INSUMO' THEN IdItem ELSE NULL END,
            IdPresentacion,
            CantidadPresentacion
        FROM @TablaDetalles;

        -- 7. ACTUALIZAR STOCK Y REGISTRAR PAGO SI ESTÁ COMPLETADA
        IF @Estado = 'COMPLETADA'
        BEGIN
            DECLARE @ObservacionesMovimiento VARCHAR(500) = 'Compra N° ' +
                CASE
                    WHEN @Referencia IS NOT NULL AND RTRIM(LTRIM(@Referencia)) <> '' THEN @Referencia
                    ELSE CAST(@IdCompraGenerada AS VARCHAR(10))
                END;

            SET @RowCount = 1;

            WHILE @RowCount <= @MaxRow
            BEGIN
                SELECT @IdItem = IdItem, @Tipo = Tipo, @Cantidad = Cantidad
                FROM @TablaDetalles WHERE RowID = @RowCount;

                IF @Tipo = 'PRODUCTO'
                BEGIN
                    EXEC dbo.sp_ActualizarStockProducto
                        @id_producto = @IdItem,
                        @id_polleria = @IdPolleria,
                        @cantidad = @Cantidad,
                        @id_usuario = @IdUsuario,
                        @tipo_movimiento = 'COMPRA',
                        @id_referencia = @IdCompraGenerada,
                        @observaciones = @ObservacionesMovimiento;
                END
                ELSE IF @Tipo = 'INSUMO'
                BEGIN
                    EXEC dbo.sp_ActualizarStockInsumo
                        @id_insumo = @IdItem,
                        @id_polleria = @IdPolleria,
                        @cantidad = @Cantidad,
                        @id_usuario = @IdUsuario,
                        @tipo_movimiento = 'COMPRA',
                        @id_referencia = @IdCompraGenerada,
                        @observaciones = @ObservacionesMovimiento;
                END

                SET @RowCount = @RowCount + 1;
            END

            -- Registrar pago con el tipo de pago enviado por el usuario
            INSERT INTO PAGO_COMPRA (
                monto,
                fecha_pago,
                estado,
                COMPRA_id_compra,
                TIPO_PAGO_id_tipoPago
            ) VALUES (
                @MontoTotal,
                GETDATE(),
                1,
                @IdCompraGenerada,
                ISNULL(@IdTipoPago, 1)
            );
        END

        SET @Resultado = 1;
        SET @Mensaje = 'Compra registrada exitosamente. ID: ' + CAST(@IdCompraGenerada AS VARCHAR);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @Resultado = 0;
        SET @Mensaje = 'Error al registrar compra: ' + ERROR_MESSAGE();
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Compra_Completar]
    @IdCompra INT,
    @IdPolleria INT,
    @IdUsuario INT,
    @Resultado BIT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Resultado = 0;
    SET @Mensaje = '';

    BEGIN TRY
        DECLARE @EstadoActual VARCHAR(50), @MontoTotal DECIMAL(10,2), @Referencia VARCHAR(100);

        SELECT
            @EstadoActual = Estado,
            @MontoTotal = MontoTotal,
            @Referencia = Referencia
        FROM COMPRA
        WHERE id_compra = @IdCompra;

        IF @EstadoActual IS NULL
        BEGIN
            SET @Mensaje = 'La compra no existe';
            RETURN;
        END

        IF @EstadoActual <> 'PENDIENTE'
        BEGIN
            SET @Mensaje = 'Solo se pueden completar compras en estado PENDIENTE. Estado actual: ' + @EstadoActual;
            RETURN;
        END

        BEGIN TRANSACTION;

        UPDATE COMPRA
        SET Estado = 'COMPLETADA'
        WHERE id_compra = @IdCompra;

        INSERT INTO PAGO_COMPRA (monto, fecha_pago, estado, COMPRA_id_compra, TIPO_PAGO_id_tipoPago)
        VALUES (@MontoTotal, GETDATE(), 1, @IdCompra, 1);

        DECLARE @Observaciones VARCHAR(500) = 'Compra Completada N° ' + ISNULL(@Referencia, CAST(@IdCompra AS VARCHAR));

        DECLARE @Detalles TABLE (
            RowID INT IDENTITY(1,1),
            IdProducto INT,
            IdInsumo INT,
            Cantidad DECIMAL(12,4)
        );

        INSERT INTO @Detalles (IdProducto, IdInsumo, Cantidad)
        SELECT PRODUCTO_id_producto, INSUMO_id_insumo, cantidad
        FROM DETALLE_COMPRA
        WHERE COMPRA_id_compra = @IdCompra;

        DECLARE @Row INT = 1, @Max INT = (SELECT MAX(RowID) FROM @Detalles);
        DECLARE @IdP INT, @IdI INT, @Cant DECIMAL(12,4);

        WHILE @Row <= @Max
        BEGIN
            SELECT @IdP = IdProducto, @IdI = IdInsumo, @Cant = Cantidad FROM @Detalles WHERE RowID = @Row;

            IF @IdP IS NOT NULL
            BEGIN
                EXEC dbo.sp_ActualizarStockProducto @id_producto = @IdP, @id_polleria = @IdPolleria, @cantidad = @Cant, @id_usuario = @IdUsuario, @tipo_movimiento = 'COMPRA', @id_referencia = @IdCompra, @observaciones = @Observaciones;
            END
            ELSE IF @IdI IS NOT NULL
            BEGIN
                EXEC dbo.sp_ActualizarStockInsumo @id_insumo = @IdI, @id_polleria = @IdPolleria, @cantidad = @Cant, @id_usuario = @IdUsuario, @tipo_movimiento = 'COMPRA', @id_referencia = @IdCompra, @observaciones = @Observaciones;
            END

            SET @Row = @Row + 1;
        END

        COMMIT TRANSACTION;

        SET @Resultado = 1;
        SET @Mensaje = 'Compra completada y stock actualizado exitosamente';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        SET @Resultado = 0;
        SET @Mensaje = 'Error al completar compra: ' + ERROR_MESSAGE();
    END CATCH
END;
GO
