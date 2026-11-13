-- Objetos que usan los procedimientos almacenados pero no estaban en el script de la base proyecto_grado.
USE proyecto_grado;
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
