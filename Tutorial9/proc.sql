CREATE PROCEDURE AddProductToWarehouse
    @IdProduct INT, 
    @IdWarehouse INT, 
    @Amount INT,  
    @CreatedAt DATETIME
AS
BEGIN  
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Amount <= 0
BEGIN
        RAISERROR('Amount must be greater than 0', 16, 1);
        RETURN;
END

    DECLARE @IdProductFromDb INT, 
            @IdOrder INT, 
            @Price DECIMAL(10,2);

SELECT @IdOrder = o.IdOrder
FROM [Order] o
    LEFT JOIN Product_Warehouse pw ON o.IdOrder = pw.IdOrder
WHERE o.IdProduct = @IdProduct
  AND o.Amount = @Amount
  AND pw.IdProductWarehouse IS NULL
  AND o.CreatedAt < @CreatedAt;

SELECT @IdProductFromDb = IdProduct,
       @Price = Price
FROM Product
WHERE IdProduct = @IdProduct;

IF @IdProductFromDb IS NULL
BEGIN
        RAISERROR('Invalid parameter: Provided IdProduct does not exist', 16, 1);
        RETURN;
END

    IF @IdOrder IS NULL
BEGIN
        RAISERROR('Invalid parameter: There is no matching order to fulfill', 16, 1);
        RETURN;
END

    IF NOT EXISTS(SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse)
BEGIN
        RAISERROR('Invalid parameter: Provided IdWarehouse does not exist', 16, 1);
        RETURN;
END

BEGIN TRANSACTION;

UPDATE [Order]
SET FulfilledAt = @CreatedAt
WHERE IdOrder = @IdOrder;

INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Amount * @Price, @CreatedAt);

SELECT SCOPE_IDENTITY() AS NewId;

COMMIT TRANSACTION;
END
