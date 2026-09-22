-- ============================================================================
-- E-commerce database seed script (SQL Server)
-- Safe to re-run: every INSERT is guarded by NOT EXISTS / IF NOT EXISTS checks.
--
-- Seeded logins (password : bcrypt hash was generated with BCrypt.Net-Next,
-- the same library used by ecommerce.Infrastructure.Security.PasswordHasher):
--   admin      / Admin@123
--   customer1  / Customer@123
--   customer2  / Customer@123
--   customer3  / Customer@123
-- ============================================================================

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRANSACTION;

-- ----------------------------------------------------------------------------
-- 1. Categories
-- ----------------------------------------------------------------------------
INSERT INTO Categories (Id, CreatedAt, UpdatedAt, Name)
SELECT NEWID(), GETUTCDATE(), GETUTCDATE(), v.Name
FROM (VALUES
    (N'Electronics'),
    (N'Clothing'),
    (N'Books'),
    (N'Home & Kitchen'),
    (N'Sports & Outdoors')
) AS v(Name)
WHERE NOT EXISTS (SELECT 1 FROM Categories c WHERE c.Name = v.Name);

-- ----------------------------------------------------------------------------
-- 2. Products
-- ----------------------------------------------------------------------------
INSERT INTO Products (Id, CreatedAt, UpdatedAt, Name, Price, Code, AvailableQuantity, CategoryId, IsDeleted)
SELECT NEWID(), GETUTCDATE(), GETUTCDATE(), v.Name, v.Price, v.Code, v.Qty, c.Id, 0
FROM (VALUES
    (N'Wireless Mouse',                N'ELEC-001', 25.99,  150, N'Electronics'),
    (N'Mechanical Keyboard',           N'ELEC-002', 89.99,   80, N'Electronics'),
    (N'27-inch 4K Monitor',            N'ELEC-003', 349.99,  40, N'Electronics'),
    (N'USB-C Hub',                     N'ELEC-004', 34.50,  200, N'Electronics'),
    (N'Men''s Cotton T-Shirt',         N'CLTH-001', 15.99,  300, N'Clothing'),
    (N'Women''s Denim Jacket',         N'CLTH-002', 59.99,  120, N'Clothing'),
    (N'Running Shoes',                 N'CLTH-003', 79.99,   90, N'Clothing'),
    (N'Clean Code',                    N'BOOK-001', 42.00,   60, N'Books'),
    (N'The Pragmatic Programmer',      N'BOOK-002', 39.50,   55, N'Books'),
    (N'Stainless Steel Cookware Set',  N'HOME-001', 129.99,  35, N'Home & Kitchen'),
    (N'Electric Kettle',               N'HOME-002', 24.99,  100, N'Home & Kitchen'),
    (N'Yoga Mat',                      N'SPRT-001', 19.99,  150, N'Sports & Outdoors')
) AS v(Name, Code, Price, Qty, CategoryName)
JOIN Categories c ON c.Name = v.CategoryName
WHERE NOT EXISTS (SELECT 1 FROM Products p WHERE p.Code = v.Code);

-- ----------------------------------------------------------------------------
-- 3. Users
-- Role enum: Admin = 0, Customer = 1
-- Password hashes below are real BCrypt.Net-Next hashes (work cost 11).
-- ----------------------------------------------------------------------------
INSERT INTO Users (Id, CreatedAt, UpdatedAt, UserName, PasswordHash, Role)
SELECT NEWID(), GETUTCDATE(), GETUTCDATE(), v.UserName, v.PasswordHash, v.Role
FROM (VALUES
    (N'admin',     N'$2a$11$aLV8qboI8b1gl1/mtzn.E.3uoEgOBTQUhbd6XoEETfyV.3iHcbmYW', 0),
    (N'customer1', N'$2a$11$rwE9YMkYOl//e8Vo4PmJRuv1BLvaKxiblGUnnMsN/aPW.xBevaNuS', 1),
    (N'customer2', N'$2a$11$rwE9YMkYOl//e8Vo4PmJRuv1BLvaKxiblGUnnMsN/aPW.xBevaNuS', 1),
    (N'customer3', N'$2a$11$rwE9YMkYOl//e8Vo4PmJRuv1BLvaKxiblGUnnMsN/aPW.xBevaNuS', 1)
) AS v(UserName, PasswordHash, Role)
WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.UserName = v.UserName);

-- ----------------------------------------------------------------------------
-- 4. Wallets (one per user)
-- ----------------------------------------------------------------------------
INSERT INTO Wallets (Id, CreatedAt, UpdatedAt, Balance, UserId)
SELECT NEWID(), GETUTCDATE(), GETUTCDATE(), v.Balance, u.Id
FROM (VALUES
    (N'admin',     1000.00),
    (N'customer1', 500.00),
    (N'customer2', 750.00),
    (N'customer3', 1000.00)
) AS v(UserName, Balance)
JOIN Users u ON u.UserName = v.UserName
WHERE NOT EXISTS (SELECT 1 FROM Wallets w WHERE w.UserId = u.Id);

-- ----------------------------------------------------------------------------
-- 5. Carts (one per customer)
-- ----------------------------------------------------------------------------
INSERT INTO Carts (Id, CreatedAt, UpdatedAt, UserId)
SELECT NEWID(), GETUTCDATE(), GETUTCDATE(), u.Id
FROM Users u
WHERE u.UserName IN (N'customer1', N'customer2', N'customer3')
  AND NOT EXISTS (SELECT 1 FROM Carts c WHERE c.UserId = u.Id);

-- ----------------------------------------------------------------------------
-- 6. Cart items
-- customer1: Wireless Mouse x2, Clean Code x1
-- customer2: Denim Jacket x1, Electric Kettle x2
-- customer3: (left empty on purpose, to exercise the empty-cart path)
-- ----------------------------------------------------------------------------
INSERT INTO CartItems (Id, CreatedAt, UpdatedAt, CartId, ProductId, Quantity)
SELECT NEWID(), GETUTCDATE(), GETUTCDATE(), c.Id, p.Id, v.Quantity
FROM (VALUES
    (N'customer1', N'ELEC-001', 2),
    (N'customer1', N'BOOK-001', 1),
    (N'customer2', N'CLTH-002', 1),
    (N'customer2', N'HOME-002', 2)
) AS v(UserName, ProductCode, Quantity)
JOIN Users u ON u.UserName = v.UserName
JOIN Carts c ON c.UserId = u.Id
JOIN Products p ON p.Code = v.ProductCode
WHERE NOT EXISTS (SELECT 1 FROM CartItems ci WHERE ci.CartId = c.Id AND ci.ProductId = p.Id);

-- ----------------------------------------------------------------------------
-- 7. Orders + order items + matching wallet transactions
-- Orders have no natural key, so each block is guarded at the user level:
-- it only runs the first time (skipped on re-run once the user has an order).
-- ----------------------------------------------------------------------------

-- customer1 / Order 1: Mechanical Keyboard x1 + T-Shirt x3, already emailed
IF NOT EXISTS (
    SELECT 1 FROM Orders o JOIN Users u ON u.Id = o.UserId
    WHERE u.UserName = N'customer1' AND o.EmailSentAt IS NOT NULL
)
BEGIN
    DECLARE @Customer1Id UNIQUEIDENTIFIER = (SELECT Id FROM Users WHERE UserName = N'customer1');
    DECLARE @Order1Id UNIQUEIDENTIFIER = NEWID();
    DECLARE @Order1Total DECIMAL(18,2) = 89.99 * 1 + 15.99 * 3;

    INSERT INTO Orders (Id, CreatedAt, UpdatedAt, UserId, EmailSentAt)
    VALUES (@Order1Id, GETUTCDATE(), GETUTCDATE(), @Customer1Id, GETUTCDATE());

    INSERT INTO OrderItems (Id, CreatedAt, UpdatedAt, OrderId, ProductId, PriceAtPurchased, Quantity)
    SELECT NEWID(), GETUTCDATE(), GETUTCDATE(), @Order1Id, p.Id, v.Price, v.Quantity
    FROM (VALUES (N'ELEC-002', 89.99, 1), (N'CLTH-001', 15.99, 3)) AS v(ProductCode, Price, Quantity)
    JOIN Products p ON p.Code = v.ProductCode;

    INSERT INTO WalletTransactions (Id, CreatedAt, UpdatedAt, WalletId, OrderId, Amount)
    SELECT NEWID(), GETUTCDATE(), GETUTCDATE(), w.Id, @Order1Id, -@Order1Total
    FROM Wallets w WHERE w.UserId = @Customer1Id;
END

-- customer1 / Order 2: Pragmatic Programmer x2, email not sent yet
IF NOT EXISTS (
    SELECT 1 FROM Orders o JOIN Users u ON u.Id = o.UserId
    WHERE u.UserName = N'customer1' AND o.EmailSentAt IS NULL
)
BEGIN
    DECLARE @Customer1IdB UNIQUEIDENTIFIER = (SELECT Id FROM Users WHERE UserName = N'customer1');
    DECLARE @Order2Id UNIQUEIDENTIFIER = NEWID();
    DECLARE @Order2Total DECIMAL(18,2) = 39.50 * 2;

    INSERT INTO Orders (Id, CreatedAt, UpdatedAt, UserId, EmailSentAt)
    VALUES (@Order2Id, GETUTCDATE(), GETUTCDATE(), @Customer1IdB, NULL);

    INSERT INTO OrderItems (Id, CreatedAt, UpdatedAt, OrderId, ProductId, PriceAtPurchased, Quantity)
    SELECT NEWID(), GETUTCDATE(), GETUTCDATE(), @Order2Id, p.Id, 39.50, 2
    FROM Products p WHERE p.Code = N'BOOK-002';

    INSERT INTO WalletTransactions (Id, CreatedAt, UpdatedAt, WalletId, OrderId, Amount)
    SELECT NEWID(), GETUTCDATE(), GETUTCDATE(), w.Id, @Order2Id, -@Order2Total
    FROM Wallets w WHERE w.UserId = @Customer1IdB;
END

-- customer2 / Order 1: Yoga Mat x1 + Cookware Set x1, already emailed
IF NOT EXISTS (
    SELECT 1 FROM Orders o JOIN Users u ON u.Id = o.UserId
    WHERE u.UserName = N'customer2'
)
BEGIN
    DECLARE @Customer2Id UNIQUEIDENTIFIER = (SELECT Id FROM Users WHERE UserName = N'customer2');
    DECLARE @Order3Id UNIQUEIDENTIFIER = NEWID();
    DECLARE @Order3Total DECIMAL(18,2) = 19.99 * 1 + 129.99 * 1;

    INSERT INTO Orders (Id, CreatedAt, UpdatedAt, UserId, EmailSentAt)
    VALUES (@Order3Id, GETUTCDATE(), GETUTCDATE(), @Customer2Id, GETUTCDATE());

    INSERT INTO OrderItems (Id, CreatedAt, UpdatedAt, OrderId, ProductId, PriceAtPurchased, Quantity)
    SELECT NEWID(), GETUTCDATE(), GETUTCDATE(), @Order3Id, p.Id, v.Price, v.Quantity
    FROM (VALUES (N'SPRT-001', 19.99, 1), (N'HOME-001', 129.99, 1)) AS v(ProductCode, Price, Quantity)
    JOIN Products p ON p.Code = v.ProductCode;

    INSERT INTO WalletTransactions (Id, CreatedAt, UpdatedAt, WalletId, OrderId, Amount)
    SELECT NEWID(), GETUTCDATE(), GETUTCDATE(), w.Id, @Order3Id, -@Order3Total
    FROM Wallets w WHERE w.UserId = @Customer2Id;
END

-- ----------------------------------------------------------------------------
-- 8. Inventory jobs (admin-uploaded stock files)
-- Status enum: Stored = 0, Accepted = 1, Processing = 2, Done = 3, Failed = 4
-- ----------------------------------------------------------------------------
INSERT INTO InventoryJobs (Id, CreatedAt, UpdatedAt, UserId, OriginalFileName, StoredFileName, Status, EmailSentAt, ErrorMessage)
SELECT NEWID(), GETUTCDATE(), GETUTCDATE(), u.Id, v.OriginalFileName, v.StoredFileName, v.Status, v.EmailSentAt, v.ErrorMessage
FROM (VALUES
    (N'october-restock.csv', N'20260901-000001-october-restock.csv', 3, CAST(GETUTCDATE() AS DATETIME2), CAST(NULL AS NVARCHAR(MAX))),
    (N'bad-format.csv',      N'20260905-000002-bad-format.csv',      4, CAST(NULL AS DATETIME2), N'Row 12: AvailableQuantity is not a valid integer.')
) AS v(OriginalFileName, StoredFileName, Status, EmailSentAt, ErrorMessage)
CROSS JOIN Users u
WHERE u.UserName = N'admin'
  AND NOT EXISTS (SELECT 1 FROM InventoryJobs ij WHERE ij.StoredFileName = v.StoredFileName);

COMMIT TRANSACTION;
