-- Objetos que usan los procedimientos almacenados pero no estaban en el script de la base proyecto_grado.
USE proyecto_grado;
GO

-- Los procedimientos usan métodos XML; deben crearse con QUOTED_IDENTIFIER ON.
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
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
    DECLARE @Reversion TABLE (id_item INT, tipo_item VARCHAR(20), cantidad DECIMAL(10,2));

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

        UPDATE VENTAS
        SET Estado = 'ANULADA'
        WHERE id_venta = @IdVenta;

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
                DECLARE @StockAntIns DECIMAL(10,2);

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
