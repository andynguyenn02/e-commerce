-- Checkout test seed. Safe to re-run: upserts users/products and resets their wallets, carts and orders.
-- Password for every seeded user: 12345678
--
-- Scenarios:
--   checkout_customer  -> cart: Keyboard x2 (200), balance 1000     => 200 OK, remaining 800
--   checkout_poor      -> cart: Keyboard x1 (100), balance 50       => 409 insufficient balance
--   checkout_lowstock  -> cart: Monitor x2, stock 1, balance 1000   => 409 not enough stock
--   checkout_empty     -> empty cart, balance 1000                  => 409 empty cart
--   checkout_admin     -> Admin role                                => 403

USE EcommerceDb;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @now DATETIME2 = SYSUTCDATETIME();
DECLARE @hash NVARCHAR(MAX) = N'$2a$11$FbPXmrspRs/7Die06vDEp.uVQi9kqc3EyI2rvzo14BP/1a8n1FCNa';

-- Role: 0 = Admin, 1 = Customer
DECLARE @SeedUsers TABLE (UserName NVARCHAR(450), Role INT, DefaultId UNIQUEIDENTIFIER, Balance DECIMAL(18,2));
INSERT INTO @SeedUsers VALUES
    (N'checkout_customer', 1, 'C0000000-0000-0000-0000-000000000001', 1000),
    (N'checkout_poor',     1, 'C0000000-0000-0000-0000-000000000002', 50),
    (N'checkout_lowstock', 1, 'C0000000-0000-0000-0000-000000000003', 1000),
    (N'checkout_empty',    1, 'C0000000-0000-0000-0000-000000000004', 1000),
    (N'checkout_admin',    0, 'C0000000-0000-0000-0000-000000000005', 1000);

-- 1. Users (upsert by UserName)
MERGE Users AS t
USING @SeedUsers AS s ON t.UserName = s.UserName
WHEN MATCHED THEN
    UPDATE SET PasswordHash = @hash, Role = s.Role, UpdatedAt = @now
WHEN NOT MATCHED THEN
    INSERT (Id, UserName, PasswordHash, Role, CreatedAt, UpdatedAt)
    VALUES (s.DefaultId, s.UserName, @hash, s.Role, @now, @now);

-- Resolve real ids (an existing user keeps its original id)
DECLARE @Ids TABLE (UserName NVARCHAR(450), UserId UNIQUEIDENTIFIER, Balance DECIMAL(18,2));
INSERT INTO @Ids
SELECT u.UserName, u.Id, s.Balance FROM Users u JOIN @SeedUsers s ON s.UserName = u.UserName;

-- 2. Clear previous checkout results for seed users
DELETE wt FROM WalletTransactions wt
    JOIN Wallets w ON w.Id = wt.WalletId
    JOIN @Ids i ON i.UserId = w.UserId;
DELETE oi FROM OrderItems oi
    JOIN Orders o ON o.Id = oi.OrderId
    JOIN @Ids i ON i.UserId = o.UserId;
DELETE o FROM Orders o JOIN @Ids i ON i.UserId = o.UserId;

-- 3. Wallets (upsert, reset balance)
MERGE Wallets AS t
USING @Ids AS s ON t.UserId = s.UserId
WHEN MATCHED THEN
    UPDATE SET Balance = s.Balance, UpdatedAt = @now
WHEN NOT MATCHED THEN
    INSERT (Id, UserId, Balance, CreatedAt, UpdatedAt)
    VALUES (NEWID(), s.UserId, s.Balance, @now, @now);

-- 4. Category + products (upsert by Id, reset price/stock)
DECLARE @CategoryId UNIQUEIDENTIFIER = 'CA000000-0000-0000-0000-000000000001';
DECLARE @Keyboard   UNIQUEIDENTIFIER = 'D0000000-0000-0000-0000-000000000001';
DECLARE @Monitor    UNIQUEIDENTIFIER = 'D0000000-0000-0000-0000-000000000002';

MERGE Categories AS t
USING (SELECT @CategoryId AS Id, N'Checkout Test' AS Name) AS s ON t.Id = s.Id
WHEN MATCHED THEN UPDATE SET Name = s.Name, UpdatedAt = @now
WHEN NOT MATCHED THEN INSERT (Id, Name, CreatedAt, UpdatedAt) VALUES (s.Id, s.Name, @now, @now);

MERGE Products AS t
USING (VALUES
    (@Keyboard, N'Test Keyboard', N'CHK-KEYBOARD', 100.00, 10),
    (@Monitor,  N'Test Monitor',  N'CHK-MONITOR',  250.00, 1)
) AS s (Id, Name, Code, Price, AvailableQuantity) ON t.Id = s.Id
WHEN MATCHED THEN
    UPDATE SET Name = s.Name, Code = s.Code, Price = s.Price,
               AvailableQuantity = s.AvailableQuantity, IsDeleted = 0, CategoryId = @CategoryId, UpdatedAt = @now
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Code, Price, AvailableQuantity, IsDeleted, CategoryId, CreatedAt, UpdatedAt)
    VALUES (s.Id, s.Name, s.Code, s.Price, s.AvailableQuantity, 0, @CategoryId, @now, @now);

-- 5. Carts (upsert by UserId) + reset items
MERGE Carts AS t
USING @Ids AS s ON t.UserId = s.UserId
WHEN NOT MATCHED THEN
    INSERT (Id, UserId, CreatedAt, UpdatedAt) VALUES (NEWID(), s.UserId, @now, @now);

DELETE ci FROM CartItems ci
    JOIN Carts c ON c.Id = ci.CartId
    JOIN @Ids i ON i.UserId = c.UserId;

INSERT INTO CartItems (Id, CartId, ProductId, Quantity, CreatedAt, UpdatedAt)
SELECT NEWID(), c.Id, s.ProductId, s.Quantity, @now, @now
FROM (VALUES
    (N'checkout_customer', @Keyboard, 2),
    (N'checkout_poor',     @Keyboard, 1),
    (N'checkout_lowstock', @Monitor,  2)
) AS s (UserName, ProductId, Quantity)
JOIN @Ids i ON i.UserName = s.UserName
JOIN Carts c ON c.UserId = i.UserId;

COMMIT TRANSACTION;

-- Summary
SELECT u.UserName, u.Role, w.Balance,
       (SELECT COUNT(*) FROM CartItems ci JOIN Carts c ON c.Id = ci.CartId WHERE c.UserId = u.Id) AS CartItems
FROM Users u JOIN Wallets w ON w.UserId = u.Id
WHERE u.UserName LIKE N'checkout[_]%'
ORDER BY u.UserName;
